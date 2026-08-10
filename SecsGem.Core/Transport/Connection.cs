using SecsGem.Core.Events;
using SecsGem.Core.HSMS;
using SecsGem.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace SecsGem.Core.Transport
{
    public class Connection : Events.InvokeEvents, IDisposable
    {
        private readonly TcpClient _client;
        private readonly CancellationToken _token;
        private readonly NetworkStream _stream;
        private readonly PacketAssembler _packetAssembler;
        private byte[] _recieveBuffer = new byte[1024];
        private readonly List<SType> allowedWhilenotSelected = new List<SType>()
        {
            SType.Select_req,
            SType.Select_rsp,
            SType.Separate_req
        };


        public Connection(TcpClient client, CancellationToken token)
        {
            _client = client;
            _token = token;
            _stream = _client.GetStream();
            _packetAssembler = new PacketAssembler();
        }

        public TcpClient TcpClient => _client;
        public async Task ReadDataFromBuffer(HsmsSession session)
        {
            while (!_token.IsCancellationRequested)
            {
                var bytesRead = await _stream.ReadAsync(_recieveBuffer, 0, _recieveBuffer.Length);

                if (bytesRead == 0)
                {
                    Console.WriteLine("Client disconnected gracefully.");

                    var clientToRemove = _client;

                    DisconnectEventTriggerd();
                    break;
                }

                var dataBytes = new Events.BytesReceivedEventArgs(_recieveBuffer.AsSpan(0, bytesRead).ToArray());
                if (dataBytes.Bytes.Length > 0)
                {
                    var assembledPackets = _packetAssembler.AssemblePackets(dataBytes.Bytes);
                    OnDataReceived(dataBytes);
                    OnPacketAssemble(new Events.PacketReceivedEventArgs(assembledPackets));

                    int packetsIter = 0;
                    int retryCount = 0;
                    while(packetsIter < assembledPackets.Count)
                    {
                        if (retryCount == 10)
                        {
                            Console.WriteLine("Cannot process message between Host and Equipment as the session not selected yet!");
                            break;
                        }
                        var sessionSelected = session.CurrentSessionState == SessionState.Selected ? true : false;
                        bool encounteredError = false;
                        var hsmsDecoder = new HsmsDecoder(assembledPackets[packetsIter]);
                        var hsmsMessage = new HsmsMessage();
                        try
                        {
                            hsmsMessage = hsmsDecoder.Decode();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error in decoding packets!");
                            encounteredError = true;
                        }
                        if (sessionSelected && hsmsMessage.SType == SType.Select_req)
                        {
                            Console.WriteLine("Cannot process selecte.req between Host and Equipment as the session is already selected!");
                            encounteredError = true;
                        }



                        if (!sessionSelected && !allowedWhilenotSelected.Contains(hsmsMessage.SType))
                        {
                            encounteredError = true;
                            ++retryCount;
                            continue;
                        }

                        OnHsmsRequestRecieved(new Events.HSMSMessageEventArgs(hsmsMessage));

                        if (hsmsMessage != null)
                        {

                            var response = session.ProcessMessage(hsmsMessage, encounteredError, out HsmsMessage hsmsResponse);
                            OnHsmsResponseRecieved(new Events.HSMSMessageEventArgs(hsmsResponse));

                            if (response.Length > 0)
                            {
                                await WriteDataToBufferAsync(response);
                                OnEquipmentSessionStateChange(new Events.ConnectionStateEventArgs(session.CurrentSessionState));
                            }
                            else
                            {
                                OnHostSessionStateChange(new ConnectionStateEventArgs(session.CurrentSessionState));
                            }
                        }
                        ++packetsIter;
                    }
                    
                    
                }
            }
        }

        public async Task WriteDataToBufferAsync(byte[] response)
        {
            if (response.Length > 0)
            {
                await _stream.WriteAsync(response, 0, response.Length);
            }
        }
        public void Dispose()
        {
            _stream.Close();
            _stream.Dispose();
            _client.Close();
            _client.Dispose();
        }
    }
}
