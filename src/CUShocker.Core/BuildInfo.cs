namespace CUShocker.Core;

public static class BuildInfo
{
	// readonly instead of const to avoid unreachable code warnings
	public static readonly bool HasSimulator =
#if SIMULATOR
		true;
#else
		false;
#endif
}
