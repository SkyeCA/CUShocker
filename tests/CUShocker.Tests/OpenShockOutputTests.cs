using CUShocker.Core;
using CUShocker.Core.Logic;
using CUShocker.Module;
using CUShocker.Module.Services;
using OpenShock.Desktop.ModuleBase.Api;
using OpenShock.Desktop.ModuleBase.Config;
using OpenShock.Desktop.ModuleBase.Models;
using OpenShock.MinimalEvents;

namespace CUShocker.Tests;

public class OpenShockOutputTests
{
	private static readonly Guid First = Guid.NewGuid();
	private static readonly Guid Second = Guid.NewGuid();

	private readonly FakeConfig _config = new();
	private readonly FakeControl _control = new();
	private readonly OpenShockOutput _output;

	public OpenShockOutputTests()
	{
		_config.Config.ShockerIds.AddRange([First, Second]);
		_output = new OpenShockOutput(_config, _control);
	}

	[Fact]
	public void CountsSelectedShockers()
	{
		Assert.Equal(2, _output.ShockerCount);
	}

	[Theory]
	[InlineData(ControlMode.Shock, ControlType.Shock)]
	[InlineData(ControlMode.Vibrate, ControlType.Vibrate)]
	public async Task SendsOneExclusiveControlPerSelectedShocker(ControlMode mode, ControlType expected)
	{
		await _output.Send(new ControlDecision(mode, 42, 1500, 60));

		var sent = Assert.Single(_control.Sent);
		Assert.Equal([First, Second], sent.Select(control => control.Id));
		Assert.All(sent, control =>
		{
			Assert.Equal(expected, control.Type);
			Assert.Equal(42, control.Intensity);
			Assert.Equal(1500, control.Duration);
			Assert.True(control.Exclusive);
		});
	}

	private sealed class FakeConfig : IModuleConfig<CUShockerConfig>
	{
		public CUShockerConfig Config { get; } = new();
		public void SaveDeferred() { }
		public Task Save() => Task.CompletedTask;
	}

	private sealed class FakeControl : IOpenShockControl
	{
		public List<List<ShockerControl>> Sent { get; } = [];

		public Task Control(IEnumerable<ShockerControl> shocks, string? customName = null)
		{
			Sent.Add(shocks.ToList());
			return Task.CompletedTask;
		}

		public void LiveControl(IEnumerable<Guid> shockers, byte intensity, ControlType type) => throw new NotSupportedException();
		public void ControlAllShockers(byte intensity, ControlType type) => throw new NotSupportedException();
		public Task ControlAll(ushort duration, byte intensity, ControlType type, bool exclusive = false, string? customName = null) => throw new NotSupportedException();
		public IAsyncMinimalEventObservable<RemoteControlledShockerArgs> OnRemoteControlledShocker => throw new NotSupportedException();
		public IAsyncMinimalEventObservable<RemoteControlledShockerArgs> OnLocalControlledShocker => throw new NotSupportedException();
	}
}
