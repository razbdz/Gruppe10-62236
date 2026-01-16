using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PackBot;

public class Robot
{
    private readonly string _ipAddress;
    private readonly int _dashboardPort;
    private readonly int _urscriptPort;

    private readonly TcpClient _clientDashboard = new();
    private readonly TcpClient _clientUrscript = new();

    private Stream? _streamDashboard;
    private StreamReader? _readerDashboard;
    private Stream? _streamUrscript;

    public Robot(string ipAddress = "172.20.254.108", int dashboardPort = 29999, int urscriptPort = 30002)
    {
        _ipAddress = ipAddress;
        _dashboardPort = dashboardPort;
        _urscriptPort = urscriptPort;
    }

    public bool Connected => _clientDashboard.Connected && _clientUrscript.Connected;

    public bool ProgramRunning
    {
        get
        {
            if (!_clientDashboard.Connected) return false;
            SendDashboard("running\n");
            var line = ReadLineDashboard();
            return line.Trim().Equals("Program running: true", StringComparison.OrdinalIgnoreCase);
        }
    }

    public string RobotMode
    {
        get
        {
            if (!_clientDashboard.Connected) return "DISCONNECTED";
            SendDashboard("robotmode\n");
            return ReadLineDashboard().Trim();
        }
    }

    public void Connect()
    {
        // Tving IPv4 (undgår ::1 / ::ffff:)
        var ip = ResolveIPv4(_ipAddress);

        _clientDashboard.Connect(ip, _dashboardPort);
        _streamDashboard = _clientDashboard.GetStream();
        _readerDashboard = new StreamReader(_streamDashboard, Encoding.ASCII);

        // Dashboard sender en "Connected: Universal Robots Dashboard Server" linje
        _ = ReadLineDashboard();

        _clientUrscript.Connect(ip, _urscriptPort);
        _streamUrscript = _clientUrscript.GetStream();
    }

    public void Disconnect()
    {
        try { _streamUrscript?.Dispose(); } catch { /* ignore */ }
        try { _readerDashboard?.Dispose(); } catch { /* ignore */ }
        try { _streamDashboard?.Dispose(); } catch { /* ignore */ }
        try { _clientUrscript.Close(); } catch { /* ignore */ }
        try { _clientDashboard.Close(); } catch { /* ignore */ }
    }

    public async Task PowerOn()
    {
        SendDashboard("power on\n");
        _ = ReadLineDashboard();
        await Task.Delay(800);
    }

    public async Task BrakeRelease()
    {
        SendDashboard("brake release\n");
        _ = ReadLineDashboard();
        await Task.Delay(800);
    }

    public void StopProgram()
    {
        SendDashboard("stop\n");
        _ = ReadLineDashboard();
    }

    public void SendDashboard(string command)
    {
        if (_streamDashboard == null) throw new InvalidOperationException("Dashboard stream not connected.");
        var bytes = Encoding.ASCII.GetBytes(command);
        _streamDashboard.Write(bytes, 0, bytes.Length);
        _streamDashboard.Flush();
    }

    public void SendUrscript(string program)
    {
        if (_streamUrscript == null) throw new InvalidOperationException("URScript stream not connected.");
        var bytes = Encoding.ASCII.GetBytes(program);
        _streamUrscript.Write(bytes, 0, bytes.Length);
        _streamUrscript.Flush();
    }

    public void SendUrscriptFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Robot script file not found: {path}");

        var program = File.ReadAllText(path) + Environment.NewLine;
        SendUrscript(program);
    }

    public string ReadLineDashboard()
    {
        if (_readerDashboard == null) throw new InvalidOperationException("Dashboard reader not connected.");
        return _readerDashboard.ReadLine() ?? "";
    }

    private static IPAddress ResolveIPv4(string hostOrIp)
    {
        // Hvis det allerede er en IP:
        if (IPAddress.TryParse(hostOrIp, out var parsed))
        {
            if (parsed.AddressFamily == AddressFamily.InterNetwork) return parsed;

            // Hvis nogen har givet IPv6, så find første IPv4 fra DNS (fallback)
        }

        var addresses = Dns.GetHostAddresses(hostOrIp);
        var ipv4 = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
        if (ipv4 != null) return ipv4;

        throw new Exception($"Could not resolve an IPv4 address for '{hostOrIp}'. Addresses: {string.Join(", ", addresses.Select(a => a.ToString()))}");
    }
}
