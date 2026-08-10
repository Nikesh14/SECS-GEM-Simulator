using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecsGem.Core.Host
{
    public class ControlState
    {
        public ControlStatus CurrentControlState { get; set; } = ControlStatus.Offline;
    }
    public enum ControlStatus
    {
        Offline,
        Pending,
        OnlineLocal,
        OnlineRemote
    }
}
