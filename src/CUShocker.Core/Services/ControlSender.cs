using CUShocker.Core.Logic;
using Microsoft.Extensions.Logging;

namespace CUShocker.Core.Services;

public sealed record SentControl(ControlDecision Decision, int ShockerCount, bool DryRun, DateTimeOffset At);

public sealed class ControlSender
{
	private readonly CUShockerSettings _settings;
	private readonly IShockerOutput _output;
	private readonly ILogger<ControlSender> _log;

	private readonly Lock _lock = new();
	private ControlDecision? _pending;
	private bool _sending;

	public SentControl? LastSent { get; private set; }
	public string? LastError { get; private set; }

	public event Action? StateChanged;

	public ControlSender(CUShockerSettings settings, IShockerOutput output, ILogger<ControlSender> log)
	{
		_settings = settings;
		_output = output;
		_log = log;
	}

	public void Submit(ControlDecision decision)
	{
		lock (_lock)
		{
			_pending = decision;
			if (_sending)
				return;
			_sending = true;
		}
		_ = Task.Run(SendPendingAsync);
	}

	private async Task SendPendingAsync()
	{
		while (true)
		{
			ControlDecision decision;
			lock (_lock)
			{
				if (_pending is not { } next)
				{
					_sending = false;
					return;
				}
				decision = next;
				_pending = null;
			}
			await SendAsync(decision);
			StateChanged?.Invoke();
		}
	}

	private async Task SendAsync(ControlDecision decision)
	{
		int shockerCount = _output.ShockerCount;
		if (shockerCount == 0)
		{
			LastError = "No shockers selected";
			return;
		}

		bool dryRun = _settings.Output != OutputMode.Live;
		var at = DateTimeOffset.Now;
		try
		{
			if (!dryRun)
				await _output.Send(decision);

			_log.LogInformation("{Mode} {Type} {Intensity}% {Duration}ms, pain {Pain:0.#}",
				dryRun ? "Test" : "Sent", decision.Mode, decision.Intensity, decision.DurationMs, decision.Pain);
			LastSent = new SentControl(decision, shockerCount, dryRun, at);
			LastError = null;
		}
		catch (Exception ex)
		{
			LastError = ex.Message;
			_log.LogError(ex, "Failed to send {Type}", decision.Mode);
		}
	}
}
