using CUShocker.Core;
using CUShocker.Core.Logic;
using CUShocker.MultiShock;
using CUShocker.MultiShock.Services;
using MultiShock.PluginSdk;

namespace CUShocker.Tests;

public class MultiShockOutputTests
{
	private readonly MultiShockConfig _config = new();
	private readonly FakeDeviceActions _devices = new();
	private readonly MultiShockOutput _output;

	public MultiShockOutputTests()
	{
		_config.Shockers.AddRange([new ShockerRef(1, 10), new ShockerRef(1, 11), new ShockerRef(2, 20)]);
		_output = new MultiShockOutput(_config, _devices);
	}

	[Fact]
	public void CountsSelectedShockers()
	{
		Assert.Equal(3, _output.ShockerCount);
	}

	[Theory]
	[InlineData(ControlMode.Shock, CommandType.Shock)]
	[InlineData(ControlMode.Vibrate, CommandType.Vibrate)]
	public async Task SendsOneActionPerDeviceForOnlyTheSelectedShockers(ControlMode mode, CommandType expected)
	{
		await _output.Send(new ControlDecision(mode, 42, 300, 60));

		Assert.Collection(_devices.Actions,
			first =>
			{
				Assert.Equal([1], first.DeviceIds);
				Assert.Equal([10, 11], first.ShockerIds);
			},
			second =>
			{
				Assert.Equal([2], second.DeviceIds);
				Assert.Equal([20], second.ShockerIds);
			});
		Assert.All(_devices.Actions, action =>
		{
			Assert.Equal(expected, action.Command);
			Assert.Equal(42, action.Intensity);
			Assert.Equal(0.3, action.DurationSeconds, 3);
		});
	}

	[Theory]
	[InlineData(1f)]
	[InlineData(500f)]
	public void PiShockDurationsStayBetweenOneAndFifteenSeconds(float pain)
	{
		var config = new MultiShockConfig { Mode = ControlMode.Vibrate };
		config.Vibrate = new ModeConfig { StartsAtPain = 1, MinIntensity = 10, MaxIntensity = 60, MinDurationMs = 300, MaxDurationMs = 60000 };

		var decision = PainRules.Choose(config, MultiShockOutput.Limits, pain)!.Value;

		Assert.InRange(decision.DurationMs, 1000, 15000);
	}

	[Fact]
	public async Task FinishesOnlyOnceTheActionHasHadTimeToRun()
	{
		var send = _output.Send(new ControlDecision(ControlMode.Vibrate, 42, 300, 60));

		Assert.False(send.IsCompleted);
		await send;
	}

	private sealed record PerformedAction(int Intensity, double DurationSeconds, CommandType Command, List<int>? DeviceIds, List<int>? ShockerIds);

	private sealed class FakeDeviceActions : IDeviceActions
	{
		public List<PerformedAction> Actions { get; } = [];

		public void PerformAction(int intensity, double durationSeconds, CommandType command, string shockerOption = "all",
			IEnumerable<int>? deviceIds = null, IEnumerable<int>? shockerIds = null, bool warning = false, bool held = false, bool priority = false) =>
			Actions.Add(new PerformedAction(intensity, durationSeconds, command, deviceIds?.ToList(), shockerIds?.ToList()));

		public int LoadedShockersCount => throw new NotSupportedException();
		public int ConnectedSerialDeviceCount => throw new NotSupportedException();
		public event Action<int>? SerialDeviceConnected { add { } remove { } }
		public event Action<int>? SerialDeviceDisconnected { add { } remove { } }
		public event Action<SerialReceivedEventArgs>? SerialDataReceived { add { } remove { } }
		public bool IsShockerLoaded(int deviceId, int shockerId) => throw new NotSupportedException();
		public IEnumerable<ShockerInfo> GetLoadedShockers() => throw new NotSupportedException();
		public IEnumerable<DeviceShockerGroup> GetLoadedShockersGroupedByDevice() => throw new NotSupportedException();
		public IEnumerable<SerialDeviceInfo> GetConnectedSerialDevices() => throw new NotSupportedException();
		public SerialDeviceInfo? GetSerialDeviceInfo(int clientId) => throw new NotSupportedException();
		public IEnumerable<string> GetAllSerialPorts() => throw new NotSupportedException();
		public bool DisconnectSerialDevice(int clientId) => throw new NotSupportedException();
		public bool DisconnectSerialPort(string portName) => throw new NotSupportedException();
		public bool ReconnectSerialPort(string portName) => throw new NotSupportedException();
		public IEnumerable<SerialLogEntry> GetRecentSerialLogs(int maxEntries = 100) => throw new NotSupportedException();
		public IEnumerable<SerialLogEntry> GetSerialLogsForDevice(int clientId, int maxEntries = 100) => throw new NotSupportedException();
		public bool SendSerialCommand(int clientId, string command, object? value = null) => throw new NotSupportedException();
		public bool SendSerialCommandByPort(string portName, string command, object? value = null) => throw new NotSupportedException();
		public void AddSerialLog(string portName, string messageType, string data, SerialDataDirection direction = SerialDataDirection.Received) => throw new NotSupportedException();
	}
}
