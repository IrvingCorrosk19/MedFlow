namespace MedFlow.Application.Exceptions;

public sealed class NotFoundException : Exception
{
    public string? EntityName { get; }
    public object? EntityId { get; }

    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string message, Exception inner) : base(message, inner) { }

    public NotFoundException(string entityName, object entityId)
        : base($"Entity '{entityName}' with id '{entityId}' was not found.")
    {
        EntityName = entityName;
        EntityId = entityId;
    }
}
