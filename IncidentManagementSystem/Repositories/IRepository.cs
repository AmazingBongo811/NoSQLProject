using IncidentManagementSystem.Models;

namespace IncidentManagementSystem.Repositories
{
    /// <summary>
    /// Generic repository interface for common CRUD operations.
    /// Provides a consistent interface for all entity repositories.
    /// 
    /// Design Pattern: Repository Pattern
    /// Rationale: Abstraction layer between data access and business logic,
    /// enables easier unit testing and potential data store changes.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IRepository<T> where T : class
    {
        /// <summary>
        /// Get all entities with optional filtering and pagination
        /// </summary>
        /// <param name="skip">Number of documents to skip (for pagination)</param>
        /// <param name="limit">Maximum number of documents to return</param>
        /// <returns>Collection of entities</returns>
        Task<IEnumerable<T>> GetAllAsync(int skip = 0, int limit = 100);

        /// <summary>
        /// Get entity by unique identifier
        /// </summary>
        /// <param name="id">Entity identifier</param>
        /// <returns>Entity if found, null otherwise</returns>
        Task<T?> GetByIdAsync(string id);

        /// <summary>
        /// Create a new entity
        /// </summary>
        /// <param name="entity">Entity to create</param>
        /// <returns>Created entity with assigned ID</returns>
        Task<T> CreateAsync(T entity);

        /// <summary>
        /// Update an existing entity
        /// </summary>
        /// <param name="id">Entity identifier</param>
        /// <param name="entity">Updated entity data</param>
        /// <returns>Updated entity if successful, null if not found</returns>
        Task<T?> UpdateAsync(string id, T entity);

        /// <summary>
        /// Delete an entity by identifier
        /// </summary>
        /// <param name="id">Entity identifier</param>
        /// <returns>True if deleted, false if not found</returns>
        Task<bool> DeleteAsync(string id);

        /// <summary>
        /// Check if entity exists by identifier
        /// </summary>
        /// <param name="id">Entity identifier</param>
        /// <returns>True if exists, false otherwise</returns>
        Task<bool> ExistsAsync(string id);

        /// <summary>
        /// Get total count of entities
        /// </summary>
        /// <returns>Total number of entities</returns>
        Task<long> GetCountAsync();
    }
}