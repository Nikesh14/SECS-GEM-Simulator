using HostSimulator;
using HostSimulator.GemHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using SecsGem.Core;
using SecsGem.Core.Equipment;
using SecsGem.Core.Formatter;
using SecsGem.Core.Host;
using SecsGem.Core.HSMS;
using SecsGem.Core.Models;
using SecsGem.Core.SecIIMessage;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using CommunicationState = SecsGem.Core.Host.CommunicationState;


var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build(); ;

Console.WriteLine("Start Host Simulator : Y or N");
var decision = Console.ReadLine();



if (decision == null || (decision != "Y" && decision != "N"))
{
    Console.WriteLine("Please enter either Y or N to make decision!");
    return;
}
if (decision == "N")
{
    Console.WriteLine("Host Simulator Stopped!");
    return;
}
//ushort deviceId = 1001;


var s1f1 = new S1F1();

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

    var host = new Host();
    try
    {
        var hostconfig = configuration.GetSection("Host");
        var hostIdentity = new SecsGem.Core.Host.Identity()
        {
            ModelName = hostconfig.GetSection("ModelName").Value,
            SoftwareRevision = hostconfig.GetSection("SoftwareRevision").Value
        };
        host.Identity = hostIdentity;
    }
    catch (Exception ex)
    {
        throw new InvalidDataException($"Unable to build Equipment model insufficient information! {ex.Message}");
    }


    var tcpClient = new TcpClient(ip, 5000);
    Console.WriteLine("Please enter the DeviceId you want to connect to: ");
    ushort deviceId = ushort.Parse(Console.ReadLine());

    var communicationState = new CommunicationState();

    var controlState = new SecsGem.Core.Host.ControlState();

    host.Communicationstate = communicationState;
    host.Controlstate = controlState;
    var hosttcpConn = new HostTcpConnection(tcpClient, deviceId, new HostSimulator.GemHost.HostMessageHandler(host), CT.Token );


    var conn = await hosttcpConn.ConnectAsync();
    var session = hosttcpConn.Session;

    var selected = false;
    conn.HostConnectionStateReceived += OnHostStateChange!;

    try
    {
        var s1Service = new S1Service(host, session);
        await s1Service.SendS1F13();
        await s1Service.SendS1F15();
        await s1Service.SendS1F17();
    }
    catch (TimeoutException)
    {
        Console.WriteLine("Equipment did not respond.");
    }

    void OnHostStateChange(object eventSender, SecsGem.Core.Events.ConnectionStateEventArgs e)
    {
        if (e == null)
            Console.WriteLine($"No Equipment Session State Message Recieved!");
        else
        {
            if (session.CurrentSessionState == SessionState.Selected) selected = true;
            Console.WriteLine($"Equipment Session State : {session.CurrentSessionState}");
        }
    }


    ConsoleKeyInfo keyInfo = Console.ReadKey(true);

    if (CT.IsCancellationRequested)
    {
        tcpClient.Close();
        break;
    }

}





