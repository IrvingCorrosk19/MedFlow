namespace MedFlow.Application.Interfaces.AI;

public interface IPaymentRiskService
{
    Task<PaymentRiskResult> EvaluatePatientAsync(Guid patientId, CancellationToken cancellationToken = default);
}

public record PaymentRiskResult(
    decimal Score,
    string Severity,
    string Summary,
    string Recommendation,
    IReadOnlyList<string> Factors,
    int OverdueInvoicesCount,
    decimal TotalOverdueAmount,
    int DaysAverageOverdue);
