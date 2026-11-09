using CUShocker.Module;
using CUShocker.Module.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;
using OpenShock.Desktop.ModuleBase;
using OpenShock.Desktop.ModuleBase.Models;
using OpenShock.Desktop.ModuleBase.Navigation;

[assembly: DesktopModule(typeof(CUShockerModule), "my.vore.cushocker", "CUShocker")]
[assembly: RequiredPermission(TokenPermissions.Shockers_Use)]

namespace CUShocker.Module;

public sealed class CUShockerModule : DesktopModuleBase
{
	public override IconOneOf? Icon { get; set; } = IconOneOf.FromSvg(Icons.Material.Filled.Bolt);

	public override Type RootComponent { get; } = typeof(Components.CUShockerPage);

	public override IReadOnlyCollection<NavigationItem> NavigationComponents { get; } = [];

	public override async Task Start()
	{
		var config = await ModuleInstanceManager.GetModuleConfig<CUShockerConfig>();
		var loggerFactory = ModuleInstanceManager.AppServiceProvider.GetRequiredService<ILoggerFactory>();

		var services = new ServiceCollection();
		services.AddSingleton(loggerFactory);
		services.AddLogging();
		services.AddSingleton(config);
		services.AddSingleton(ModuleInstanceManager.OpenShock);
		services.AddSingleton(ModuleInstanceManager.OpenShock.Control);
		services.AddSingleton<ControlSender>();
		services.AddSingleton<PainProcessor>();
		services.AddSingleton<GameListener>();

		ModuleServiceProvider = services.BuildServiceProvider();

		ModuleServiceProvider.GetRequiredService<GameListener>().Start(config.Config.UdpPort);
	}
}
