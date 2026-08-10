using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecsGem.Core.Host
{
    public class CommunicationState
    {
        public CommunicationStatus CurrentStatus { get; set; } = CommunicationStatus.NotCommunicating;
    }
    public enum CommunicationStatus
    {
        Disabled,
        NotCommunicating,
        Communicating
    }
}
