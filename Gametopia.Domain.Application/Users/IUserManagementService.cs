namespace Gametopia.Domain.Application.Users;

public interface IUserManagementService
{
    Task<UserOperationResult> CreateUserAsync(CreateUserRequest request);
    Task<UserOperationResult> UpdateUserAsync(Guid userId, UpdateUserRequest request);
    Task<UserOperationResult> SoftDeleteUserAsync(Guid userId);
    Task<UserOperationResult> DeleteUserAsync(Guid userId);
}
