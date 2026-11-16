using CampusEvents.Data;
using CampusEvents.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CampusEvents.Controllers;

public class EventsController : Controller
{
	private readonly ILogger<EventsController> _logger;
	private readonly ApplicationDbContext _context;
	private readonly TimeProvider _timeProvider;

	public EventsController(ILogger<EventsController> logger, ApplicationDbContext context, TimeProvider timeProvider)
	{
		_logger = logger;
		_context = context;
		_timeProvider = timeProvider;
	}

	public async Task<IActionResult> Index()
	{
		_logger.LogInformation("Loading Published upcoming CEMS events.");

		var now = _timeProvider.GetLocalNow().DateTime;
		var query = _context.Events.AsNoTracking()
			.Include(eventItem => eventItem.Category)
			.Where(eventItem => eventItem.Status == EventStatus.Published && eventItem.StartDateTime > now)
			.OrderBy(eventItem => eventItem.StartDateTime).ThenBy(eventItem => eventItem.Id);

		var events = await query.ToListAsync();
		return View(events);
	}

	public async Task<IActionResult> Details(int id)
	{
		var query = _context.Events.AsNoTracking()
			.Include(item => item.Category).Include(item => item.Organizer)
			.Where(item => item.Id == id && item.Status == EventStatus.Published);

		var eventItem = await query.SingleOrDefaultAsync();

		if (eventItem is null)
		{
			return EventNotFound();
		}

		return View(eventItem);
	}

	private ViewResult EventNotFound()
	{
		Response.StatusCode = StatusCodes.Status404NotFound;
		return View("EventNotFound");
	}
}
