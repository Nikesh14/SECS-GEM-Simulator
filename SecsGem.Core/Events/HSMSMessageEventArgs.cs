using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecsGem.Core.Events
{
    public class HSMSMessageEventArgs : EventArgs
    {
        public HSMSMessageEventArgs(Models.HsmsMessage message)
        {
            HsmsMessage = message;
        }
        public Models.HsmsMessage HsmsMessage { get; }
    }
}
