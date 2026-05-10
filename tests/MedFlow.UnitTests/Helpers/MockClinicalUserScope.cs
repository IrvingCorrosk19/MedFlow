using MedFlow.Application.Interfaces;
using Moq;

namespace MedFlow.UnitTests.Helpers;

/// <summary>Mocks <see cref="IClinicalUserScope"/> sin restricción de médico (comportamiento previo en tests).</summary>
public static class MockClinicalUserScope
{
    public static IClinicalUserScope NoDoctorRestriction()
    {
        var m = new Mock<IClinicalUserScope>();
        m.Setup(x => x.GetDoctorDataScopeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, (Guid?)null));
        m.Setup(x => x.DoctorHasClinicalRelationshipWithPatientAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        return m.Object;
    }
}
