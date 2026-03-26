namespace MedFlow.Application.Interfaces.Workflow;

public interface IPayloadTemplateEngine
{
    string Apply(string templateJson, IReadOnlyDictionary<string, object?> variables);
}
