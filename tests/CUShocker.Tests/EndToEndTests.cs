using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using CUShocker.Core;
using CUShocker.Core.Logic;
using CUShocker.Core.Services;
using CUShocker.Protocol;
using Microsoft.Extensions.Logging.Abstractions;

namespace CUShocker.Tests;

public sealed class EndToEndTests : IAsyncLifetime
{
	private readonly CUShockerSettings _settings = new() { Output = OutputMode.Live };
	private readonly ControlLimits _limits = new(MinIntensity: 1, MaxIntensity: 100, MinDurationMs: 300, MaxDurationMs: 30000);
	private readonly RecordingOutput _output = new();
	private readonly GameListener _listener;
	private readonly PainProcessor _processor;
	private readonly UdpClient _game = new();
	private readonly int _port = FreeUdpPort();
	private long _seq;

	public EndToEndTests()
	{
		var sender = new ControlSender(_settings, _output, NullLogger<ControlSender>.Instance);
		_processor = new PainProcessor(_settings, _limits, sender);
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

		await WaitUntil(() => !_output.Sent.IsEmpty);
		var sent = Assert.Single(_output.Sent);
		Assert.Equal(ControlMode.Shock, sent.Mode);
		Assert.Equal(40f, sent.Pain);
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
		Assert.True(_output.Sent.IsEmpty);
	}

	[Fact]
	public async Task DryRunSendsNothing()
	{
		_settings.Output = OutputMode.DryRun;
		await SendPain(0);
		await SendPain(40);

		await WaitUntil(() => _processor.LastDecision is not null);
		await Task.Delay(200);
		Assert.True(_output.Sent.IsEmpty);
	}

	[Fact]
	public async Task NoShockersSelectedIsReported()
	{
		_output.ShockerCount = 0;
		var sender = new ControlSender(_settings, _output, NullLogger<ControlSender>.Instance);

		sender.Submit(new ControlDecision(ControlMode.Shock, 10, 300, 40));

		await WaitUntil(() => sender.LastError is not null);
		Assert.Equal("No shockers selected", sender.LastError);
		Assert.True(_output.Sent.IsEmpty);
	}

	[Fact]
	public async Task SimulatorReplacesTheGameWhileOn()
	{
		var simulator = new PainSimulator(_processor, _settings) { Enabled = true };
		await SendPain(0);
		await SendPain(40);
		await WaitUntil(() => _processor.LastGameState?.AveragePain == 40f);

		simulator.SetPain(70);

		await WaitUntil(() => !_output.Sent.IsEmpty);
		await Task.Delay(200);
		var only = Assert.Single(_output.Sent);
		Assert.Equal(70f, only.Pain);
		Assert.Equal(70f, _processor.ActiveState?.AveragePain);
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

	private sealed class RecordingOutput : IShockerOutput
	{
		public ConcurrentQueue<ControlDecision> Sent { get; } = new();

		public int ShockerCount { get; set; } = 1;

		public Task Send(ControlDecision decision)
		{
			Sent.Enqueue(decision);
			return Task.CompletedTask;
		}
	}
}
