namespace MedFlow.Application.Options;

public class ResendOptions
{
    public const string SectionName = "Resend";

    public string ApiKey { get; set; } = "";
    public string FromEmail { get; set; } = "notifications@medflow.ai";
    public string FromName { get; set; } = "MedFlow AI";
}
