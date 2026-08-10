using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SecsGem.Core.Host
{
    public class Host
    {
        public Identity Identity { get; set; }
        public CommunicationState Communicationstate { get; set; } = new CommunicationState();
        public ControlState Controlstate { get; set; } = new ControlState();
    }
}
