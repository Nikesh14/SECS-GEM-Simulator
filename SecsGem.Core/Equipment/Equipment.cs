using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecsGem.Core.Equipment
{
    public class Equipment
    {
        public Identity Identity { get; set; } = new Identity();
        public CommunicationState Communicationstate { get; set; } = new CommunicationState();
        public ControlState ControlState { get; set; } = new ControlState();
        public ProcessingState ProcessingState { get; set; } = new ProcessingState();
        public Dictionary<uint, StatusVariable> StatusVariables = new Dictionary<uint, StatusVariable>();
    }
}
