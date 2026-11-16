using CUShocker.Core.Logic;

namespace CUShocker.Core.Services;

public interface IShockerOutput
{
	int ShockerCount { get; }

	Task Send(ControlDecision decision);
}
