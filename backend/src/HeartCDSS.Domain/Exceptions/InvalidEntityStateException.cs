namespace HeartCDSS.Domain.Exceptions;

/// <summary>
/// Thrown when an entity lifecycle transition or operation violates business domain rules.
/// </summary>
public class InvalidEntityStateException : DomainException
{
    public string EntityName { get; }
    public object EntityId { get; }
    public string Operation { get; }

    public InvalidEntityStateException(string entityName, object entityId, string operation, string message)
        : base($"Entity '{entityName}' with ID '{entityId}' cannot execute '{operation}': {message}")
    {
        EntityName = entityName;
        EntityId = entityId;
        Operation = operation;
    }
}
