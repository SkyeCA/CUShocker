using CUShocker.Core;

namespace CUShocker.Module;

public sealed class CUShockerConfig : CUShockerSettings
{
	public List<Guid> ShockerIds { get; set; } = [];
}
