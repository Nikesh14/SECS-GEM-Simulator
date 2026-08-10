using SecsGem.Core.Formatter;
using SecsGem.Core.Models;
using SecsGem.Core.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SecsGem.Core.Events
{
    public static class Subscribe
    {

        public static void OnDataReceived(object eventSender, Events.BytesReceivedEventArgs e)
        {
            if (e == null)
                Console.WriteLine($"No bytes Received!");
            else
            {
                Console.WriteLine($"Bytes Received !");
            }
        }

        public static void OnPacketAssemble(object eventSender, Events.PacketReceivedEventArgs e)
        {
            if (e == null)
                Console.WriteLine($"No Packets Recieved!");
            else
            {
                Console.WriteLine($"Number of Packets Recieved : {e.Packets.Count} ");
            }
        }

        public static void OnHsmsRequestRecieved(object eventSender, Events.HSMSMessageEventArgs e)
        {
            if (e == null)
                Console.WriteLine($"No Hsms Request Message Recieved!");
            else
            {
                var smlFormatter = new SMLFormatter(e.HsmsMessage);

                Console.WriteLine(smlFormatter.FormatMessage());
            }
        }

        public static void OnHostStateChange(object eventSender, Events.ConnectionStateEventArgs e)
        {
            if (e == null)
                Console.WriteLine($"No Host Session State Message Recieved!");
            else
            {
                Console.WriteLine($"Host Session State : {e.SessionState}");
            }
        }

        public static void OnEquipmentStateChange(object eventSender, Events.ConnectionStateEventArgs e)
        {
            if (e == null)
                Console.WriteLine($"No Equipment Session State Message Recieved!");
            else
            {
                Console.WriteLine($"Equipment Session State : {e.SessionState}");
            }
        }


    }
}
