using System.Text.Json;
using System.Text.RegularExpressions;
using MedFlow.Application.Interfaces.Workflow;

namespace MedFlow.Infrastructure.Workflow;

public sealed class PayloadTemplateEngine : IPayloadTemplateEngine
{
    private static readonly Regex VariablePattern = new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

    public string Apply(string templateJson, IReadOnlyDictionary<string, object?> variables)
    {
        if (string.IsNullOrWhiteSpace(templateJson))
            return "{}";

        var replaced = VariablePattern.Replace(templateJson, match =>
        {
            var key = match.Groups[1].Value;
            if (variables.TryGetValue(key, out var value))
                return value == null ? "null" : JsonSerializer.Serialize(value);
            return string.Empty;
        });

        try
        {
            _ = JsonDocument.Parse(replaced);
            return replaced;
        }
        catch
        {
            return replaced;
        }
    }
}
