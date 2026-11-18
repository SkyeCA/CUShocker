using CUShocker.Core;

namespace CUShocker.OpenShock;

public sealed class CUShockerConfig : CUShockerSettings
{
	public List<Guid> ShockerIds { get; set; } = [];
}
