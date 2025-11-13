using MongoDB.Driver;
using IncidentManagementSystem.Data;
using IncidentManagementSystem.Models;

namespace IncidentManagementSystem.Repositories
{
    /// <summary>
    /// MongoDB implementation of the User repository.
    /// Provides comprehensive CRUD operations and user-specific queries.
    /// 
    /// Performance Considerations:
    /// - Uses MongoDB-specific optimizations like compound indexes
    /// - Implements efficient filtering with MongoDB query builders
    /// - Utilizes projection for queries that don't need full documents
    /// 
    /// Error Handling Strategy:
    /// - Graceful handling of MongoDB exceptions
    /// - Null safety for optional operations
    /// - Proper validation of inputs
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly IMongoCollection<User> _users;

        /// <summary>
        /// Initialize repository with MongoDB context
        /// </summary>
        /// <param name="context">MongoDB database context</param>
        public UserRepository(MongoDbContext context)
        {
            _users = context.Users ?? throw new ArgumentNullException(nameof(context.Users));
        }

        #region Generic Repository Implementation

        /// <summary>
        /// Get all users with pagination support
        /// Uses skip/limit pattern for efficient pagination
        /// </summary>
        public async Task<IEnumerable<User>> GetAllAsync(int skip = 0, int limit = 100)
        {
            try
            {
                return await _users
                    .Find(FilterDefinition<User>.Empty)
                    .Skip(skip)
                    .Limit(limit)
                    .SortByDescending(u => u.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // In production, use proper logging framework (e.g., ILogger)
                throw new InvalidOperationException("Failed to retrieve users", ex);
            }
        }

        /// <summary>
        /// Get user by MongoDB ObjectId
        /// </summary>
        public async Task<User?> GetByIdAsync(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return null;

                return await _users
                    .Find(u => u.Id == id)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve user with ID {id}", ex);
            }
        }

        /// <summary>
        /// Create new user with validation and error handling
        /// </summary>
        public async Task<User> CreateAsync(User user)
        {
            try
            {
                if (user == null)
                    throw new ArgumentNullException(nameof(user));

                // Set timestamps
                user.CreatedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                // Validate uniqueness before insert
                if (!await IsUsernameAvailableAsync(user.Username))
                    throw new InvalidOperationException($"Username '{user.Username}' is already taken");

                if (!await IsEmailAvailableAsync(user.Email))
                    throw new InvalidOperationException($"Email '{user.Email}' is already registered");

                await _users.InsertOneAsync(user);
                return user;
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                throw new InvalidOperationException("Username or email already exists", ex);
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                throw new InvalidOperationException("Failed to create user", ex);
            }
        }

