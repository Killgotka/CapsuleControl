using System;
using System.Net.Sockets;
using NModbus;

namespace CapsuleControl;

public class RelayController : IDisposable
{
	private TcpClient? _client;

	private IModbusMaster? _master;

	private bool _isConnected;

	public RelayController(string ipAddress, int port = 502)
	{
		try
		{
			_client = new TcpClient();
			_client.Connect(ipAddress, port);
			ModbusFactory modbusFactory = new ModbusFactory();
			_master = modbusFactory.CreateMaster(_client);
			_isConnected = true;
			EndSession();
		}
		catch (Exception)
		{
			_isConnected = false;
		}
	}

	public void SetRelay(int channel, bool state)
	{
		if (!_isConnected || _master == null)
		{
			return;
		}
		try
		{
			_master.WriteSingleCoil(1, (ushort)channel, state);
		}
		catch (Exception)
		{
		}
	}

	public bool[] ReadDigitalVal()
	{
		if (!_isConnected || _master == null)
		{
			return new bool[8];
		}
		try
		{
			return _master.ReadInputs(1, 0, 8);
		}
		catch (Exception)
		{
			return new bool[8];
		}
	}

	public void StartSession()
	{
		SetRelay(0, state: true);
	}

	public void EndSession()
	{
		SetRelay(0, state: false);
	}

	public void Dispose()
	{
		_master?.Dispose();
		_client?.Dispose();
	}
}
