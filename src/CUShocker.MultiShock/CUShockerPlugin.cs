using CUShocker.Core;
using CUShocker.Core.Services;
using CUShocker.MultiShock.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MultiShock.PluginSdk;

namespace CUShocker.MultiShock;

public sealed class CUShockerPlugin : IPlugin, IPluginRouteProvider
{
	public const string PluginId = "my.vore.cushocker";

	public string Id => PluginId;
	public string Name => "CUShocker";
	public string Version => typeof(CUShockerPlugin).Assembly.GetName().Version!.ToString(3);
	public string Description => "Shocks you when your Expie gets hurt in Casualties Unknown";
	public string SourceUrl => "https://www.nexusmods.com/scavprototype/mods/879";

	public void ConfigureServices(IServiceCollection services)
	{
		services.AddSingleton(sp => new MultiShockConfigStore(
			Path.Combine(sp.GetRequiredService<IPluginHost>().GetPluginDataPath(PluginId), "config.json"),
			MultiShockOutput.Limits,
			sp.GetRequiredService<ILogger<MultiShockConfigStore>>()
		));
		services.AddSingleton(sp => sp.GetRequiredService<MultiShockConfigStore>().Config);
		services.AddSingleton<CUShockerSettings>(sp => sp.GetRequiredService<MultiShockConfig>());
		services.AddSingleton(MultiShockOutput.Limits);
		services.AddSingleton<IShockerOutput, MultiShockOutput>();
		services.AddSingleton<ControlSender>();
		services.AddSingleton<PainProcessor>();
		services.AddSingleton<PainSimulator>();
		services.AddSingleton<GameListener>();
	}

	public void Initialize(IServiceProvider services)
	{
		var settings = services.GetRequiredService<CUShockerSettings>();
		services.GetRequiredService<GameListener>().Start(settings.UdpPort);
	}

	public IEnumerable<NavigationItem> GetNavigationItems() =>
	[
		new NavigationItem { Text = "CUShocker", Href = "/cushocker", Icon = "zap" },
	];
}