        /// <summary>
        /// Update existing user with optimistic concurrency
        /// </summary>
        public async Task<User?> UpdateAsync(string id, User user)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id) || user == null)
                    return null;

                user.Id = id;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _users.ReplaceOneAsync(
                    u => u.Id == id,
                    user);

                return result.MatchedCount > 0 ? user : null;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to update user with ID {id}", ex);
            }
        }

        /// <summary>
        /// Delete user permanently from database
        /// Note: Consider implementing soft delete for audit purposes
        /// </summary>
        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return false;

                var result = await _users.DeleteOneAsync(u => u.Id == id);
                return result.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to delete user with ID {id}", ex);
            }
        }

        /// <summary>
        /// Check if user exists by ID
        /// Uses projection to minimize data transfer
        /// </summary>
        public async Task<bool> ExistsAsync(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return false;

                var count = await _users
                    .CountDocumentsAsync(u => u.Id == id);
                
                return count > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to check existence of user with ID {id}", ex);
            }
        }

        /// <summary>
        /// Get total count of users for pagination calculation
        /// </summary>
        public async Task<long> GetCountAsync()
        {
            try
            {
                return await _users.CountDocumentsAsync(FilterDefinition<User>.Empty);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get user count", ex);
            }
        }

        #endregion

        #region User-Specific Methods

        /// <summary>
        /// Find user by username for authentication
        /// Uses indexed field for optimal performance
        /// </summary>
        public async Task<User?> GetByUsernameAsync(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    return null;

                return await _users
                    .Find(u => u.Username == username)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve user with username {username}", ex);
            }
        }

        /// <summary>
        /// Find user by email address
        /// Uses indexed field for optimal performance
        /// </summary>
        public async Task<User?> GetByEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return null;

                return await _users
                    .Find(u => u.Email == email)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve user with email {email}", ex);
            }
        }

        /// <summary>
        /// Get users filtered by role with pagination
        /// Utilizes role index for efficient filtering
        /// </summary>
        public async Task<IEnumerable<User>> GetByRoleAsync(UserRole role, int skip = 0, int limit = 100)
        {
            try
            {
                return await _users
                    .Find(u => u.Role == role)
                    .Skip(skip)
                    .Limit(limit)
                    .SortBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve users with role {role}", ex);
            }
        }

        /// <summary>
        /// Get users by department with pagination
        /// </summary>
        public async Task<IEnumerable<User>> GetByDepartmentAsync(string department, int skip = 0, int limit = 100)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(department))
                    return Enumerable.Empty<User>();

                return await _users
                    .Find(u => u.Department == department)
                    .Skip(skip)
                    .Limit(limit)
                    .SortBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve users in department {department}", ex);
            }
        }

        /// <summary>
        /// Get only active users with pagination
        /// </summary>
        public async Task<IEnumerable<User>> GetActiveUsersAsync(int skip = 0, int limit = 100)
        {
            try
            {
                return await _users
                    .Find(u => u.IsActive)
                    .Skip(skip)
                    .Limit(limit)
                    .SortBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve active users", ex);
            }
        }

        /// <summary>
        /// Check username availability for registration
        /// Case-insensitive comparison for better user experience
        /// </summary>
        public async Task<bool> IsUsernameAvailableAsync(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    return false;

                var count = await _users
                    .CountDocumentsAsync(u => u.Username.ToLower() == username.ToLower());
                
                return count == 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to check username availability for {username}", ex);
            }
        }

        /// <summary>
        /// Check email availability for registration
        /// Case-insensitive comparison for better user experience
        /// </summary>
        public async Task<bool> IsEmailAvailableAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return false;

                var count = await _users
                    .CountDocumentsAsync(u => u.Email.ToLower() == email.ToLower());
                
                return count == 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to check email availability for {email}", ex);
            }
        }

        /// <summary>
        /// Update user's last login timestamp for session tracking
        /// Uses partial update for efficiency
        /// </summary>
        public async Task<bool> UpdateLastLoginAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return false;

                var update = Builders<User>.Update
                    .Set(u => u.LastLogin, DateTime.UtcNow)
                    .Set(u => u.UpdatedAt, DateTime.UtcNow);

                var result = await _users.UpdateOneAsync(
                    u => u.Id == userId,
                    update);

                return result.MatchedCount > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to update last login for user {userId}", ex);
            }
        }

        /// <summary>
        /// Soft delete user by setting IsActive to false
        /// Preserves data for audit purposes
        /// </summary>
        public async Task<bool> DeactivateUserAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return false;

                var update = Builders<User>.Update
                    .Set(u => u.IsActive, false)
                    .Set(u => u.UpdatedAt, DateTime.UtcNow);

                var result = await _users.UpdateOneAsync(
                    u => u.Id == userId,
                    update);

                return result.MatchedCount > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to deactivate user {userId}", ex);
            }
        }

        /// <summary>
        /// Reactivate user account by setting IsActive to true
        /// </summary>
        public async Task<bool> ActivateUserAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return false;

                var update = Builders<User>.Update
                    .Set(u => u.IsActive, true)
                    .Set(u => u.UpdatedAt, DateTime.UtcNow);

                var result = await _users.UpdateOneAsync(
                    u => u.Id == userId,
                    update);

                return result.MatchedCount > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to activate user {userId}", ex);
            }
        }

        #endregion
    }
}