using CUShocker.Core;
using CUShocker.MultiShock;
using CUShocker.MultiShock.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace CUShocker.Tests;

public sealed class MultiShockConfigStoreTests : IDisposable
{
	private readonly string _directory = Path.Combine(Path.GetTempPath(), "cushocker-tests-" + Guid.NewGuid().ToString("N"));

	private string ConfigPath => Path.Combine(_directory, "config.json");

	private MultiShockConfigStore Open() => new(ConfigPath, MultiShockOutput.Limits, NullLogger<MultiShockConfigStore>.Instance);

	[Fact]
	public void StartsInTestModeWithNoShockers()
	{
		var config = Open().Config;

		Assert.Equal(OutputMode.DryRun, config.Output);
		Assert.Empty(config.Shockers);
	}

	[Fact]
	public void SavedSettingsSurviveARestart()
	{
		var store = Open();
		store.Config.Output = OutputMode.Live;
		store.Config.Mode = ControlMode.Vibrate;
		store.Config.Vibrate.MaxIntensity = 77;
		store.Config.UdpPort = 40000;
		store.Config.Shockers.Add(new ShockerRef(29764, 6145));
		store.Save();

		var reloaded = Open().Config;

		Assert.Equal(OutputMode.Live, reloaded.Output);
		Assert.Equal(ControlMode.Vibrate, reloaded.Mode);
		Assert.Equal(77, reloaded.Vibrate.MaxIntensity);
		Assert.Equal(40000, reloaded.UdpPort);
		Assert.Equal([new ShockerRef(29764, 6145)], reloaded.Shockers);
	}

	[Fact]
	public void DefaultDurationsAreRaisedToPiShocksMinimum()
	{
		var config = Open().Config;

		Assert.All([config.Shock, config.Vibrate], mode =>
		{
			Assert.InRange(mode.MinDurationMs, 1000, 15000);
			Assert.InRange(mode.MaxDurationMs, 1000, 15000);
		});
	}

	[Fact]
	public void SavedDurationsOutsidePiShocksRangeAreClamped()
	{
		var store = Open();
		store.Config.Shock.MinDurationMs = 300;
		store.Config.Shock.MaxDurationMs = 30000;
		store.Save();

		var shock = Open().Config.Shock;

		Assert.Equal(1000, shock.MinDurationMs);
		Assert.Equal(15000, shock.MaxDurationMs);
	}

	[Fact]
	public void UnreadableConfigFallsBackToDefaults()
	{
		Directory.CreateDirectory(_directory);
		File.WriteAllText(ConfigPath, "{ not json");

		Assert.Equal(OutputMode.DryRun, Open().Config.Output);
	}

	public void Dispose()
	{
		if (Directory.Exists(_directory))
			Directory.Delete(_directory, true);
	}
}
