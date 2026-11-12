using CUShocker.Module;
using CUShocker.Module.Logic;
using OpenShock.Desktop.ModuleBase.Models;

namespace CUShocker.Tests;

public class PainRulesTests
{
	private static ControlDecision? Choose(CUShockerConfig config, float pain) => PainRules.Choose(config, pain);

	private static CUShockerConfig Config(ControlMode mode = ControlMode.Shock)
	{
		var config = new CUShockerConfig { PainForMaximum = 100, Mode = mode };
		config.Vibrate = new ModeConfig { StartsAtPain = 10, MinIntensity = 20, MaxIntensity = 60, MinDurationMs = 300, MaxDurationMs = 1000 };
		config.Shock = new ModeConfig { StartsAtPain = 50, MinIntensity = 5, MaxIntensity = 25, MinDurationMs = 300, MaxDurationMs = 300 };
		return config;
	}

	[Theory]
	[InlineData(0f)]
	[InlineData(-5f)]
	[InlineData(float.NaN)]
	public void NoPainSendsNothing(float pain)
	{
		Assert.Null(Choose(Config(), pain));
	}

	[Theory]
	[InlineData(ControlMode.Shock, ControlType.Shock)]
	[InlineData(ControlMode.Vibrate, ControlType.Vibrate)]
	public void SelectedModeIsAlwaysUsed(ControlMode mode, ControlType expected)
	{
		var config = Config(mode);

		Assert.Equal(expected, Choose(config, 60)?.Type);
		Assert.Equal(expected, Choose(config, 500)?.Type);
	}

	[Theory]
	[InlineData(ControlMode.Shock, 49.9f, null)]
	[InlineData(ControlMode.Shock, 50f, ControlType.Shock)]
	[InlineData(ControlMode.Vibrate, 9.9f, null)]
	[InlineData(ControlMode.Vibrate, 10f, ControlType.Vibrate)]
	public void NothingBelowTheSelectedModesThreshold(ControlMode mode, float pain, ControlType? expected)
	{
		Assert.Equal(expected, Choose(Config(mode), pain)?.Type);
	}

	[Fact]
	public void UsesOnlyTheSelectedModesSettings()
	{
		var shock = Choose(Config(ControlMode.Shock), 100)!.Value;
		Assert.Equal(25, shock.Intensity);
		Assert.Equal(300, shock.DurationMs);

		var vibrate = Choose(Config(ControlMode.Vibrate), 100)!.Value;
		Assert.Equal(60, vibrate.Intensity);
		Assert.Equal(1000, vibrate.DurationMs);
	}

	[Fact]
	public void ScalesFromModeThresholdToPainAtMaximum()
	{
		var config = Config(ControlMode.Vibrate);

		var atThreshold = Choose(config, 10)!.Value;
		Assert.Equal(20, atThreshold.Intensity);
		Assert.Equal(300, atThreshold.DurationMs);

		var halfway = Choose(config, 55)!.Value; // (55 - 10) / (100 - 10) = 0.5
		Assert.Equal(40, halfway.Intensity);
		Assert.Equal(650, halfway.DurationMs);

		var beyondMaximum = Choose(config, 250)!.Value;
		Assert.Equal(60, beyondMaximum.Intensity);
		Assert.Equal(1000, beyondMaximum.DurationMs);
	}

	[Fact]
	public void ClampsToWhatOpenShockAccepts()
	{
		var config = Config(ControlMode.Vibrate);
		config.Vibrate = new ModeConfig { StartsAtPain = 1, MinIntensity = 0, MaxIntensity = 400, MinDurationMs = 50, MaxDurationMs = 90000 };

		var low = Choose(config, 1)!.Value;
		Assert.Equal(PainRules.MinIntensity, low.Intensity);
		Assert.Equal(PainRules.MinDurationMs, low.DurationMs);

		var high = Choose(config, 100)!.Value;
		Assert.Equal(PainRules.MaxIntensity, high.Intensity);
		Assert.Equal(PainRules.MaxDurationMs, high.DurationMs);
	}

	[Fact]
	public void MaxBelowMinUsesMin()
	{
		var config = Config();
		config.Shock.MinIntensity = 30;
		config.Shock.MaxIntensity = 10;

		Assert.Equal(30, Choose(config, 100)!.Value.Intensity);
	}

	[Fact]
	public void PainAtMaximumBelowThresholdMeansFullStrength()
	{
		var config = Config();
		config.PainForMaximum = 20; // below the shock threshold of 50

		Assert.Equal(25, Choose(config, 60)!.Value.Intensity);
	}
}
