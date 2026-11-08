using BepInEx;
using BepInEx.Logging;
using CUShocker.Protocol;

namespace CUShocker.Mod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public sealed class Plugin : BaseUnityPlugin
{
	internal static new ManualLogSource Logger = null!;

	private void Awake()
	{
		Logger = base.Logger;
		Logger.LogInfo(
			$"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION} loaded " +
			$"(protocol v{ProtocolInfo.Version}, {ProtocolInfo.Host}:{ProtocolInfo.DefaultPort})"
		);
	}
}
