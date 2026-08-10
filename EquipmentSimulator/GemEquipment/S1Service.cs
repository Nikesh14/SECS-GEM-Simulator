using SecsGem.Core.Equipment;
using SecsGem.Core.Host;
using SecsGem.Core.Models;
using SecsGem.Core.SecIIMessage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EquipmentSimulator.GemEquipment
{
    public class S1Service
    {
        private readonly Equipment _equipment;

        public S1Service(Equipment Equipment)
        {
            _equipment = Equipment;
        }
        public SecsMessage HandleS1F1(HsmsMessage _message)
        {
            if(_message.Payload != null)
            {
                var s9Service = new S9Service(_message);
                return s9Service.SendS9F7();
            }
            else
            {
                return new S1F2(_equipment.Identity);
            }
        }
        public SecsMessage HandleS1F13(HsmsMessage _message)
        {
            if( _message.DeviceId == _equipment.Identity.DeviceId && _equipment.Communicationstate.CurrentStatus != SecsGem.Core.Equipment.CommunicationStatus.Disabled)
            {
                if (_message.Payload == null
                       || _message.Payload!.ItemType != SecsItemType.List
                       || (_message.Payload as ListItem)!.Value.Count() != 2
                       || (_message.Payload as ListItem)!.Value.Any(item => item is not AsciiItem))
                {
                    var s9Service = new S9Service(_message);
                    return s9Service.SendS9F7();
                }
                else
                {
                    if (_equipment.Communicationstate.CurrentStatus == SecsGem.Core.Equipment.CommunicationStatus.NotCommunicating)
                        _equipment.Communicationstate.CurrentStatus = SecsGem.Core.Equipment.CommunicationStatus.Communicating;
                    Console.WriteLine($"Equipment {_equipment.Identity.ModelName} is connected!");
                    return new S1F14(_equipment.Identity, Commack.Accepted);
                }
            }
            else
            {
                Console.WriteLine($"Equipment {_equipment.Identity.ModelName} is Disabled and connot be connected!");
                return new S1F14(_equipment.Identity, Commack.Denied);
            }
        }
        public SecsMessage HandleS1F15(HsmsMessage _message)
        {
            if (_message.DeviceId == _equipment.Identity.DeviceId && _equipment.Communicationstate.CurrentStatus == SecsGem.Core.Equipment.CommunicationStatus.Communicating)
            {
                if (_message.Payload != null)
                {
                    var s9Service = new S9Service(_message);
                    return s9Service.SendS9F7();
                }
                else
                {
                    if (_equipment.ControlState.CurrentControlState == SecsGem.Core.Equipment.ControlStatus.OnlineLocal
                        || _equipment.ControlState.CurrentControlState == SecsGem.Core.Equipment.ControlStatus.OnlineRemote)
                        _equipment.ControlState.CurrentControlState = SecsGem.Core.Equipment.ControlStatus.Offline;

                    Console.WriteLine($"Equipment {_equipment.Identity.ModelName} is offline!");
                    return new S1F16(Oflack.Accepted);
                }
            }
            else
            {
                Console.WriteLine($"Equipment {_equipment.Identity.ModelName} not connected to Host!");
                return new S1F16(Oflack.Denied);
            }
        }
        public SecsMessage HandleS1F17(HsmsMessage _message)
        {
            if (_message.DeviceId == _equipment.Identity.DeviceId && _equipment.Communicationstate.CurrentStatus == SecsGem.Core.Equipment.CommunicationStatus.Communicating)
            {
                if (_message.Payload != null)
                {
                    var s9Service = new S9Service(_message);
                    return s9Service.SendS9F7();
                }
                else
                {
                    if (_equipment.ControlState.CurrentControlState == SecsGem.Core.Equipment.ControlStatus.Offline)
                        _equipment.ControlState.CurrentControlState = _equipment.Identity.DefaultOnlineState == "OnlineRemote" ? SecsGem.Core.Equipment.ControlStatus.OnlineRemote : SecsGem.Core.Equipment.ControlStatus.OnlineLocal;

                    Console.WriteLine($"Equipment {_equipment.Identity.ModelName} is {_equipment.Identity.DefaultOnlineState}!");
                    return new S1F18(Onlack.Accepted);
                }
            }
            else
            {
                Console.WriteLine($"Equipment {_equipment.Identity.ModelName} not connected to Host!");
                return new S1F18(Onlack.Denied);
            }
        }
        public async Task CreateS1F14()
        {

        }
        public async Task CreateS1F2()
        {

        }
    }
}
