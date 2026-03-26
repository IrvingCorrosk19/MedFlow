namespace MedFlow.Web.Models.Ui;

public class StatCardModel
{
    public string Value { get; set; } = "0";
    public string Label { get; set; } = "";
    public string IconClass { get; set; } = "fa fa-circle";
    public string ColorClass { get; set; } = "bg-info";
    public string? FooterUrl { get; set; }
    public string? FooterText { get; set; }
}
