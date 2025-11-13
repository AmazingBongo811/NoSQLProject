using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IncidentManagementSystem.Repositories;
using IncidentManagementSystem.Models;
using IncidentManagementSystem.ViewModels;
using System.Security.Claims;

namespace IncidentManagementSystem.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            ITicketRepository ticketRepository,
            IUserRepository userRepository,
            ILogger<DashboardController> logger)
        {
            _ticketRepository = ticketRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Get user's tickets
                var userTickets = await _ticketRepository.GetTicketsByReporterAsync(userId);
                
                // Get ticket statistics
                var allTickets = userTickets.ToList();
                var openTickets = allTickets.Count(t => t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed);
                var resolvedTickets = allTickets.Count(t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed);
                var urgentTickets = allTickets.Count(t => t.Priority == TicketPriority.High && t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed);

                // Get recent tickets (last 5)
                var recentTickets = allTickets.OrderByDescending(t => t.CreatedAt).Take(5).ToList();

                var viewModel = new DashboardViewModel
                {
                    TotalTickets = allTickets.Count,
                    OpenTickets = openTickets,
                    ResolvedTickets = resolvedTickets,
                    UrgentTickets = urgentTickets,
                    RecentTickets = recentTickets,
                    UserName = User.Identity?.Name ?? "User"
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard for user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return View("Error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateTicket()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                var userName = User.Identity?.Name;

                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Create a new ticket with proper UserInfo
                var ticket = new Ticket
                {
                    Reporter = new UserInfo
                    {
                        UserId = userId,
                        Username = userName ?? "",
                        Email = userEmail ?? "",
                        FullName = userName ?? ""
                    },
                    Status = TicketStatus.Open,
                    Priority = TicketPriority.Medium,
                    Category = TicketCategory.Other,
                    CreatedAt = DateTime.UtcNow
                };

                return View(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create ticket form");
                return View("Error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTicket(Ticket ticket)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(ticket);
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                var userName = User.Identity?.Name;

                // Set reporter info
                ticket.Reporter = new UserInfo
                {
                    UserId = userId ?? "",
                    Username = userName ?? "",
                    Email = userEmail ?? "",
                    FullName = userName ?? ""
                };

                ticket.CreatedAt = DateTime.UtcNow;
                ticket.UpdatedAt = DateTime.UtcNow;
                ticket.Status = TicketStatus.Open;

                // Add initial activity
                var initialActivity = new TicketActivity
                {
                    Action = "Created",
                    PerformedBy = ticket.Reporter,
                    Timestamp = DateTime.UtcNow,
                    Description = "Ticket created"
                };

                ticket.Activities = new List<TicketActivity> { initialActivity };

                await _ticketRepository.CreateAsync(ticket);

                TempData["SuccessMessage"] = $"Ticket has been created successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ticket for user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                ModelState.AddModelError("", "An error occurred while creating the ticket. Please try again.");
                return View(ticket);
            }
        }

        [HttpGet]
        public async Task<IActionResult> MyTickets()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var tickets = await _ticketRepository.GetTicketsByReporterAsync(userId);
                var sortedTickets = tickets.OrderByDescending(t => t.CreatedAt).ToList();

                return View(sortedTickets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tickets for user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return View("Error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ViewTicket(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return NotFound();
                }

                var ticket = await _ticketRepository.GetByIdAsync(id);
                if (ticket == null)
                {
                    return NotFound();
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                // Check if user owns this ticket or is service desk
                if (ticket.Reporter?.UserId != userId && !User.IsInRole("ServiceDesk"))
                {
                    return Forbid();
                }

                return View(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ticket {TicketId}", id);
                return View("Error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(string ticketId, string commentContent)
        {
            try
            {
                if (string.IsNullOrEmpty(ticketId) || string.IsNullOrWhiteSpace(commentContent))
                {
                    return BadRequest();
                }

                var ticket = await _ticketRepository.GetByIdAsync(ticketId);
                if (ticket == null)
                {
                    return NotFound();
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                // Check if user owns this ticket or is service desk
                if (ticket.Reporter?.UserId != userId && !User.IsInRole("ServiceDesk"))
                {
                    return Forbid();
                }

                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                var userName = User.Identity?.Name;

                // Add new comment
                var newComment = new TicketComment
                {
                    Content = commentContent.Trim(),
                    Author = new UserInfo
                    {
                        UserId = userId ?? "",
                        Username = userName ?? "",
                        Email = userEmail ?? "",
                        FullName = userName ?? ""
                    },
                    Timestamp = DateTime.UtcNow,
                    IsInternal = User.IsInRole("ServiceDesk")
                };

                ticket.Comments.Add(newComment);
                ticket.UpdatedAt = DateTime.UtcNow;

                await _ticketRepository.UpdateAsync(ticket.Id!, ticket);

                TempData["SuccessMessage"] = "Comment added successfully!";
                return RedirectToAction("ViewTicket", new { id = ticketId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment to ticket {TicketId}", ticketId);
                TempData["ErrorMessage"] = "Failed to add comment. Please try again.";
                return RedirectToAction("ViewTicket", new { id = ticketId });
            }
        }
    }
}