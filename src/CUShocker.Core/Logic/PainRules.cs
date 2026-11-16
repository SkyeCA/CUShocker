namespace CUShocker.Core.Logic;

public readonly record struct ControlDecision(ControlMode Mode, byte Intensity, ushort DurationMs, float Pain)
{
	public string Describe() => $"{Mode} {Intensity}% for {DurationMs} ms";
}

public sealed record ControlLimits(int MinIntensity, int MaxIntensity, int MinDurationMs, int MaxDurationMs);

public static class PainRules
{
	public static ControlDecision? Choose(CUShockerSettings config, ControlLimits limits, float pain)
	{
		if (!(pain > 0))
			return null;

		var mode = config.ActiveMode;
		if (pain < mode.StartsAtPain)
			return null;

		float t = PainFraction(pain, mode.StartsAtPain, config.PainForMaximum);
		var (minIntensity, maxIntensity) = Range(mode.MinIntensity, mode.MaxIntensity, limits.MinIntensity, limits.MaxIntensity);
		var (minDuration, maxDuration) = Range(mode.MinDurationMs, mode.MaxDurationMs, limits.MinDurationMs, limits.MaxDurationMs);

		return new ControlDecision(
			config.Mode,
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
