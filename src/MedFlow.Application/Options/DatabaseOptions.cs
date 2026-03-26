namespace MedFlow.Application.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";
    public string DefaultConnection { get; set; } = "";
}
