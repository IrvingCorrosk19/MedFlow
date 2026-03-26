namespace MedFlow.Application.Options;

public sealed class TenantResolutionOptions
{
    public const string SectionName = "TenantResolution";
    public string DefaultTenantCode { get; set; } = "demo";
    public bool AllowHeaderOverride { get; set; } = true;
    public string? HostSuffix { get; set; }
}
