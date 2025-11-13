using IncidentManagementSystem.Data;
using IncidentManagementSystem.Models;
using IncidentManagementSystem.Repositories;
using MongoDB.Driver;

namespace IncidentManagementSystem.Services
{
    /// <summary>
    /// Service for seeding the database with initial data to meet project requirements.
    /// Creates minimum 100 documents in Users and Tickets collections as specified in deliverable 1.
    /// 
    /// Design Strategy:
    /// - Creates realistic test data that represents actual business scenarios
    /// - Ensures referential integrity between related documents
    /// - Provides diverse data distribution for testing various query patterns
    /// - Includes proper timestamps and realistic activity histories
    /// 
    /// Data Distribution Strategy:
    /// Users: 150 total (100 Regular + 50 Service Desk employees)
    /// Tickets: 200 total (diverse status, priority, and category distribution)
    /// Departments: 10 departments covering typical organizational structure
    /// </summary>
    public class DatabaseSeedService
    {
        private readonly MongoDbContext _context;
        private readonly IUserRepository _userRepository;
        private readonly ITicketRepository _ticketRepository;
        private readonly Random _random;

        // Predefined data arrays for realistic content generation
        private readonly string[] _departments = {
            "Information Technology", "Human Resources", "Finance", "Operations",
            "Marketing", "Customer Service", "Sales", "Legal", "Procurement", "Facilities"
        };

        private readonly string[] _firstNames = {
            "James", "Mary", "John", "Patricia", "Robert", "Jennifer", "Michael", "Linda",
            "William", "Elizabeth", "David", "Barbara", "Richard", "Susan", "Joseph", "Jessica",
            "Thomas", "Sarah", "Christopher", "Karen", "Charles", "Nancy", "Daniel", "Lisa",
            "Matthew", "Betty", "Anthony", "Helen", "Mark", "Sandra", "Donald", "Donna",
            "Steven", "Carol", "Paul", "Ruth", "Andrew", "Sharon", "Joshua", "Michelle",
            "Kenneth", "Laura", "Kevin", "Sarah", "Brian", "Kimberly", "George", "Deborah",
            "Edward", "Dorothy", "Ronald", "Lisa", "Timothy", "Nancy", "Jason", "Karen"
        };

        private readonly string[] _lastNames = {
            "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis",
            "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson",
            "Thomas", "Taylor", "Moore", "Jackson", "Martin", "Lee", "Perez", "Thompson",
            "White", "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson", "Walker",
            "Young", "Allen", "King", "Wright", "Scott", "Torres", "Nguyen", "Hill",
            "Flores", "Green", "Adams", "Nelson", "Baker", "Hall", "Rivera", "Campbell"
        };

        private readonly string[] _ticketTitles = {
            "Computer won't start", "Password reset required", "Software installation request",
            "Network connectivity issue", "Printer not working", "Email not syncing",
            "Application crashes frequently", "Slow computer performance", "VPN connection problems",
            "Database access denied", "File sharing issues", "Monitor display problems",
            "Keyboard/mouse not responding", "Software license expiration", "Backup system failure",
            "Security software alert", "Website not loading", "Mobile device setup",
            "Access card not working", "Phone system issues", "Meeting room equipment failure",
            "Cloud storage synchronization", "Application permission error", "System update required",
            "Data recovery needed", "Hardware replacement request", "Internet browser issues"
        };

        private readonly string[] _ticketDescriptionTemplates = {
            "I'm experiencing issues with {0}. This started {1} and is affecting my daily work. Please help resolve this as soon as possible.",
            "There seems to be a problem with {0}. I've tried restarting but the issue persists. This is {1} for my current tasks.",
            "I need assistance with {0}. The problem occurred {1} and I'm unable to complete my work efficiently.",
            "Help needed with {0}. This issue started {1} and is causing significant delays in my projects.",
            "Technical support required for {0}. The problem began {1} and is preventing me from accessing important resources.",
            "I'm having trouble with {0}. This issue emerged {1} and is impacting my productivity severely.",
            "Urgent help needed with {0}. The problem started {1} and I cannot perform my essential job functions."
        };

        private readonly string[] _timeReferences = {
            "yesterday morning", "earlier today", "last Friday", "this morning",
            "after lunch", "during the weekend", "first thing today", "late yesterday"
        };

