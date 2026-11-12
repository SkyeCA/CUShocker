using OpenShock.Desktop.ModuleBase.Models;

namespace CUShocker.Module.Logic;

public readonly record struct ControlDecision(ControlType Type, byte Intensity, ushort DurationMs, float Pain);

public static class PainRules
{
	public const int MinIntensity = 1;
	public const int MaxIntensity = 100;
	public const int MinDurationMs = 300;
	public const int MaxDurationMs = 30000;

	public static ControlDecision? Choose(CUShockerConfig config, float pain)
	{
		if (!(pain > 0))
			return null;

		var (type, mode) = config.Mode switch
		{
			ControlMode.Vibrate => (ControlType.Vibrate, config.Vibrate),
			_ => (ControlType.Shock, config.Shock),
		};
		if (pain < mode.StartsAtPain)
			return null;

		float t = PainFraction(pain, mode.StartsAtPain, config.PainForMaximum);
		var (minIntensity, maxIntensity) = Range(mode.MinIntensity, mode.MaxIntensity, MinIntensity, MaxIntensity);
		var (minDuration, maxDuration) = Range(mode.MinDurationMs, mode.MaxDurationMs, MinDurationMs, MaxDurationMs);

		return new ControlDecision(
			type,
			(byte)Lerp(minIntensity, maxIntensity, t),
			(ushort)Lerp(minDuration, maxDuration, t),
			pain
		);
	}

	public static float PainFraction(float pain, float startsAtPain, float painForMaximum)
	{
		if (painForMaximum <= startsAtPain)
			return 1f;
		return Math.Clamp((pain - startsAtPain) / (painForMaximum - startsAtPain), 0f, 1f);
	}

	private static (int Min, int Max) Range(int min, int max, int lower, int upper)
	{
		min = Math.Clamp(min, lower, upper);
		max = Math.Clamp(max, lower, upper);
		return max < min ? (min, min) : (min, max);
	}

	private static int Lerp(int from, int to, float t) => (int)MathF.Round(from + (to - from) * t);
}
