using System;
using System.Net.Sockets;
using NModbus;

namespace CapsuleControl;

public class RelayController : IDisposable
{
    private TcpClient?    _client;
    private IModbusMaster? _master;
    private readonly string _ip;
    private readonly int    _port;

    public bool IsConnected { get; private set; }

    public RelayController(string ipAddress, int port = 502)
    {
        _ip   = ipAddress;
        _port = port;
        Connect();
    }

    private void Connect()
    {
        try
        {
            _client?.Dispose();
            _master?.Dispose();

            _client = new TcpClient();
            _client.Connect(_ip, _port);
            _master = new ModbusFactory().CreateMaster(_client);
            IsConnected = true;
            Logger.Info($"Relay connected: {_ip}:{_port}");
            EndSession();
        }
        catch (Exception ex)
        {
            IsConnected = false;
            Logger.Error($"Relay connection failed: {_ip}:{_port}", ex);
        }
    }

    public void SetRelay(int channel, bool state)
    {
        if (!IsConnected || _master == null) return;
        try
        {
            _master.WriteSingleCoil(1, (ushort)channel, state);
        }
        catch (Exception ex)
        {
            Logger.Error("SetRelay failed", ex);
            IsConnected = false;
        }
    }

    public bool[] ReadDigitalInputs()
    {
        if (!IsConnected || _master == null) return new bool[8];
        try
        {
            return _master.ReadInputs(1, 0, 8);
        }
        catch (Exception ex)
        {
            Logger.Error("ReadDigitalInputs failed", ex);
            IsConnected = false;
            return new bool[8];
        }
    }

    public void TryReconnect() => Connect();

    public void StartSession() => SetRelay(0, true);
    public void EndSession()   => SetRelay(0, false);

    public void Dispose()
    {
        _master?.Dispose();
        _client?.Dispose();
    }
}
