using CUShocker.Protocol;

namespace CUShocker.Module.Logic;

// Compares pain against the pain at the last command. With IncreasesOnly the baseline also
// follows pain down, so getting hurt again while recovering still fires.
public sealed class PainTrigger
{
	public const float SmallestPainChange = 0.1f;

	private string? _session;
	private float? _baseline;

	public void Reset()
	{
		_session = null;
		_baseline = null;
	}

	public bool ShouldFire(PainState state, TriggerFilter filter, float minPainChange)
	{
		if (state.Session != _session)
		{
			_session = state.Session;
			_baseline = null;
		}

		if (!state.InWorld || !state.Alive)
		{
			_baseline = null;
			return false;
		}

		float pain = state.AveragePain;
		if (float.IsNaN(pain))
			return false;

		// first reading just sets the baseline so loading in hurt doesn't fire
		if (_baseline is not { } baseline)
		{
			_baseline = pain;
			return false;
		}

		float threshold = Math.Max(minPainChange, SmallestPainChange);
		float change = pain - baseline;

		if (filter == TriggerFilter.IncreasesOnly && change < 0)
		{
			_baseline = pain;
			return false;
		}

		if (Math.Abs(change) < threshold)
			return false;

		_baseline = pain;
		return true;
	}
}
