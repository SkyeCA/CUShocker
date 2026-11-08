using CUShocker.Module.Logic;
using CUShocker.Protocol;
using OpenShock.Desktop.ModuleBase.Config;

namespace CUShocker.Module.Services;

public enum StateSource
{
	Game,
	Simulator,
}

public sealed class PainProcessor
{
	private readonly IModuleConfig<CUShockerConfig> _config;
	private readonly ControlSender _sender;

	private readonly Lock _lock = new();
	private readonly PainTrigger _trigger = new();
	private bool _useSimulator;

	public PainState? LastGameState { get; private set; }
	public DateTimeOffset? LastGameMessageAt { get; private set; }
	public PainState? LastSimulatorState { get; private set; }
	public ControlDecision? LastDecision { get; private set; }

	public event Action? StateChanged;

	public PainProcessor(IModuleConfig<CUShockerConfig> config, ControlSender sender)
	{
		_config = config;
		_sender = sender;
	}

	public bool UseSimulator
	{
		get => _useSimulator;
		set
		{
			lock (_lock)
			{
				if (_useSimulator == value)
					return;
				_useSimulator = value;
				_trigger.Reset();
			}
			StateChanged?.Invoke();
		}
	}

	public bool GameConnected =>
		LastGameMessageAt is { } at &&
		DateTimeOffset.Now - at < TimeSpan.FromMilliseconds(ProtocolInfo.ConnectionTimeoutMs);

	public void Handle(PainState state, StateSource source)
	{
		ControlDecision? decision = null;
		var config = _config.Config;

		lock (_lock)
		{
			if (source == StateSource.Game)
			{
				LastGameState = state;
				LastGameMessageAt = DateTimeOffset.Now;
			}
			else
			{
				LastSimulatorState = state;
			}

			bool active = (source == StateSource.Simulator) == _useSimulator;
			if (active && _trigger.ShouldFire(state, config.Trigger, config.MinPainChange))
			{
				decision = PainRules.Choose(config, state.AveragePain);
				if (decision is not null)
					LastDecision = decision;
			}
		}

		if (decision is { } command && config.Output != OutputMode.Off)
			_sender.Submit(command);

		StateChanged?.Invoke();
	}
}
