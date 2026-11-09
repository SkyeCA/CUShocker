using OpenShock.Desktop.ModuleBase.Models;

namespace CUShocker.Module.Logic;

public readonly record struct ControlDecision(ControlType Type, byte Intensity, ushort DurationMs, float Pain);

public static class PainRules
{
	public const int MinIntensity = 1;
	public const int MaxIntensity = 100;
	public const int MinDurationMs = 300;
	public const int MaxDurationMs = 30000;

	public static ControlDecision? Choose(CUShockerConfig config, float pain) =>
		Choose(config, pain, Features.Vibrate);

	public static ControlDecision? Choose(CUShockerConfig config, float pain, bool includeVibrate)
	{
		if (!(pain > 0))
			return null;

		ControlType? bestType = null;
		ModeConfig? best = null;
		foreach (var (type, mode) in Modes(config, includeVibrate))
		{
			if (!mode.Enabled || pain < mode.StartsAtPain)
				continue;
			if (best is null || mode.StartsAtPain > best.StartsAtPain)
			{
				bestType = type;
				best = mode;
			}
		}
		if (best is null || bestType is null)
			return null;

		float t = PainFraction(pain, best.StartsAtPain, config.PainForMaximum);
		var (minIntensity, maxIntensity) = Range(best.MinIntensity, best.MaxIntensity, MinIntensity, MaxIntensity);
		var (minDuration, maxDuration) = Range(best.MinDurationMs, best.MaxDurationMs, MinDurationMs, MaxDurationMs);

		return new ControlDecision(
			bestType.Value,
			(byte)Lerp(minIntensity, maxIntensity, t),
			(ushort)Lerp(minDuration, maxDuration, t),
			pain
		);
	}

	private static IEnumerable<(ControlType Type, ModeConfig Mode)> Modes(CUShockerConfig config, bool includeVibrate)
	{
		yield return (ControlType.Shock, config.Shock);
		if (includeVibrate)
			yield return (ControlType.Vibrate, config.Vibrate);
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
