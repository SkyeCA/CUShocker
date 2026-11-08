namespace CUShocker.Module;

public static class BuildInfo
{
	// readonly instead of const to avoid unreachable code warnings
	public static readonly bool IsDevBuild =
#if DEBUG
		true;
#else
		false;
#endif
}
