using CUShocker.Core;
using CUShocker.Core.Logic;
using CUShocker.Core.Services;
using OpenShock.Desktop.ModuleBase.Api;
using OpenShock.Desktop.ModuleBase.Config;
using OpenShock.Desktop.ModuleBase.Models;

namespace CUShocker.OpenShock.Services;

public sealed class OpenShockOutput : IShockerOutput
{
	public static readonly ControlLimits Limits = new(MinIntensity: 1, MaxIntensity: 100, MinDurationMs: 300, MaxDurationMs: 30000);

	private readonly IModuleConfig<CUShockerConfig> _config;
	private readonly IOpenShockControl _control;

	public OpenShockOutput(IModuleConfig<CUShockerConfig> config, IOpenShockControl control)
	{
		_config = config;
		_control = control;
	}

	public int ShockerCount => _config.Config.ShockerIds.Count;

	public Task Send(ControlDecision decision)
	{
		var shocks = _config.Config.ShockerIds.ToList().Select(id => new ShockerControl
		{
			Id = id,
			Type = ToControlType(decision.Mode),
			Intensity = decision.Intensity,
			Duration = decision.DurationMs,
			Exclusive = true,
		});
		return _control.Control(shocks, "CUShocker");
	}

	private static ControlType ToControlType(ControlMode mode) =>
		mode == ControlMode.Vibrate ? ControlType.Vibrate : ControlType.Shock;
}
