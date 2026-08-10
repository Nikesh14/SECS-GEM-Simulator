using SecsGem.Core.HSMS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecsGem.Core.Events
{
    public class InvokeEvents
    {

        public event EventHandler<Events.BytesReceivedEventArgs>? DataReceived;
        public event EventHandler? Disconnected;
        public event EventHandler<Events.PacketReceivedEventArgs>? PacketAssembled;
        public event EventHandler<Events.HSMSMessageEventArgs>? RequestRecieved;
        public event EventHandler<Events.HSMSMessageEventArgs>? ResponseRecieved;
        public event EventHandler<Events.ConnectionStateEventArgs>? EquipmentConnectionStateReceived;
        public event EventHandler<ConnectionStateEventArgs>? HostConnectionStateReceived;
        public InvokeEvents()
        {
            
        }
        protected virtual void OnPacketAssemble(Events.PacketReceivedEventArgs e)
        {
            if (PacketAssembled == null)
            {
                Console.WriteLine("No Packet Assembled Event Subscribed!");
            }
            PacketAssembled?.Invoke(this, e);
        }
        protected virtual void OnHsmsRequestRecieved(Events.HSMSMessageEventArgs e)
        {
            if (PacketAssembled == null)
            {
                Console.WriteLine("No HSMS Request Message Event Subscribed!");
            }
            RequestRecieved?.Invoke(this, e);
        }
        protected virtual void OnHsmsResponseRecieved(Events.HSMSMessageEventArgs e)
        {
            if (PacketAssembled == null)
            {
                Console.WriteLine("No HSMS Response Message Event Subscribed!");
            }
            ResponseRecieved?.Invoke(this, e);
        }
        protected virtual void OnDataReceived(Events.BytesReceivedEventArgs e)
        {
            if (DataReceived == null)
            {
                Console.WriteLine("No On Byte Event Subscribed!");
            }
            DataReceived?.Invoke(this, e);
        }
        protected virtual void OnHostSessionStateChange(SecsGem.Core.Events.ConnectionStateEventArgs e)
        {
            if (HostConnectionStateReceived == null)
            {
                Console.WriteLine("No Host Session state Event Subscribed!");
            }
            HostConnectionStateReceived?.Invoke(this, e);
        }
        protected virtual void OnEquipmentSessionStateChange(Events.ConnectionStateEventArgs e)
        {
            if (EquipmentConnectionStateReceived == null)
            {
                Console.WriteLine("No Equipment Session state Event Subscribed!");
            }
            EquipmentConnectionStateReceived?.Invoke(this, e);
        }
        protected virtual void DisconnectEventTriggerd()
        {
            if (Disconnected == null)
            {
                Console.WriteLine("No Disconnect Event Subscribed!");
            }
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }
}
