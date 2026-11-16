using CUShocker.Core;
using CUShocker.Core.Logic;
using CUShocker.Core.Services;
using MultiShock.PluginSdk;

namespace CUShocker.MultiShock.Services;

public sealed class MultiShockOutput : IShockerOutput
{
	public static readonly ControlLimits Limits = new(MinIntensity: 1, MaxIntensity: 100, MinDurationMs: 1000, MaxDurationMs: 15000);

	private readonly MultiShockConfig _config;
	private readonly IDeviceActions _devices;

	public MultiShockOutput(MultiShockConfig config, IDeviceActions devices)
	{
		_config = config;
		_devices = devices;
	}

	public int ShockerCount => _config.Shockers.Count;

	public Task Send(ControlDecision decision)
	{
		var command = decision.Mode == ControlMode.Vibrate ? CommandType.Vibrate : CommandType.Shock;
		var duration = TimeSpan.FromMilliseconds(decision.DurationMs);

		foreach (var device in _config.Shockers.ToList().GroupBy(shocker => shocker.DeviceId))
		{
			_devices.PerformAction(
				decision.Intensity,
				duration.TotalSeconds,
				command,
				deviceIds: [device.Key],
				shockerIds: device.Select(shocker => shocker.ShockerId).ToList()
			);
		}

		return WaitUntilFinished(duration);
	}

	private static Task WaitUntilFinished(TimeSpan duration) => Task.Delay(duration);
}
