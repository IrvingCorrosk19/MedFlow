namespace MedFlow.Application.Options;

public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";
    public string CorrelationIdHeader { get; set; } = "X-Correlation-ID";
    public bool IncludeTraceInErrorResponse { get; set; } = false;
    public bool LogRequestBody { get; set; } = false;
}
