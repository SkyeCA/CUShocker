using System.Text.Json.Serialization;
using CUShocker.Core.Logic;
using CUShocker.Protocol;

namespace CUShocker.Core;

public class CUShockerSettings
{
	public int UdpPort { get; set; } = ProtocolInfo.DefaultPort;
	public OutputMode Output { get; set; } = OutputMode.DryRun;

	public TriggerFilter Trigger { get; set; } = TriggerFilter.IncreasesOnly;
	public float MinPainChange { get; set; } = 1f;
	public float PainForMaximum { get; set; } = 100f;

	public ControlMode Mode { get; set; } = ControlMode.Shock;

	public ModeConfig Shock { get; set; } = new()
	{
		StartsAtPain = 1,
		MinIntensity = 5,
		MaxIntensity = 25,
		MinDurationMs = 300,
		MaxDurationMs = 300,
	};

	public ModeConfig Vibrate { get; set; } = new()
	{
		StartsAtPain = 1,
		MinIntensity = 10,
		MaxIntensity = 60,
		MinDurationMs = 300,
		MaxDurationMs = 1000,
	};

	[JsonIgnore]
	public ModeConfig ActiveMode => Mode == ControlMode.Vibrate ? Vibrate : Shock;

	public void ClampTo(ControlLimits limits)
	{
		Shock.ClampTo(limits);
		Vibrate.ClampTo(limits);
	}
}

public enum OutputMode
{
	Off,
	DryRun, // I renamed this 'Test Mode' in the UI fwiw
	Live,
}

public enum ControlMode
{
	Shock,
	Vibrate,
}

public enum TriggerFilter
{
	AllChanges,
	IncreasesOnly,
}

public sealed class ModeConfig
{
	public float StartsAtPain { get; set; }
	public int MinIntensity { get; set; } = 1;
	public int MaxIntensity { get; set; } = 1;
	public int MinDurationMs { get; set; } = 300;
	public int MaxDurationMs { get; set; } = 300;

	public void ClampTo(ControlLimits limits)
	{
		MinIntensity = Math.Clamp(MinIntensity, limits.MinIntensity, limits.MaxIntensity);
		MaxIntensity = Math.Clamp(MaxIntensity, limits.MinIntensity, limits.MaxIntensity);
		MinDurationMs = Math.Clamp(MinDurationMs, limits.MinDurationMs, limits.MaxDurationMs);
		MaxDurationMs = Math.Clamp(MaxDurationMs, limits.MinDurationMs, limits.MaxDurationMs);
	}
}
