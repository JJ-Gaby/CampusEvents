using CampusEvents.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusEvents.Tests;

[ApiController]
[Route("__test-only/organizer")]
[Authorize(Roles = RoleNames.Organizer)]
public sealed class TestOnlyOrganizerController : ControllerBase
{
	[HttpGet]
	public IActionResult Index()
	{
		return Ok("Organizer-only test endpoint");
	}
}
