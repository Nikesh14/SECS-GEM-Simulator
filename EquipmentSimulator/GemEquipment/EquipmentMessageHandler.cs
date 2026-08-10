using SecsGem.Core.Equipment;
using SecsGem.Core.Interfaces;
using SecsGem.Core.Models;
using SecsGem.Core.SecIIMessage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EquipmentSimulator.GemEquipment
{
    public class EquipmentMessageHandler : IDataMessageHandler
    {
        private readonly Equipment _equipment;

        public EquipmentMessageHandler(Equipment equipment)
        {
            _equipment = equipment;
        }

        SecsMessage IDataMessageHandler.Handle(HsmsMessage message)
        {
            var s9Services = new S9Service(message);
            switch(message.Stream)
            {
                case 1:
                    var s1Service = new S1Service(_equipment);
                    switch(message.Function)
                    {
                        case 1:
                            return s1Service.HandleS1F1(message);
                        case 13:
                            return s1Service.HandleS1F13(message);
                        case 15:
                            return s1Service.HandleS1F15(message);
                        case 17:
                            return s1Service.HandleS1F17(message);
                        default:
                            return s9Services.SendS9F5();
                    }
                default:
                    return s9Services.SendS9F3();
            }
        }
    }
}
