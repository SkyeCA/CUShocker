using System.Net;
using System.Net.Sockets;
using CUShocker.Protocol;
using Microsoft.Extensions.Logging;

namespace CUShocker.Module.Services;

public sealed class GameListener : IAsyncDisposable
{
	private readonly PainProcessor _processor;
	private readonly ILogger<GameListener> _log;

	private CancellationTokenSource? _cts;
	private Task? _loop;
	private string? _session;
	private long _lastSeq;

	public int Port { get; private set; }
	public bool IsListening { get; private set; }
	public string? LastError { get; private set; }
	public long ReceivedCount { get; private set; }
	public long RejectedCount { get; private set; }
	public long StaleCount { get; private set; }

	public event Action? StateChanged;

	public GameListener(PainProcessor processor, ILogger<GameListener> log)
	{
		_processor = processor;
		_log = log;
	}

	public void Start(int port)
	{
		if (_loop is { IsCompleted: false })
			return;

		Port = port;
		_cts = new CancellationTokenSource();
		var token = _cts.Token;
		_loop = Task.Run(() => RunAsync(port, token));
	}

	public async Task RestartAsync(int port)
	{
		await StopAsync();
		Start(port);
	}

	public async Task StopAsync()
	{
		if (_cts is null)
			return;
		_cts.Cancel();
		try
		{
			if (_loop is not null)
				await _loop;
		}
		catch (OperationCanceledException)
		{
		}
		finally
		{
			_cts.Dispose();
			_cts = null;
			_loop = null;
		}
	}

	private async Task RunAsync(int port, CancellationToken ct)
	{
		var buffer = new byte[4096];
		try
		{
			using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
			IsListening = true;
			LastError = null;
			_log.LogInformation("Listening for the game on {Host}:{Port}", ProtocolInfo.Host, port);
			StateChanged?.Invoke();

			while (!ct.IsCancellationRequested)
			{
				int received;
				try
				{
					received = await socket.ReceiveAsync(buffer, SocketFlags.None, ct);
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset)
				{
					continue; // ICMP port unreachable on windows, ignore
				}

				ReceivedCount++;
				if (!PainState.TryParse(buffer.AsSpan(0, received), out var state))
				{
					if (RejectedCount++ == 0)
						_log.LogWarning("Got a message that isn't protocol v{Version}, ignoring", ProtocolInfo.Version);
					continue;
				}

				if (state.Session == _session && state.Seq <= _lastSeq)
				{
					StaleCount++;
					continue;
				}
				if (state.Session != _session)
					_log.LogInformation("Game session {Session} connected", state.Session);
				_session = state.Session;
				_lastSeq = state.Seq;

				_processor.Handle(state, StateSource.Game);
			}
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			LastError = ex is SocketException { SocketErrorCode: SocketError.AddressAlreadyInUse }
				? $"Port {port} is already in use by another program"
				: ex.Message;
			_log.LogError(ex, "Game listener stopped");
		}
		finally
		{
			IsListening = false;
			StateChanged?.Invoke();
		}
	}

	public async ValueTask DisposeAsync() => await StopAsync();
}
