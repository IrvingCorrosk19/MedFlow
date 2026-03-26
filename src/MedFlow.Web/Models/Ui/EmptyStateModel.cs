namespace MedFlow.Web.Models.Ui;

public class EmptyStateModel
{
    public string IconClass { get; set; } = "fa fa-inbox";
    public string Title { get; set; } = "Sin datos";
    public string? Description { get; set; }
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }
}
