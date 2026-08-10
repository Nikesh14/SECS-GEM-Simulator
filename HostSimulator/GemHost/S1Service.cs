using SecsGem.Core.Formatter;
using SecsGem.Core.Host;
using SecsGem.Core.HSMS;
using SecsGem.Core.Models;
using SecsGem.Core.SecIIMessage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace HostSimulator.GemHost
{
    public class S1Service
    {
        private readonly Host _host;
        private readonly HsmsSession? _session;

        public S1Service(Host Host, SecsGem.Core.HSMS.HsmsSession? Session = null)
        {
            _host = Host;
            if(Session != null) 
                _session = Session;
        }
        public async Task SendS1F1()
        {
            var s1f1 = new S1F1();
            var res = await _session.SendAsync(s1f1);
            if(res != null)
            {
                var formatter = new SMLFormatter(res);
                Console.WriteLine(formatter.FormatMessage());
            }
        }
        public async Task ValidateS1F2()
        {

        }
        public async Task SendS1F13()
        {
            var s1f13 = new S1F13(_host.Identity);
            var res = await _session!.SendAsync(s1f13);
            if (res != null)
            {
                if (ValidateS1F14(s1f13, res)) 
                {
                    var formatter = new SMLFormatter(res);
                    Console.WriteLine(formatter.FormatMessage());
                    Console.WriteLine($"Host {_host.Identity.ModelName} is connected!");
                    _host.Communicationstate.CurrentStatus = CommunicationStatus.Communicating;
                }
                else
                {
                    Console.WriteLine($"Host {_host.Identity.ModelName} is not connected as response is invalid!");
                }
            }
        }
        public bool ValidateS1F14(SecsMessage request, HsmsMessage response)
        {
            if(response != null && response.Payload != null && response.Payload is ListItem)
            {
                var responseItem = response.Payload as ListItem;
                if(responseItem.Value.Count == 2 
                    && (responseItem.Value[0] is BinaryItem) 
                    && (responseItem.Value[0] as BinaryItem).Value.Count > 0 
                    && (responseItem.Value[0] as BinaryItem).Value[0] == (byte)Commack.Accepted
                    && (responseItem.Value[1] is ListItem) 
                    && (responseItem.Value[1] as ListItem).Value.Count() == (request.Payload as ListItem).Value.Count())
                {
                    return true;
                }
            }
            return false;
        }
        public async Task SendS1F15()
        {
            var s1f15 = new S1F15();
            if (_host.Communicationstate.CurrentStatus == CommunicationStatus.Communicating)
            {
                var res = await _session!.SendAsync(s1f15);
                if(res != null && ValidateS1F16(s1f15, res))
                {
                    var formatter = new SMLFormatter(res);
                    Console.WriteLine(formatter.FormatMessage());
                    Console.WriteLine($"Equipment transitioning to Offline!");
                    _host.Controlstate.CurrentControlState = ControlStatus.Offline;
                }
                else
                {
                    Console.WriteLine($"Equipment refused transistioning to offline!");
                }
            }
            else
            {
                Console.WriteLine($"Equipment and Host not connected!");
            }
        }
        public bool ValidateS1F16(SecsMessage request, HsmsMessage response)
        {
            if (response != null && response.Payload != null && response.Payload is BinaryItem)
            {
                var responseItem = response.Payload as BinaryItem;
                if (responseItem.Value.Count == 1
                    && responseItem.Value[0] == (byte)Oflack.Accepted)
                {
                    return true;
                }
            }
            return false;
        }
        public async Task SendS1F17()
        {
            var s1f17 = new S1F17();
            if(_host.Communicationstate.CurrentStatus == CommunicationStatus.Communicating)
            {
                var res = await _session!.SendAsync(s1f17);
                if (res != null && ValidateS1F18(s1f17, res))
                {
                    var formatter = new SMLFormatter(res);
                    Console.WriteLine(formatter.FormatMessage());
                    Console.WriteLine($"Equipment transitioning to Online!");
                    _host.Controlstate.CurrentControlState = ControlStatus.Pending;
                }
                else
                {
                    Console.WriteLine($"Equipment refused transistioning to online! Host state Pending!");
                }
            }
            else
            {
                Console.WriteLine($"Equipment and Host not connected!");
            }
        }
        public bool ValidateS1F18(SecsMessage request, HsmsMessage response)
        {
            if (response != null && response.Payload != null && response.Payload is BinaryItem)
            {
                var responseItem = response.Payload as BinaryItem;
                if (responseItem.Value.Count == 1
                    && responseItem.Value[0] == (byte)Onlack.Accepted)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
