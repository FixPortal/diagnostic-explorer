using AwesomeAssertions;
using DiagnosticExplorer;
using Xunit;

namespace DiagnosticExplorer.UnitTests;

/// <summary>
/// RateCounter's instance side drives a global timer and UtcNow, but its static
/// GetRates is the pure ring-buffer read behind per-second rate history. These
/// tests pin that extraction without touching the time-dependent machinery.
/// </summary>
public class RateCounterTests
{
    /// <summary>
    /// GetRates reads backwards from the current index, newest first, wrapping around
    /// the ring buffer. Parameterized to cover a simple read and the request-clamped-to-
    /// available case, which is the behaviour the rate graph depends on.
    /// </summary>
    [Theory]
    [InlineData(3, 5, new[] { 50, 40, 30 })] // newest (index 4) first, walking back
    [InlineData(10, 2, new[] { 20, 10 })]    // seconds clamped to currentIndex
    public void GetRates_WithinFilledBuffer_ReturnsNewestFirst(int seconds, int currentIndex, int[] expected)
    {
        var values = new[] { 10, 20, 30, 40, 50 };

        var rates = RateCounter.GetRates(seconds, currentIndex, values);

        rates.Should().Equal(expected);
    }

    /// <summary>
    /// Before any sample has been recorded (currentIndex 0) GetRates must return an
    /// empty array rather than reading stale or out-of-range slots.
    /// </summary>
    [Fact]
    public void GetRates_WithNoSamplesYet_ReturnsEmpty()
    {
        var values = new[] { 10, 20, 30, 40, 50 };

        var rates = RateCounter.GetRates(3, 0, values);

        rates.Should().BeEmpty();
    }

    /// <summary>
    /// With more increments than buffer slots the index keeps climbing and the read
    /// must wrap modulo the buffer length, so the newest values still come back in order.
    /// </summary>
    [Fact]
    public void GetRates_WhenIndexHasWrapped_ReadsModuloBufferLength()
    {
        var values = new[] { 10, 20, 30, 40, 50 };

        // currentIndex 7 over a length-5 buffer: read slots 6%5=1, 5%5=0, 4%5=4.
        var rates = RateCounter.GetRates(3, 7, values);

        rates.Should().Equal(20, 10, 50);
    }
}
