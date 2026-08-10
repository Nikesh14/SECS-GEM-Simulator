using SecsGem.Core.HSMS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecsGem.Core.Events
{
    public class ConnectionStateEventArgs : EventArgs
    {
        public ConnectionStateEventArgs(SessionState sessionState)
        {
            SessionState = sessionState;
        }
        public SessionState SessionState { get; set; }
    }
}
