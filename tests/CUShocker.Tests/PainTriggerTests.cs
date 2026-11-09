using CUShocker.Module;
using CUShocker.Module.Logic;
using CUShocker.Protocol;

namespace CUShocker.Tests;

public class PainTriggerTests
{
	private readonly PainTrigger _trigger = new();
	private long _seq;

	private bool Feed(float pain, TriggerFilter filter = TriggerFilter.IncreasesOnly, float minChange = 1,
		bool inWorld = true, bool alive = true, string session = "a") =>
		_trigger.ShouldFire(new PainState
		{
			Session = session,
			Seq = ++_seq,
			InWorld = inWorld,
			Alive = alive,
			Conscious = alive,
			AveragePain = pain,
		}, filter, minChange);

	[Fact]
	public void FirstReadingOnlySetsBaseline()
	{
		Assert.False(Feed(30));
		Assert.False(Feed(30.5f));
		Assert.True(Feed(31));
	}

	[Fact]
	public void RepeatedHeartbeatsDoNotFire()
	{
		Feed(0);
		Assert.True(Feed(10));
		Assert.False(Feed(10));
		Assert.False(Feed(10));
	}

	[Fact]
	public void SmallStepsAddUpAgainstTheLastCommand()
	{
		Feed(0);
		Assert.False(Feed(0.4f));
		Assert.False(Feed(0.8f));
		Assert.True(Feed(1.2f));
	}

	[Fact]
	public void IncreasesOnlyIgnoresRecoveryButFiresOnNewInjury()
	{
		Feed(0);
		Assert.True(Feed(20));
		Assert.False(Feed(15));
		Assert.False(Feed(10));
		Assert.True(Feed(12));
	}

	[Fact]
	public void AllChangesFiresWhileRecovering()
	{
		Feed(0, TriggerFilter.AllChanges);
		Assert.True(Feed(20, TriggerFilter.AllChanges));
		Assert.True(Feed(15, TriggerFilter.AllChanges));
		Assert.False(Feed(14.5f, TriggerFilter.AllChanges));
	}

	[Fact]
	public void MinimumChangeHasAFloor()
	{
		Feed(5, minChange: 0);
		Assert.False(Feed(5, minChange: 0));
		Assert.True(Feed(5.2f, minChange: 0));
	}

	[Fact]
	public void MenusAndDeathNeverFireAndResetTheBaseline()
	{
		Feed(0);
		Assert.False(Feed(50, inWorld: false));
		Assert.False(Feed(50, alive: false));
		Assert.False(Feed(50));
		Assert.True(Feed(52));
	}

	[Fact]
	public void NewSessionStartsFresh()
	{
		Feed(0, session: "a");
		Assert.True(Feed(10, session: "a"));
		Assert.False(Feed(40, session: "b"));
		Assert.True(Feed(41, session: "b"));
	}
}
