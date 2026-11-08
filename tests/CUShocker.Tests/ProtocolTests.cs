using System.Globalization;
using System.Text;
using CUShocker.Protocol;

namespace CUShocker.Tests;

public class ProtocolTests
{
	private static PainState Sample() => new()
	{
		Session = "3f2a9c",
		Seq = 42,
		InWorld = true,
		Alive = true,
		Conscious = false,
		AveragePain = 12.5f,
	};

	[Fact]
	public void RoundTripsThroughJson()
	{
		var sent = Sample();

		Assert.True(PainState.TryParse(sent.ToUtf8Bytes(), out var received));
		Assert.Equal(ProtocolInfo.Version, received.V);
		Assert.Equal(MessageTypes.State, received.Type);
		Assert.Equal("3f2a9c", received.Session);
		Assert.Equal(42, received.Seq);
		Assert.True(received.InWorld);
		Assert.True(received.Alive);
		Assert.False(received.Conscious);
		Assert.Equal(12.5f, received.AveragePain);
	}

	[Fact]
	public void WritesCamelCaseJson()
	{
		Assert.Equal(
			"{\"v\":1,\"type\":\"state\",\"session\":\"3f2a9c\",\"seq\":42,\"inWorld\":true,\"alive\":true,\"conscious\":false,\"averagePain\":12.5}",
			Sample().ToJson()
		);
	}

	[Fact]
	public void UsesInvariantNumberFormat()
	{
		// The mod runs inside the game, which may be on a locale with a comma decimal separator
		var previous = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = new CultureInfo("de-DE");
			var state = Sample();
			state.AveragePain = 0.25f;
			Assert.Contains("\"averagePain\":0.25", state.ToJson());
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}
	}

	[Theory]
	[InlineData(float.NaN)]
	[InlineData(float.PositiveInfinity)]
	public void WritesNonFiniteNumbersAsZero(float pain)
	{
		var state = Sample();
		state.AveragePain = pain;

		Assert.True(PainState.TryParse(state.ToUtf8Bytes(), out var received));
		Assert.Equal(0f, received.AveragePain);
	}

	[Fact]
	public void EscapesStrings()
	{
		var state = Sample();
		state.Session = "a\"b\\c\n";

		Assert.True(PainState.TryParse(state.ToUtf8Bytes(), out var received));
		Assert.Equal("a\"b\\c\n", received.Session);
	}

	[Theory]
	[InlineData("")]
	[InlineData("not json")]
	[InlineData("{\"v\":1,\"type\":\"state\"")]
	[InlineData("{\"v\":2,\"type\":\"state\"}")]
	[InlineData("{\"v\":1,\"type\":\"something-else\"}")]
	public void RejectsMalformedOrOtherVersions(string datagram)
	{
		Assert.False(PainState.TryParse(Encoding.UTF8.GetBytes(datagram), out _));
	}
}
