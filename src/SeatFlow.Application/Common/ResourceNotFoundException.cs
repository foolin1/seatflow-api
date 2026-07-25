namespace SeatFlow.Application.Common;

public sealed class ResourceNotFoundException
    : Exception
{
    public ResourceNotFoundException(
        string resourceName,
        Guid resourceId)
        : base(
            $"{resourceName} with identifier " +
            $"'{resourceId}' was not found.")
    {
        ResourceName = resourceName;
        ResourceId = resourceId;
    }

    public string ResourceName { get; }

    public Guid ResourceId { get; }
}