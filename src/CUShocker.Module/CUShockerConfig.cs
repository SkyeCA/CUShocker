using CUShocker.Protocol;

namespace CUShocker.Module;

/// <summary>Saved by OpenShock Desktop as JSON in the module's data folder.</summary>
public sealed class CUShockerConfig
{
	/// <summary>The UDP port the game mod sends to. Must match the mod's setting.</summary>
	public int UdpPort { get; set; } = ProtocolInfo.DefaultPort;
}
