using CUShocker.Module.Logic;
using Microsoft.Extensions.Logging;
using OpenShock.Desktop.ModuleBase.Api;
using OpenShock.Desktop.ModuleBase.Config;
using OpenShock.Desktop.ModuleBase.Models;

namespace CUShocker.Module.Services;

public sealed record SentControl(ControlDecision Decision, int ShockerCount, bool DryRun, DateTimeOffset At);

public sealed class ControlSender
{
	private readonly IModuleConfig<CUShockerConfig> _config;
	private readonly IOpenShockControl _control;
	private readonly ILogger<ControlSender> _log;

	private readonly Lock _lock = new();
	private ControlDecision? _pending;
	private bool _sending;

	public SentControl? LastSent { get; private set; }
	public string? LastError { get; private set; }

	public event Action? StateChanged;

	public ControlSender(IModuleConfig<CUShockerConfig> config, IOpenShockControl control, ILogger<ControlSender> log)
	{
		_config = config;
		_control = control;
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
		var config = _config.Config;
		var shockerIds = config.ShockerIds.ToList();
		if (shockerIds.Count == 0)
		{
			LastError = "No shockers selected";
			return;
		}

		bool dryRun = config.Output != OutputMode.Live;
		try
		{
			if (!dryRun)
			{
				var shocks = shockerIds.Select(id => new ShockerControl
				{
					Id = id,
					Type = decision.Type,
					Intensity = decision.Intensity,
					Duration = decision.DurationMs,
					Exclusive = true,
				});
				await _control.Control(shocks, "CUShocker");
			}

			_log.LogInformation("{Mode} {Type} {Intensity}% {Duration}ms, pain {Pain:0.#}",
				dryRun ? "Test" : "Sent", decision.Type, decision.Intensity, decision.DurationMs, decision.Pain);
			LastSent = new SentControl(decision, shockerIds.Count, dryRun, DateTimeOffset.Now);
			LastError = null;
		}
		catch (Exception ex)
		{
			LastError = ex.Message;
			_log.LogError(ex, "Failed to send {Type}", decision.Type);
		}
	}
}
