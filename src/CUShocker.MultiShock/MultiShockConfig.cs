using CUShocker.Core;

namespace CUShocker.MultiShock;

public sealed class MultiShockConfig : CUShockerSettings
{
	public List<ShockerRef> Shockers { get; set; } = [];
}

public sealed record ShockerRef(int DeviceId, int ShockerId);
