using Frms.Business.Validators;

namespace Frms.UnitTests;

public sealed class PolicyVersionValidatorTests
{
    [Test]
    public void ValidPolicyVersion_ReturnsNoValidationError()
    {
        var violation = PolicyVersionValidator.Validate(24, 1, 31, 15, 16, 30, 31);
        Assert.That(violation, Is.Null);
    }

    [Test]
    public void MultipleInvalidInputs_ReturnsTheFirstOriginalValidationError()
    {
        var violation = PolicyVersionValidator.Validate(0, 0, 32, 0, 0, 0, 0);
        Assert.Multiple(() =>
        {
            Assert.That(violation?.Field, Is.EqualTo("depositTimeoutHours"));
            Assert.That(violation?.Message, Is.EqualTo("Deposit timeout hours must be greater than 0."));
        });
    }

    [Test]
    public void ReversedVisitRange_PreservesOriginalFieldAndMessage()
    {
        var violation = PolicyVersionValidator.Validate(24, 20, 10, 15, 16, 30, 25);
        Assert.Multiple(() =>
        {
            Assert.That(violation?.Field, Is.EqualTo("reservationVisitEndDay"));
            Assert.That(violation?.Message,
                Is.EqualTo("Reservation visit end day must be greater than or equal to start day."));
        });
    }
}
