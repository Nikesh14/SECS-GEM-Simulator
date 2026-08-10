using SecsGem.Core.Models;
using SecsGem.Core.SecIIMessage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecsGem.Core.Interfaces
{
    public interface IDataMessageHandler
    {
        public SecsMessage Handle(HsmsMessage message);
    }
}
