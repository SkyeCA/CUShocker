using CUShocker.Protocol;

namespace CUShocker.Core.Services;

public sealed class PainSimulator
{
	private readonly PainProcessor _processor;
	private readonly CUShockerSettings _settings;
	private readonly string _session = "simulator-" + Guid.NewGuid().ToString("N");
	private long _seq;

	public float Pain { get; private set; }
	public bool Alive { get; private set; } = true;

	public PainSimulator(PainProcessor processor, CUShockerSettings settings)
	{
		_processor = processor;
		_settings = settings;
	}

	public float MaxPain => Math.Max(100f, _settings.PainForMaximum);

	public bool Enabled
	{
		get => _processor.UseSimulator;
		set
		{
			_processor.UseSimulator = value;
			if (value)
				Send();
		}
	}

	public void SetPain(float pain)
	{
		Pain = pain;
		Send();
	}

	public void SetAlive(bool alive)
	{
		Alive = alive;
		Send();
	}

	private void Send() => _processor.Handle(new PainState
	{
		Session = _session,
		Seq = ++_seq,
		InWorld = true,
		Alive = Alive,
		Conscious = Alive,
		AveragePain = Pain,
	}, StateSource.Simulator);
}
