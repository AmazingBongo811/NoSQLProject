using Microsoft.AspNetCore.Mvc;
using IncidentManagementSystem.Repositories;
using IncidentManagementSystem.Services;

namespace IncidentManagementSystem.Controllers
{
    /// <summary>
    /// Development controller for testing database operations and seeding.
    /// This controller demonstrates the CRUD functionality implemented in deliverable 2.
    /// 
    /// Note: This is for development/testing purposes. In production, these operations
    /// would be handled through proper service layers and authentication.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class DatabaseTestController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ITicketRepository _ticketRepository;
        private readonly DatabaseSeedService _seedService;
        private readonly CsvExportService _csvExportService;

        public DatabaseTestController(
            IUserRepository userRepository, 
            ITicketRepository ticketRepository,
            DatabaseSeedService seedService,
            CsvExportService csvExportService)
        {
            _userRepository = userRepository;
            _ticketRepository = ticketRepository;
            _seedService = seedService;
            _csvExportService = csvExportService;
        }

        /// <summary>
        /// Seed the database with initial test data (minimum 100 documents per collection)
        /// GET: api/databasetest/seed
        /// </summary>
        [HttpGet("seed")]
        public async Task<ActionResult<object>> SeedDatabase()
        {
            try
            {
                var success = await _seedService.SeedDatabaseAsync();
                
                if (success)
                {
                    var stats = await _seedService.GetSeedingStatisticsAsync();
                    return Ok(new
                    {
                        Success = true,
                        Message = "Database seeded successfully",
                        Statistics = stats
                    });
                }
                
                return BadRequest(new { Success = false, Message = "Database seeding failed" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Get seeding statistics to verify data requirements are met
        /// GET: api/databasetest/stats
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetStatistics()
        {
            try
            {
                var stats = await _seedService.GetSeedingStatisticsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Demonstrate User CRUD operations
        /// GET: api/databasetest/users
        /// </summary>
        [HttpGet("users")]
        public async Task<ActionResult<object>> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var skip = (page - 1) * pageSize;
                var users = await _userRepository.GetAllAsync(skip, pageSize);
                var totalCount = await _userRepository.GetCountAsync();
                
                return Ok(new
                {
                    Users = users.Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.Email,
                        u.FullName,
                        u.Role,
                        u.Department,
                        u.IsActive,
                        u.CreatedAt
                    }),
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Demonstrate Ticket CRUD operations and aggregation queries
        /// GET: api/databasetest/tickets
        /// </summary>
        [HttpGet("tickets")]
        public async Task<ActionResult<object>> GetTickets([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var skip = (page - 1) * pageSize;
                var tickets = await _ticketRepository.GetAllAsync(skip, pageSize);
                var totalCount = await _ticketRepository.GetCountAsync();
                
                return Ok(new
                {
                    Tickets = tickets.Select(t => new
                    {
                        t.Id,
                        t.Title,
                        t.Status,
                        t.Priority,
                        t.Category,
                        Reporter = t.Reporter.FullName,
                        Assignee = t.Assignee?.FullName,
                        t.CreatedAt,
                        t.UpdatedAt,
                        ActivityCount = t.Activities.Count,
                        CommentCount = t.Comments.Count
                    }),
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Demonstrate aggregation pipeline for user dashboard statistics
        /// GET: api/databasetest/user-dashboard/{userId}
        /// </summary>
        [HttpGet("user-dashboard/{userId}")]
        public async Task<ActionResult<object>> GetUserDashboardStats(string userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { Message = "User not found" });
                }

                var stats = await _ticketRepository.GetUserDashboardStatsAsync(userId);
                
                return Ok(new
                {
                    User = new
                    {
                        user.Id,
                        user.FullName,
                        user.Email,
                        user.Department
                    },
                    Statistics = stats
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Demonstrate aggregation pipeline for service desk dashboard statistics
        /// GET: api/databasetest/service-desk-dashboard
        /// </summary>
        [HttpGet("service-desk-dashboard")]
        public async Task<ActionResult<object>> GetServiceDeskDashboardStats()
        {
            try
            {
                var stats = await _ticketRepository.GetServiceDeskDashboardStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Demonstrate ticket search functionality
        /// GET: api/databasetest/search?q={searchTerm}
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<object>> SearchTickets([FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return BadRequest(new { Message = "Search query is required" });
                }

                var skip = (page - 1) * pageSize;
                var tickets = await _ticketRepository.SearchTicketsAsync(q, skip, pageSize);
                
                return Ok(new
                {
                    SearchTerm = q,
                    Results = tickets.Select(t => new
                    {
                        t.Id,
                        t.Title,
                        t.Description,
                        t.Status,
                        Reporter = t.Reporter.FullName,
                        t.CreatedAt
                    }),
                    Page = page,
                    PageSize = pageSize,
                    ResultCount = tickets.Count()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Demonstrate filtering tickets by status
        /// GET: api/databasetest/tickets/status/{status}
        /// </summary>
        [HttpGet("tickets/status/{status}")]
        public async Task<ActionResult<object>> GetTicketsByStatus(string status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                if (!Enum.TryParse<Models.TicketStatus>(status, true, out var ticketStatus))
                {
                    return BadRequest(new { Message = "Invalid status. Valid values: Open, Resolved, Closed" });
                }

                var skip = (page - 1) * pageSize;
                var tickets = await _ticketRepository.GetTicketsByStatusAsync(ticketStatus, skip, pageSize);
                
                return Ok(new
                {
                    Status = ticketStatus.ToString(),
                    Tickets = tickets.Select(t => new
                    {
                        t.Id,
                        t.Title,
                        t.Priority,
                        Reporter = t.Reporter.FullName,
                        Assignee = t.Assignee?.FullName,
                        t.CreatedAt
                    }),
                    Page = page,
                    PageSize = pageSize,
                    ResultCount = tickets.Count()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Health check endpoint to verify database connectivity
        /// GET: api/databasetest/health
        /// </summary>
        [HttpGet("health")]
        public async Task<ActionResult<object>> HealthCheck()
        {
            try
            {
                var userCount = await _userRepository.GetCountAsync();
                var ticketCount = await _ticketRepository.GetCountAsync();
                
                return Ok(new
                {
                    Status = "Healthy",
                    Database = "Connected",
                    Collections = new
                    {
                        Users = userCount,
                        Tickets = ticketCount
                    },
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Status = "Unhealthy",
                    Database = "Connection Failed",
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Export all collections to CSV files for deliverable 2 submission
        /// GET: api/databasetest/export-csv
        /// </summary>
        [HttpGet("export-csv")]
        public async Task<ActionResult<object>> ExportCsv()
        {
            try
            {
                var result = await _csvExportService.ExportAllToFilesAsync();
                
                return Ok(new
                {
                    Success = true,
                    Message = "CSV export completed successfully",
                    Details = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Download Users CSV file directly
        /// GET: api/databasetest/download/users
        /// </summary>
        [HttpGet("download/users")]
        public async Task<IActionResult> DownloadUsersCsv()
        {
            try
            {
                var csvContent = await _csvExportService.ExportUsersAsync();
                var fileName = $"Users_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                
                return File(
                    System.Text.Encoding.UTF8.GetBytes(csvContent),
                    "text/csv",
                    fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Download Tickets CSV file directly
        /// GET: api/databasetest/download/tickets
        /// </summary>
        [HttpGet("download/tickets")]
        public async Task<IActionResult> DownloadTicketsCsv()
        {
            try
            {
                var csvContent = await _csvExportService.ExportTicketsAsync();
                var fileName = $"Tickets_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                
                return File(
                    System.Text.Encoding.UTF8.GetBytes(csvContent),
                    "text/csv",
                    fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Create simple test users with known credentials
        /// GET: api/databasetest/createtestusers
        /// </summary>
        [HttpGet("createtestusers")]
        public async Task<ActionResult<object>> CreateTestUsers()
        {
            try
            {
                var createdUsers = new List<object>();
                var existingUsers = new List<string>();

                // Check and create regular test user
                var existingJohn = (await _userRepository.GetAllAsync()).FirstOrDefault(u => u.Email == "john.doe@company.com");
                if (existingJohn == null)
                {
                    var johnDoe = new Models.User
                    {
                        Username = "john.doe",
                        Email = "john.doe@company.com",
                        PasswordHash = HashPassword("password123"),
                        FirstName = "John",
                        LastName = "Doe",
                        Role = Models.UserRole.Regular,
                        Department = "Information Technology",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    await _userRepository.CreateAsync(johnDoe);
                    createdUsers.Add(new { Email = "john.doe@company.com", Password = "password123", Role = "Regular" });
                }
                else
                {
                    existingUsers.Add("john.doe@company.com");
                }

                // Check and create service desk test user
                var existingAdmin = (await _userRepository.GetAllAsync()).FirstOrDefault(u => u.Email == "admin@company.com");
                if (existingAdmin == null)
                {
                    var admin = new Models.User
                    {
                        Username = "admin",
                        Email = "admin@company.com",
                        PasswordHash = HashPassword("admin123"),
                        FirstName = "Admin",
                        LastName = "User",
                        Role = Models.UserRole.ServiceDesk,
                        Department = "Information Technology",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    await _userRepository.CreateAsync(admin);
                    createdUsers.Add(new { Email = "admin@company.com", Password = "admin123", Role = "ServiceDesk" });
                }
                else
                {
                    existingUsers.Add("admin@company.com");
                }

                var message = createdUsers.Count > 0 
                    ? $"{createdUsers.Count} test user(s) created successfully" 
                    : "All test users already exist";

                if (existingUsers.Count > 0)
                {
                    message += $". Existing: {string.Join(", ", existingUsers)}";
                }

                return Ok(new
                {
                    Success = true,
                    Message = message,
                    CreatedUsers = createdUsers,
                    ExistingUsers = existingUsers,
                    AllUsers = new[]
                    {
                        new { Email = "john.doe@company.com", Password = "password123", Role = "Regular" },
                        new { Email = "admin@company.com", Password = "admin123", Role = "ServiceDesk" }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Simple password hashing to match the seed service
        /// </summary>
        private string HashPassword(string password)
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password + "salt"));
        }

        /// <summary>
        /// Test login credentials
        /// GET: api/databasetest/testlogin?email=admin@company.com&password=admin123
        /// </summary>
        [HttpGet("testlogin")]
        public async Task<ActionResult<object>> TestLogin(string email, string password)
        {
            try
            {
                var users = await _userRepository.GetAllAsync();
                var user = users.FirstOrDefault(u => u.Email.ToLower() == email.ToLower());

                if (user == null)
                {
                    return Ok(new { Success = false, Message = "User not found", Email = email });
                }

                var hashedPassword = HashPassword(password);
                var match = user.PasswordHash == hashedPassword;

                return Ok(new
                {
                    Success = match,
                    Message = match ? "Credentials are valid" : "Password does not match",
                    Email = user.Email,
                    Role = user.Role.ToString(),
                    StoredHash = user.PasswordHash,
                    ProvidedHash = hashedPassword,
                    Match = match
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }
    }
}