using IncidentManagementSystem.Models;

namespace IncidentManagementSystem.Repositories
{
    /// <summary>
    /// Repository interface specific to User entity operations.
    /// Extends the generic repository with user-specific queries.
    /// 
    /// Design Decision: Separate interface for entity-specific operations
    /// Rationale: Provides type safety and specific methods for user management
    /// while maintaining the generic repository pattern benefits.
    /// </summary>
    public interface IUserRepository : IRepository<User>
    {
        /// <summary>
        /// Find user by username (for authentication)
        /// </summary>
        /// <param name="username">Username to search for</param>
        /// <returns>User if found, null otherwise</returns>
        Task<User?> GetByUsernameAsync(string username);

        /// <summary>
        /// Find user by email address
        /// </summary>
        /// <param name="email">Email address to search for</param>
        /// <returns>User if found, null otherwise</returns>
        Task<User?> GetByEmailAsync(string email);

        /// <summary>
        /// Get users by role (Regular or ServiceDesk)
        /// </summary>
        /// <param name="role">User role to filter by</param>
        /// <param name="skip">Number of documents to skip (for pagination)</param>
        /// <param name="limit">Maximum number of documents to return</param>
        /// <returns>Collection of users with specified role</returns>
        Task<IEnumerable<User>> GetByRoleAsync(UserRole role, int skip = 0, int limit = 100);

        /// <summary>
        /// Get users by department
        /// </summary>
        /// <param name="department">Department name to filter by</param>
        /// <param name="skip">Number of documents to skip (for pagination)</param>
        /// <param name="limit">Maximum number of documents to return</param>
        /// <returns>Collection of users in specified department</returns>
        Task<IEnumerable<User>> GetByDepartmentAsync(string department, int skip = 0, int limit = 100);

        /// <summary>
        /// Get only active users
        /// </summary>
        /// <param name="skip">Number of documents to skip (for pagination)</param>
        /// <param name="limit">Maximum number of documents to return</param>
        /// <returns>Collection of active users</returns>
        Task<IEnumerable<User>> GetActiveUsersAsync(int skip = 0, int limit = 100);

        /// <summary>
        /// Check if username is available (not taken)
        /// </summary>
        /// <param name="username">Username to check</param>
        /// <returns>True if available, false if taken</returns>
        Task<bool> IsUsernameAvailableAsync(string username);

        /// <summary>
        /// Check if email is available (not taken)
        /// </summary>
        /// <param name="email">Email to check</param>
        /// <returns>True if available, false if taken</returns>
        Task<bool> IsEmailAvailableAsync(string email);

        /// <summary>
        /// Update user's last login timestamp
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>True if updated successfully</returns>
        Task<bool> UpdateLastLoginAsync(string userId);

        /// <summary>
        /// Deactivate user account (soft delete)
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>True if deactivated successfully</returns>
        Task<bool> DeactivateUserAsync(string userId);

        /// <summary>
        /// Activate user account
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>True if activated successfully</returns>
        Task<bool> ActivateUserAsync(string userId);
    }
}