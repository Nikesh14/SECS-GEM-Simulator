using SecsGem.Core.Events;
using SecsGem.Core.Interfaces;
using SecsGem.Core.Models;
using SecsGem.Core.SecIIMessage;
using SecsGem.Core.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SecsGem.Core.HSMS
{
    public class HsmsSession
    { 
        private readonly Connection _conn;
        private readonly ushort _deviceId;
        private HsmsMessage? _responseRecieved;
        private readonly IDataMessageHandler _handler;
        private uint _systemBytes;
        private SessionState _state;
        const int PollMs = 50, TimeoutMs = 45000;


        public HsmsSession(Connection conn, IDataMessageHandler messageHandler, ushort deviceId)
        {
            _conn = conn;
            _deviceId = deviceId;
            _systemBytes = (uint)Random.Shared.Next();
            // conn is null in ProcessMessage-only unit tests; SendAsync path always has a real connection.
            if (_conn != null)
                _conn.ResponseRecieved += OnHsmsResponseRecieved!;
            _responseRecieved = null ;
            _handler = messageHandler;
        }

        public SessionState CurrentSessionState => _state;

        public byte[] ProcessMessage(HsmsMessage _message, bool encounteredError, out HsmsMessage response)
        {
            bool responseRequired = false;
            var encodedMessage = new List<byte>();


            response = new HsmsMessage();
            response.PType = _message.PType;
            response.SystemBytes = _message.SystemBytes;
            response.DeviceId = _message.DeviceId;
           
            if (encounteredError)
            {
                response.SType = SType.Reject_req;
                responseRequired = true;
            }
            else
            {
                switch (_message.SType)
                {
                    case SType.Select_req:
                        response.SType = SType.Select_rsp;
                        _state = SessionState.Selected;
                        responseRequired = true;
                        break;
                    case SType.Linktest_req:
                        response.SType = SType.Linktest_rsp;
                        responseRequired = true;
                        break;
                    case SType.Deselect_req:
                        response.SType = SType.Deselect_rsp;
                        _state = SessionState.Seperated;
                        responseRequired = true;
                        break;
                    case SType.Select_rsp:
                        _state = SessionState.Selected;
                        break;
                    case SType.Linktest_rsp:
                        break;
                    case SType.Deselect_rsp:
                        _state = SessionState.Seperated;
                        break;
                    case SType.Separate_req:
                        _state = SessionState.NotConnected;
                        _conn.Dispose();
                        break;
                    case SType.Data:
                        bool isPrimary = (_message.Function % 2) == 1;   // odd = request, even = reply
                        if (isPrimary)
                        {
                            var res = _handler.Handle(_message);
                            if (_message.Waitbit && res != null)
                            {
                                responseRequired = true;
                                // A request came in — I must answer it.
                                //var secMessageRes = ProcessSecDataMessage(_message);
                                response.SType = SType.Data;
                                response.Waitbit = res.Waitbit;
                                response.Stream = res.Stream;
                                response.Function = res.Function;
                                response.Payload = res.Payload;
                                // → gets written to the wire
                            }
                        }
                        else
                        {
                            // A reply to something *I* sent — hand it back intact, send nothing.
                            response.SType = SType.Data;
                            response.Stream = _message.Stream;
                            response.Function = _message.Function;
                            response.Waitbit = _message.Waitbit;
                            response.Payload = _message.Payload;
                            // responseRequired stays false — do NOT reply to a reply
                        }
                        break;
                    default:
                        break;
                }
            }
            if (responseRequired)
            {
                var encode = new HsmsEncoder(response);
                encodedMessage.AddRange(encode.Encode().ToArray()); 
            }
            return encodedMessage.ToArray();
        }
        
        public async Task<HsmsMessage> SendAsync(SecsMessage secsMessages)
        {
            var response = new HsmsMessage();
            _responseRecieved = null;
            if(await EnsureSelected())
            {
               
                var hsmsMessage = CreateDataMessage(secsMessages);
                var hsmsEncoder = new HsmsEncoder(hsmsMessage);
                await _conn.WriteDataToBufferAsync(hsmsEncoder.Encode());

               
                int waited = 0;
                while(true)
                {
                    waited += PollMs;
                    await Task.Delay(PollMs);
                    if(waited >= TimeoutMs)
                    {
                        Console.WriteLine("No response recieved!");
                        break;
                    }
                    if (_responseRecieved != null && _responseRecieved.SystemBytes == hsmsMessage.SystemBytes)
                    {
                        return _responseRecieved;
                    }
                }
            }
            throw new TimeoutException(
                $"Timed out waiting for response to S{secsMessages.Stream}F{secsMessages.Function}."
            );
        }
        public void OnHsmsResponseRecieved(object eventSender, Events.HSMSMessageEventArgs e)
        {
            if (e == null)
            {
                Console.WriteLine($"No Hsms Response Message Recieved!");
            }
            else
            {
                _responseRecieved = e.HsmsMessage;
            }
        }

        private uint GetNextSystemBytes()
        {
            return _systemBytes++;
        }
        private async Task<bool> EnsureSelected()
        {
            if(_state != SessionState.Selected)
            {
                var hsmsEncoder = new HsmsEncoder(CreateControlMessage(SType.Select_req));
                await _conn.WriteDataToBufferAsync(hsmsEncoder.Encode());

                int waited = 0;
                while (_state != SessionState.Selected)
                {
                    if (waited >= TimeoutMs)
                        break;
                    waited += PollMs;
                    await Task.Delay(PollMs);
                }

                if (_state != SessionState.Selected)
                {
                    Console.WriteLine("Unable to connect to Equipment");
                    return false;
                }
                
            }
            return true;
        }
        
        private HsmsMessage CreateControlMessage(SType stype)
        {
            return new HsmsMessage() 
            {
                DeviceId = _deviceId,
                SType = stype,
                SystemBytes = GetNextSystemBytes()
            };
        }
        private HsmsMessage CreateDataMessage(SecsMessage secsMessages)
        {
            return new HsmsMessage()
            {
                DeviceId = _deviceId,
                Stream = secsMessages.Stream,
                Function = secsMessages.Function,
                Waitbit = secsMessages.Waitbit,
                SType = SType.Data,
                PType = 0,
                Payload = secsMessages.Payload,
                SystemBytes = GetNextSystemBytes()
            };
        }
    }

    public enum SessionState
    {
        NotConnected,
        Connected,
        Selected,
        NotSelected,
        Seperated
    }
}
