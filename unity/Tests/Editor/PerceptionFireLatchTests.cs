using NUnit.Framework;

public sealed class PerceptionFireLatchTests
{
    [Test]
    public void DetectionCanBeTriggeredAgainAfterClear()
    {
        var latch = new PerceptionFireLatch();

        Assert.That(latch.TryActivate(true), Is.True);
        Assert.That(latch.TryActivate(true), Is.False);
        Assert.That(latch.TryActivate(false), Is.False);
        Assert.That(latch.TryActivate(true), Is.True);
    }
}
