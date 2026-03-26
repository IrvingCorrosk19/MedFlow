namespace MedFlow.Application.Exceptions;

public sealed class TenantResolutionException : Exception
{
    public TenantResolutionException(string message) : base(message) { }

    public TenantResolutionException(string message, Exception inner) : base(message, inner) { }
}
