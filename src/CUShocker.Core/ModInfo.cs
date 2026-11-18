namespace CUShocker.Core;

public static class ModInfo
{
	public const string Name = "CUShocker";
	public const string Author = "SkyeCA";
	public const string AuthorUrl = "https://vore.my";
	public const string GitHubUrl = "https://github.com/SkyeCA/CUShocker";

	public static readonly string Version = typeof(ModInfo).Assembly.GetName().Version!.ToString(3);
}