        private readonly string[] _urgencyLevels = {
            "critical", "important", "blocking", "urgent", "preventing progress", "essential"
        };

        public DatabaseSeedService(MongoDbContext context, IUserRepository userRepository, ITicketRepository ticketRepository)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
            _random = new Random(42); // Fixed seed for consistent test data
        }

        /// <summary>
        /// Main method to seed all collections with required data
        /// </summary>
        /// <returns>True if seeding completed successfully</returns>
        public async Task<bool> SeedDatabaseAsync()
        {
            try
            {
                Console.WriteLine("Starting database seeding process...");

                // Check if data already exists
                if (await _userRepository.GetCountAsync() > 0 || await _ticketRepository.GetCountAsync() > 0)
                {
                    Console.WriteLine("Database already contains data. Skipping seed process.");
                    return true;
                }

                // Seed departments first
                await SeedDepartmentsAsync();
                Console.WriteLine("✓ Departments seeded successfully");

                // Seed users (100 Regular + 50 Service Desk)
                var users = await SeedUsersAsync();
                Console.WriteLine($"✓ {users.Count} users seeded successfully");

                // Seed tickets (200 tickets with diverse distribution)
                await SeedTicketsAsync(users);
                Console.WriteLine("✓ Tickets seeded successfully");

                Console.WriteLine("Database seeding completed successfully!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during database seeding: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Seed the departments collection
        /// </summary>
        private async Task SeedDepartmentsAsync()
        {
            var departments = new List<Department>();

            for (int i = 0; i < _departments.Length; i++)
            {
                departments.Add(new Department
                {
                    Name = _departments[i],
                    Description = $"Responsible for {_departments[i].ToLower()} operations and management",
                    CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(30, 365)),
                    IsActive = true
                });
            }

            await _context.Departments.InsertManyAsync(departments);
        }

        /// <summary>
        /// Seed the users collection with diverse user profiles
        /// Creates 100 Regular employees and 50 Service Desk employees
        /// </summary>
        /// <returns>List of created users for use in ticket creation</returns>
        private async Task<List<User>> SeedUsersAsync()
        {
            var users = new List<User>();

            // Create 100 Regular employees
            for (int i = 1; i <= 100; i++)
            {
                var firstName = _firstNames[_random.Next(_firstNames.Length)];
                var lastName = _lastNames[_random.Next(_lastNames.Length)];
                var username = $"{firstName.ToLower()}.{lastName.ToLower()}{i}";
                var email = $"{username}@company.com";
                var department = _departments[_random.Next(_departments.Length)];

                users.Add(new User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = HashPassword("TempPass123!"), // In production, use proper password hashing
                    FirstName = firstName,
                    LastName = lastName,
                    Role = UserRole.Regular,
                    Department = department,
                    CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(1, 180)),
                    UpdatedAt = DateTime.UtcNow.AddDays(-_random.Next(0, 30)),
                    IsActive = _random.Next(100) < 95, // 95% active users
                    LastLogin = _random.Next(10) < 8 ? DateTime.UtcNow.AddDays(-_random.Next(0, 30)) : null
                });
            }

            // Create 50 Service Desk employees
            for (int i = 1; i <= 50; i++)
            {
                var firstName = _firstNames[_random.Next(_firstNames.Length)];
                var lastName = _lastNames[_random.Next(_lastNames.Length)];
                var username = $"sd.{firstName.ToLower()}.{lastName.ToLower()}{i}";
                var email = $"{username}@company.com";

                users.Add(new User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = HashPassword("ServiceDesk123!"),
                    FirstName = firstName,
                    LastName = lastName,
                    Role = UserRole.ServiceDesk,
                    Department = "Information Technology", // Service desk typically in IT
                    CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(30, 365)),
                    UpdatedAt = DateTime.UtcNow.AddDays(-_random.Next(0, 7)),
                    IsActive = true, // All service desk employees are active
                    LastLogin = DateTime.UtcNow.AddDays(-_random.Next(0, 3)) // Recent logins
                });
            }

            // Insert users in batches for better performance
            const int batchSize = 50;
            for (int i = 0; i < users.Count; i += batchSize)
            {
                var batch = users.Skip(i).Take(batchSize).ToList();
                await _context.Users.InsertManyAsync(batch);
            }

            return users;
        }

        /// <summary>
        /// Seed the tickets collection with realistic incident data
        /// Creates 200 tickets with diverse statuses, priorities, and categories
        /// </summary>
        /// <param name="users">List of users to assign as reporters and assignees</param>
        private async Task SeedTicketsAsync(List<User> users)
        {
            var tickets = new List<Ticket>();
            var regularUsers = users.Where(u => u.Role == UserRole.Regular && u.IsActive).ToList();
            var serviceDeskUsers = users.Where(u => u.Role == UserRole.ServiceDesk).ToList();

            // Status distribution: 40% Open, 35% Resolved, 25% Closed
            var statusDistribution = new[]
            {
                (TicketStatus.Open, 80),      // 40% of 200
                (TicketStatus.Resolved, 70),  // 35% of 200
                (TicketStatus.Closed, 50)     // 25% of 200
            };

            int ticketId = 1;

            foreach (var (status, count) in statusDistribution)
            {
                for (int i = 0; i < count; i++)
                {
                    var reporter = regularUsers[_random.Next(regularUsers.Count)];
                    var createdAt = GenerateRandomCreatedDate(status);
                    var priority = GenerateRandomPriority();
                    var category = GenerateRandomCategory();
                    var title = _ticketTitles[_random.Next(_ticketTitles.Length)];
                    var description = GenerateTicketDescription(title);

                    var ticket = new Ticket
                    {
                        Title = $"{title} - #{ticketId:D4}",
                        Description = description,
                        Status = status,
                        Priority = priority,
                        Category = category,
                        Reporter = new UserInfo
                        {
                            UserId = reporter.Id!,
                            Username = reporter.Username,
                            Email = reporter.Email,
                            FullName = reporter.FullName,
                            Department = reporter.Department
                        },
                        CreatedAt = createdAt,
                        UpdatedAt = createdAt
                    };

                    // Assign service desk employee for some tickets
                    if (_random.Next(100) < 70) // 70% of tickets are assigned
                    {
                        var assignee = serviceDeskUsers[_random.Next(serviceDeskUsers.Count)];
                        ticket.Assignee = new UserInfo
                        {
                            UserId = assignee.Id!,
                            Username = assignee.Username,
                            Email = assignee.Email,
                            FullName = assignee.FullName,
                            Department = assignee.Department
                        };
                    }

                    // Set resolution/closure dates based on status
                    if (status == TicketStatus.Resolved)
                    {
                        ticket.ResolvedAt = createdAt.AddHours(_random.Next(2, 72));
                        ticket.UpdatedAt = ticket.ResolvedAt.Value;
                    }
                    else if (status == TicketStatus.Closed)
                    {
                        ticket.ResolvedAt = createdAt.AddHours(_random.Next(1, 48));
                        ticket.ClosedAt = ticket.ResolvedAt.Value.AddHours(_random.Next(1, 24));
                        ticket.UpdatedAt = ticket.ClosedAt.Value;
                    }

                    // Add activities and comments
                    AddTicketActivities(ticket, serviceDeskUsers);
                    AddTicketComments(ticket, regularUsers, serviceDeskUsers);

                    tickets.Add(ticket);
                    ticketId++;
                }
            }

            // Insert tickets in batches for better performance
            const int batchSize = 25;
            for (int i = 0; i < tickets.Count; i += batchSize)
            {
                var batch = tickets.Skip(i).Take(batchSize).ToList();
                await _context.Tickets.InsertManyAsync(batch);
            }
        }

        /// <summary>
        /// Generate a realistic created date based on ticket status
        /// </summary>
        private DateTime GenerateRandomCreatedDate(TicketStatus status)
        {
            return status switch
            {
                TicketStatus.Open => DateTime.UtcNow.AddDays(-_random.Next(1, 30)),     // Recent open tickets
                TicketStatus.Resolved => DateTime.UtcNow.AddDays(-_random.Next(7, 60)),  // Resolved in last 2 months
                TicketStatus.Closed => DateTime.UtcNow.AddDays(-_random.Next(30, 180)),  // Closed in last 6 months
                _ => DateTime.UtcNow.AddDays(-_random.Next(1, 30))
            };
        }

        /// <summary>
        /// Generate random priority with realistic distribution
        /// </summary>
        private TicketPriority GenerateRandomPriority()
        {
            var rand = _random.Next(100);
            return rand switch
            {
                < 10 => TicketPriority.Critical, // 10%
                < 30 => TicketPriority.High,     // 20%
                < 80 => TicketPriority.Medium,   // 50%
                _ => TicketPriority.Low          // 20%
            };
        }

        /// <summary>
        /// Generate random category with realistic distribution
        /// </summary>
        private TicketCategory GenerateRandomCategory()
        {
            var rand = _random.Next(100);
            return rand switch
            {
                < 35 => TicketCategory.Software, // 35%
                < 60 => TicketCategory.Hardware, // 25%
                < 80 => TicketCategory.Network,  // 20%
                < 95 => TicketCategory.Access,   // 15%
                _ => TicketCategory.Other        // 5%
            };
        }

        /// <summary>
        /// Generate realistic ticket description
        /// </summary>
        private string GenerateTicketDescription(string title)
        {
            var template = _ticketDescriptionTemplates[_random.Next(_ticketDescriptionTemplates.Length)];
            var timeRef = _timeReferences[_random.Next(_timeReferences.Length)];
            var urgency = _urgencyLevels[_random.Next(_urgencyLevels.Length)];

            return string.Format(template, title.ToLower(), timeRef) + 
                   $" This is {urgency} for completing my assigned tasks. " +
                   "Any assistance would be greatly appreciated.";
        }

        /// <summary>
        /// Add realistic activities to a ticket
        /// </summary>
        private void AddTicketActivities(Ticket ticket, List<User> serviceDeskUsers)
        {
            // Always add creation activity
            ticket.Activities.Add(new TicketActivity
            {
                Action = "Created",
                PerformedBy = ticket.Reporter,
                Timestamp = ticket.CreatedAt,
                Description = "Ticket created by user"
            });

            // Add assignment activity if assigned
            if (ticket.Assignee != null)
            {
                var assigner = serviceDeskUsers[_random.Next(serviceDeskUsers.Count)];
                ticket.Activities.Add(new TicketActivity
                {
                    Action = "Assigned",
                    PerformedBy = new UserInfo
                    {
                        UserId = assigner.Id!,
                        Username = assigner.Username,
                        Email = assigner.Email,
                        FullName = assigner.FullName
                    },
                    Timestamp = ticket.CreatedAt.AddMinutes(_random.Next(30, 480)),
                    Description = $"Assigned to {ticket.Assignee.FullName}",
                    NewValue = ticket.Assignee.Username
                });
            }

            // Add status change activities
            if (ticket.Status != TicketStatus.Open)
            {
                var resolver = ticket.Assignee ?? new UserInfo
                {
                    UserId = serviceDeskUsers[0].Id!,
                    Username = serviceDeskUsers[0].Username,
                    Email = serviceDeskUsers[0].Email,
                    FullName = serviceDeskUsers[0].FullName
                };

                if (ticket.Status == TicketStatus.Resolved || ticket.Status == TicketStatus.Closed)
                {
                    ticket.Activities.Add(new TicketActivity
                    {
                        Action = "Resolved",
                        PerformedBy = resolver,
                        Timestamp = ticket.ResolvedAt!.Value,
                        Description = "Ticket marked as resolved",
                        OldValue = "Open",
                        NewValue = "Resolved"
                    });
                }

                if (ticket.Status == TicketStatus.Closed)
                {
                    ticket.Activities.Add(new TicketActivity
                    {
                        Action = "Closed",
                        PerformedBy = resolver,
                        Timestamp = ticket.ClosedAt!.Value,
                        Description = "Ticket closed after verification",
                        OldValue = "Resolved",
                        NewValue = "Closed"
                    });
                }
            }
        }

        /// <summary>
        /// Add realistic comments to a ticket
        /// </summary>
        private void AddTicketComments(Ticket ticket, List<User> regularUsers, List<User> serviceDeskUsers)
        {
            var commentCount = _random.Next(0, 5); // 0-4 comments per ticket

            for (int i = 0; i < commentCount; i++)
            {
                var isServiceDeskComment = _random.Next(2) == 0;
                var author = isServiceDeskComment 
                    ? serviceDeskUsers[_random.Next(serviceDeskUsers.Count)]
                    : regularUsers[_random.Next(regularUsers.Count)];

                var commentTime = ticket.CreatedAt.AddHours(_random.Next(1, 72));
                var content = GenerateCommentContent(isServiceDeskComment, i == 0);

                ticket.Comments.Add(new TicketComment
                {
                    Author = new UserInfo
                    {
                        UserId = author.Id!,
                        Username = author.Username,
                        Email = author.Email,
                        FullName = author.FullName
                    },
                    Content = content,
                    Timestamp = commentTime,
                    IsInternal = isServiceDeskComment && _random.Next(4) == 0 // 25% of SD comments are internal
                });
            }
        }

        /// <summary>
        /// Generate realistic comment content
        /// </summary>
        private string GenerateCommentContent(bool isServiceDesk, bool isFirst)
        {
            if (isServiceDesk)
            {
                var serviceDeskComments = new[]
                {
                    "Thank you for reporting this issue. We are investigating and will update you shortly.",
                    "I've identified the root cause and am working on a resolution. Expected completion within 2 hours.",
                    "This issue has been resolved. Please test and confirm if everything is working properly.",
                    "I need additional information to troubleshoot this issue. Could you please provide more details?",
                    "This appears to be related to a known issue. I'm applying the standard fix procedure.",
                    "The issue should now be resolved. Please restart your application and let me know if problems persist."
                };
                return serviceDeskComments[_random.Next(serviceDeskComments.Length)];
            }
            else
            {
                var userComments = new[]
                {
                    "Thank you for the quick response. I really appreciate the help!",
                    "I've tried the suggested solution but the problem still exists. Could you please take another look?",
                    "The issue appears to be resolved now. Thank you so much for your assistance!",
                    "I have additional information that might help with the troubleshooting process.",
                    "This is quite urgent as it's preventing me from completing important work.",
                    "Just wanted to follow up on this ticket. Any updates would be appreciated."
                };
                return userComments[_random.Next(userComments.Length)];
            }
        }

        /// <summary>
        /// Simple password hashing (in production, use BCrypt or similar)
        /// </summary>
        private string HashPassword(string password)
        {
            // This is a simplified hash for demo purposes
            // In production, use proper password hashing like BCrypt.Net
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password + "salt"));
        }

        /// <summary>
        /// Get seeding statistics for verification
        /// </summary>
        public async Task<SeedingStatistics> GetSeedingStatisticsAsync()
        {
            var userCount = await _userRepository.GetCountAsync();
            var ticketCount = await _ticketRepository.GetCountAsync();
            var departmentCount = await _context.Departments.CountDocumentsAsync(d => true);

            var regularUsers = await _userRepository.GetByRoleAsync(UserRole.Regular);
            var serviceDeskUsers = await _userRepository.GetByRoleAsync(UserRole.ServiceDesk);

            var openTickets = await _ticketRepository.GetTicketsByStatusAsync(TicketStatus.Open);
            var resolvedTickets = await _ticketRepository.GetTicketsByStatusAsync(TicketStatus.Resolved);
            var closedTickets = await _ticketRepository.GetTicketsByStatusAsync(TicketStatus.Closed);

            return new SeedingStatistics
            {
                TotalUsers = (int)userCount,
                RegularUsers = regularUsers.Count(),
                ServiceDeskUsers = serviceDeskUsers.Count(),
                TotalTickets = (int)ticketCount,
                OpenTickets = openTickets.Count(),
                ResolvedTickets = resolvedTickets.Count(),
                ClosedTickets = closedTickets.Count(),
                TotalDepartments = (int)departmentCount
            };
        }
    }

    /// <summary>
    /// Statistics about the seeded data for verification purposes
    /// </summary>
    public class SeedingStatistics
    {
        public int TotalUsers { get; set; }
        public int RegularUsers { get; set; }
        public int ServiceDeskUsers { get; set; }
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int ResolvedTickets { get; set; }
        public int ClosedTickets { get; set; }
        public int TotalDepartments { get; set; }

        public override string ToString()
        {
            return $@"Database Seeding Statistics:
Users: {TotalUsers} total ({RegularUsers} Regular, {ServiceDeskUsers} Service Desk)
Tickets: {TotalTickets} total ({OpenTickets} Open, {ResolvedTickets} Resolved, {ClosedTickets} Closed)
Departments: {TotalDepartments}";
        }
    }
}