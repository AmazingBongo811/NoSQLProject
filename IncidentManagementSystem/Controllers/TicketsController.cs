using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IncidentManagementSystem.Repositories;
using IncidentManagementSystem.Models;
using System.Security.Claims;

namespace IncidentManagementSystem.Controllers
{
    [Authorize]
    public class TicketsController : Controller
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<TicketsController> _logger;

        public TicketsController(
            ITicketRepository ticketRepository,
            IUserRepository userRepository,
            ILogger<TicketsController> logger)
        {
            _ticketRepository = ticketRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        // GET: /Tickets
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Check if user is service desk
                var isServiceDesk = User.IsInRole("ServiceDesk");

                List<Ticket> tickets;
                if (isServiceDesk)
                {
                    // Service desk sees all tickets
                    tickets = (await _ticketRepository.GetAllAsync()).ToList();
                }
                else
                {
                    // Regular users see only their tickets
                    tickets = (await _ticketRepository.GetTicketsByReporterAsync(userId)).ToList();
                }

                // Order by most recent first
                tickets = tickets.OrderByDescending(t => t.CreatedAt).ToList();

                ViewBag.IsServiceDesk = isServiceDesk;
                return View(tickets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tickets for user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return View("Error");
            }
        }

        // GET: /Tickets/Details/5
        public async Task<IActionResult> Details(string id)
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

                // Check authorization - users can only view their own tickets unless they're service desk
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isServiceDesk = User.IsInRole("ServiceDesk");

                if (!isServiceDesk && ticket.Reporter?.UserId != userId)
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

        // GET: /Tickets/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Tickets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Ticket ticket)
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

                // Set ticket properties
                ticket.Reporter = new UserInfo
                {
                    UserId = userId,
                    Email = userEmail ?? "",
                    FullName = userName ?? ""
                };

                ticket.CreatedAt = DateTime.UtcNow;
                ticket.UpdatedAt = DateTime.UtcNow;
                ticket.Status = TicketStatus.Open;

                await _ticketRepository.CreateAsync(ticket);

                TempData["Success"] = "Ticket created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ticket");
                ModelState.AddModelError("", "Error creating ticket. Please try again.");
                return View(ticket);
            }
        }
    }
}
