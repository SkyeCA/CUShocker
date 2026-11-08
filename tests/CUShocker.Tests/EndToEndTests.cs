using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using CUShocker.Module;
using CUShocker.Module.Services;
using CUShocker.Protocol;
using Microsoft.Extensions.Logging.Abstractions;
using OpenShock.Desktop.ModuleBase.Api;
using OpenShock.Desktop.ModuleBase.Config;
using OpenShock.Desktop.ModuleBase.Models;
using OpenShock.MinimalEvents;

namespace CUShocker.Tests;

public sealed class EndToEndTests : IAsyncLifetime
{
	private static readonly Guid ShockerId = Guid.NewGuid();

	private readonly FakeConfig _config = new();
	private readonly FakeControl _control = new();
	private readonly GameListener _listener;
	private readonly PainProcessor _processor;
	private readonly UdpClient _game = new();
	private readonly int _port = FreeUdpPort();
	private long _seq;

	public EndToEndTests()
	{
		_config.Config.ShockerIds.Add(ShockerId);
		_config.Config.Output = OutputMode.Live;
		var sender = new ControlSender(_config, _control, NullLogger<ControlSender>.Instance);
		_processor = new PainProcessor(_config, sender);
		_listener = new GameListener(_processor, NullLogger<GameListener>.Instance);
	}

	public async Task InitializeAsync()
	{
		_listener.Start(_port);
		await WaitUntil(() => _listener.IsListening);
	}

	public async Task DisposeAsync()
	{
		_game.Dispose();
		await _listener.DisposeAsync();
	}

	private async Task SendPain(float pain, string session = "game", long? seq = null)
	{
		var state = new PainState
		{
			Session = session,
			Seq = seq ?? ++_seq,
			InWorld = true,
			Alive = true,
			Conscious = true,
			AveragePain = pain,
		};
		var bytes = state.ToUtf8Bytes();
		await _game.SendAsync(bytes, bytes.Length, new IPEndPoint(IPAddress.Loopback, _port));
	}

	[Fact]
	public async Task PainFromTheGameReachesTheShockers()
	{
		await SendPain(0);
		await SendPain(40);

		await WaitUntil(() => !_control.Sent.IsEmpty);
		var shock = Assert.Single(Assert.Single(_control.Sent));
		Assert.Equal(ShockerId, shock.Id);
		Assert.Equal(ControlType.Shock, shock.Type);
		Assert.True(shock.Exclusive);
		Assert.True(_processor.GameConnected);
		Assert.Equal(40f, _processor.LastGameState?.AveragePain);
	}

	[Fact]
	public async Task StaleAndMalformedDatagramsAreDropped()
	{
		await SendPain(0, seq: 10);
		await SendPain(40, seq: 5); // stale
		await _game.SendAsync([1, 2, 3], 3, new IPEndPoint(IPAddress.Loopback, _port));
		await SendPain(0.5f, seq: 11);

		await WaitUntil(() => _listener.ReceivedCount == 4);
		Assert.Equal(1, _listener.StaleCount);
		Assert.Equal(1, _listener.RejectedCount);
		Assert.Equal(0.5f, _processor.LastGameState?.AveragePain);
		Assert.True(_control.Sent.IsEmpty);
	}

	[Fact]
	public async Task DryRunSendsNothing()
	{
		_config.Config.Output = OutputMode.DryRun;
		await SendPain(0);
		await SendPain(40);

		await WaitUntil(() => _processor.LastDecision is not null);
		await Task.Delay(200);
		Assert.True(_control.Sent.IsEmpty);
	}

	[Fact]
	public async Task SimulatorReplacesTheGameWhileOn()
	{
		_processor.UseSimulator = true;
		await SendPain(0);
		await SendPain(40);
		await WaitUntil(() => _processor.LastGameState?.AveragePain == 40f);

		_processor.Handle(new PainState { Session = "sim", Seq = 1, InWorld = true, Alive = true, AveragePain = 0 }, StateSource.Simulator);
		_processor.Handle(new PainState { Session = "sim", Seq = 2, InWorld = true, Alive = true, AveragePain = 70 }, StateSource.Simulator);

		await WaitUntil(() => !_control.Sent.IsEmpty);
		await Task.Delay(200);
		var only = Assert.Single(_control.Sent);
		Assert.Equal(70f, _processor.LastDecision?.Pain);
		Assert.Single(only);
	}

	private static async Task WaitUntil(Func<bool> condition)
	{
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (!condition())
		{
			if (DateTime.UtcNow > deadline)
				throw new TimeoutException("Condition not met within 5 seconds");
			await Task.Delay(10);
		}
	}

	private static int FreeUdpPort()
	{
		using var probe = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
		return ((IPEndPoint)probe.Client.LocalEndPoint!).Port;
	}

	private sealed class FakeConfig : IModuleConfig<CUShockerConfig>
	{
		public CUShockerConfig Config { get; } = new();
		public void SaveDeferred() { }
		public Task Save() => Task.CompletedTask;
	}

	private sealed class FakeControl : IOpenShockControl
	{
		public ConcurrentQueue<List<ShockerControl>> Sent { get; } = new();

		public Task Control(IEnumerable<ShockerControl> shocks, string? customName = null)
		{
			Sent.Enqueue(shocks.ToList());
			return Task.CompletedTask;
		}

		public void LiveControl(IEnumerable<Guid> shockers, byte intensity, ControlType type) => throw new NotSupportedException();
		public void ControlAllShockers(byte intensity, ControlType type) => throw new NotSupportedException();
		public Task ControlAll(ushort duration, byte intensity, ControlType type, bool exclusive = false, string? customName = null) => throw new NotSupportedException();
		public IAsyncMinimalEventObservable<RemoteControlledShockerArgs> OnRemoteControlledShocker => throw new NotSupportedException();
		public IAsyncMinimalEventObservable<RemoteControlledShockerArgs> OnLocalControlledShocker => throw new NotSupportedException();
	}
}
