using NUnit.Framework;

public sealed class OperatorIntentProcessorTests
{
    [Test]
    public void ProcessorKeepsMovementAsARepeatableIntent()
    {
        var processor = new OperatorIntentProcessor(0.25, 0.5);
        OperatorIntent? result = processor.Process(
            OperatorIntentMapping.Create(OperatorIntentKind.ForwardSmall, 1.0));

        Assert.That(result?.Kind, Is.EqualTo(OperatorIntentKind.ForwardSmall));
        Assert.That(result?.IsOneShot, Is.False);
    }

    [Test]
    public void ProcessorRequiresHoldForExtinguish()
    {
        var processor = new OperatorIntentProcessor(0.25, 0.5);
        Assert.That(
            processor.Process(OperatorIntentMapping.Create(OperatorIntentKind.Extinguish, 2.0)),
            Is.Null);
        Assert.That(
            processor.Process(OperatorIntentMapping.Create(OperatorIntentKind.Extinguish, 2.26))?.Kind,
            Is.EqualTo(OperatorIntentKind.Extinguish));
    }
}
