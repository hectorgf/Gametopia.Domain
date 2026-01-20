namespace Gametopia.Domain.Application.Users;

public sealed class UserOperationResult
{
    public bool Succeeded { get; init; }
    public string? UserId { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static UserOperationResult Success(Guid userId)
        => new() { Succeeded = true, UserId = userId.ToString() };

    public static UserOperationResult Failed(params string[] errors)
        => new() { Succeeded = false, Errors = errors };
}
