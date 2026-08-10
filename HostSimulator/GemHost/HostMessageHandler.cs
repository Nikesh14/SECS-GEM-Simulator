using HostSimulator.GemHost;
using SecsGem.Core.Host;
using SecsGem.Core.Interfaces;
using SecsGem.Core.Models;
using SecsGem.Core.SecIIMessage;

namespace HostSimulator.GemHost
{
    public class HostMessageHandler : IDataMessageHandler
    {
        private readonly Host _host;

        public HostMessageHandler(Host Host) { _host = Host; }
       
        SecsMessage IDataMessageHandler.Handle(HsmsMessage message)
        {
            return new S9F5(message.Payload);
        }
    }
}
