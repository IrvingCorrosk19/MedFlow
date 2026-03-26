namespace MedFlow.Application.Options;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";
    public bool AllowAnyOrigin { get; set; } = true;
    public string[] AllowedOrigins { get; set; } = [];
    public string[] AllowedMethods { get; set; } = ["GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"];
    public string[] AllowedHeaders { get; set; } = ["Authorization", "Content-Type", "X-Correlation-ID", "X-Tenant-Code", "X-Api-Key", "X-N8n-Api-Key"];
    public bool AllowCredentials { get; set; } = true;
}
