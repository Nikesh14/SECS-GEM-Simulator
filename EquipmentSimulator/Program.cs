using SecsGem.Core;
using System.ComponentModel;
using System.Net;
using Microsoft.Extensions.Configuration;
using SecsGem.Core.Equipment;
using Microsoft.Extensions.Configuration.Json;
using SecsGem.Core.Models;
using System.Net.Http.Headers;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build(); ;

Console.WriteLine("Enter Esc key to stop.");
Console.WriteLine("Start Equipment Simulator : Y or N");
var decision = Console.ReadLine();

if (decision == null || (decision != "Y" && decision != "N"))
{
    Console.WriteLine("Please enter either Y or N to make decision!");
    return;
}
if (decision == "N")
{
    Console.WriteLine("Equipment Simulator Stopped!");
    return;
}
while (true)
{
    Console.WriteLine("Enter IP to Listen to :");
    var ip = Console.ReadLine();
    if (!IPAddress.TryParse(ip, out var ipAddress))
    {
        Console.WriteLine("Please enter a valid IP Address!");
        return;
    }
    Console.WriteLine("Note: Port choosen as 5000 as default if needed a change contact developer!");
    //Console.WriteLine("Enter Port :");

    //var port = Console.ReadLine();
    //if (!int.TryParse(port, out int portNumber) || portNumber < 0 || portNumber > 65535)
    //{
    //    Console.WriteLine("Please enter a valid port number (0-65535)!");
    //    return;
    //}

    var CT = new CancellationTokenSource();

    Console.CancelKeyPress += (sender, eventArgs) =>
    {
        Console.WriteLine("\nCtrl+C detected! Requesting a graceful shutdown...");

        // Tells the OS NOT to kill the app immediately. 
        // We want to finish our cleanup code first.
        eventArgs.Cancel = true;

        // Trigger the signal! This sets CT.Token.IsCancellationRequested to true.
        CT.Cancel();
    };
    var equipment = new Equipment();
    try
    {
        var equipmentConfig = configuration.GetSection("Equipment");
        var equipmentIdentity = new Identity()
        {
            DeviceId = ushort.Parse(equipmentConfig.GetSection("DeviceId").Value),
            Manufacturer = equipmentConfig.GetSection("Manufacturer").Value,
            ModelName = equipmentConfig.GetSection("ModelName").Value,
            SerialNumber = equipmentConfig.GetSection("SerialNumber").Value,
            SoftwareRevision = equipmentConfig.GetSection("SoftwareRevision").Value,
            DefaultOnlineState = equipmentConfig.GetSection("DefaultOnlineState").Value
        };
        // Device id now lives only on Identity; the state objects read it from there.
        equipment.Identity = equipmentIdentity;
        equipment.Communicationstate = new CommunicationState();
        equipment.ControlState = new ControlState();

        equipment.StatusVariables = new Dictionary<uint, StatusVariable>()
        {
            {1, new StatusVariable()
                {
                    Id = 1,
                    Name = "Communication State",
                    ValueProvider = () => new AsciiItem($"{equipment.Communicationstate.CurrentStatus}")
                }
            },
            {2, new StatusVariable()
                {
                    Id = 2,
                    Name = "Control State",
                    ValueProvider = () => new AsciiItem($"{equipment.ControlState.CurrentControlState}")
                }
            },
            {3, new StatusVariable()
                {
                    Id = 3,
                    Name = "Equipment Model",
                    ValueProvider = () => new AsciiItem($"{equipment.Identity.ModelName}")
                }  
            },
            {4, new StatusVariable()
                {
                    Id = 4,
                    Name = "Software Revision",
                    ValueProvider = () => new AsciiItem($"{equipment.Identity.SoftwareRevision}")
                }  
            }
        };
    }
    catch(Exception ex)
    {
        throw new InvalidDataException($"Unable to build Equipment model insufficient information! {ex.Message}");
    }

    var tcpServer = new SecsGem.Core.Transport.TcpServer(ipAddress, 5000, equipment, new EquipmentSimulator.GemEquipment.EquipmentMessageHandler(equipment), CT.Token);


    tcpServer.Start();
    await tcpServer.AcceptLoopAsync();
    // true hides the character from rendering in the terminal console
    ConsoleKeyInfo keyInfo = Console.ReadKey(true);

    if (CT.IsCancellationRequested)
    {
        tcpServer.Stop();
        break;
    }
}


