using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecsGem.Core.Equipment
{
    public class ControlState
    {
        public ControlStatus CurrentControlState { get; set; } = ControlStatus.Offline;
    }
    public enum ControlStatus
    {
        Offline,
        OnlineLocal,
        OnlineRemote
    }
}
