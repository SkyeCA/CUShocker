using CUShocker.Module;
using CUShocker.Module.Logic;
using OpenShock.Desktop.ModuleBase.Models;

namespace CUShocker.Tests;

public class PainRulesTests
{
	// test with vibrate on regardless of the featureflag
	private static ControlDecision? Choose(CUShockerConfig config, float pain) =>
		PainRules.Choose(config, pain, includeVibrate: true);

	[Fact]
	public void VibrateNeverFiresWhileTheFeatureIsOff()
	{
		var config = Config();
		config.Shock.Enabled = false;

		Assert.Null(PainRules.Choose(config, 30, includeVibrate: false));
		Assert.Equal(Features.Vibrate ? ControlType.Vibrate : null, PainRules.Choose(config, 30)?.Type);
	}

	private static CUShockerConfig Config()
	{
		var config = new CUShockerConfig { PainForMaximum = 100 };
		config.Vibrate = new ModeConfig { Enabled = true, StartsAtPain = 10, MinIntensity = 20, MaxIntensity = 60, MinDurationMs = 300, MaxDurationMs = 1000 };
		config.Shock = new ModeConfig { Enabled = true, StartsAtPain = 50, MinIntensity = 5, MaxIntensity = 25, MinDurationMs = 300, MaxDurationMs = 300 };
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
	[InlineData(0.5f, null)]
	[InlineData(9.9f, null)]
	[InlineData(10f, ControlType.Vibrate)]
	[InlineData(49f, ControlType.Vibrate)]
	[InlineData(50f, ControlType.Shock)]
	[InlineData(500f, ControlType.Shock)]
	public void HighestThresholdReachedWins(float pain, ControlType? expected)
	{
		Assert.Equal(expected, Choose(Config(), pain)?.Type);
	}

	[Fact]
	public void DisabledModesAreSkipped()
	{
		var config = Config();
		config.Shock.Enabled = false;

		Assert.Equal(ControlType.Vibrate, Choose(config, 80)?.Type);
	}

	[Fact]
	public void TiesPreferShock()
	{
		var config = Config();
		config.Vibrate.StartsAtPain = config.Shock.StartsAtPain = 1;

		Assert.Equal(ControlType.Shock, Choose(config, 5)?.Type);
		config.Shock.Enabled = false;
		Assert.Equal(ControlType.Vibrate, Choose(config, 5)?.Type);
	}

	[Fact]
	public void ScalesFromModeThresholdToPainAtMaximum()
	{
		var config = Config();
		config.Shock.Enabled = false;

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
		var config = Config();
		config.Vibrate = new ModeConfig { Enabled = true, StartsAtPain = 1, MinIntensity = 0, MaxIntensity = 400, MinDurationMs = 50, MaxDurationMs = 90000 };
		config.Shock.Enabled = false;

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
