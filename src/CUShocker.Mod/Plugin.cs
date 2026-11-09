using BepInEx;
using BepInEx.Logging;
using CUShocker.Protocol;
using UnityEngine;

namespace CUShocker.Mod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public sealed class Plugin : BaseUnityPlugin
{
	internal static new ManualLogSource Logger = null!;

	private void Awake()
	{
		Logger = base.Logger;
		var port = Config.Bind("Connection", "Port", ProtocolInfo.DefaultPort, "UDP port of the CUShocker module in OpenShock Desktop");
		var host = new GameObject("CUShocker") { hideFlags = HideFlags.HideAndDontSave };
		DontDestroyOnLoad(host);
		host.AddComponent<PainReporter>().Init(port.Value);

		Logger.LogInfo($"Sending to {ProtocolInfo.Host}:{port.Value}");
	}
}
