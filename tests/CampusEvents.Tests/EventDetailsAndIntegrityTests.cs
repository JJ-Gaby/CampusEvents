using CampusEvents.ViewModels;
using CampusEvents.Controllers;
using CampusEvents.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace CampusEvents.Tests;

public class EventDetailsAndIntegrityTests
{
    [Fact]
    public async Task Details_Published_ReturnsEventWithCategoryAndOrganizer()
    {
        await using var database = await IsolatedSqlServerDatabase.CreateAsync();
        int eventId;
        await using (var arrange = database.CreateContext())
        {
            var item = CreateEvent(50);
            arrange.Events.Add(item);
            await arrange.SaveChangesAsync();
            eventId = item.Id;
        }

        await using var verify = database.CreateContext();
        var controller = new EventsController(NullLogger<EventsController>.Instance, verify, new FakeTimeProvider(), null!)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var view = Assert.IsType<ViewResult>(await controller.Details(eventId));
        var model = Assert.IsType<EventDetailsViewModel>(view.Model).EventItem;
        Assert.Equal(eventId, model.Id);
        Assert.Equal("Published Details Example", model.Title);
        Assert.Equal("Academic", model.Category.Name);
        Assert.Equal("Test Organizer", model.Organizer.DisplayName);
        Assert.Equal(StatusCodes.Status200OK, controller.Response.StatusCode);
    }

    [Fact]
    public async Task SaveChanges_ZeroCapacity_IsRejectedAndNotPersisted()
    {
        await using var database = await IsolatedSqlServerDatabase.CreateAsync();
        await using (var arrange = database.CreateContext())
        {
            arrange.Events.Add(CreateEvent(0));
            await Assert.ThrowsAsync<DbUpdateException>(() => arrange.SaveChangesAsync());
        }

        await using var verify = database.CreateContext();
        Assert.Empty(await verify.Events.AsNoTracking().ToListAsync());
    }

    private static Event CreateEvent(int capacity) => new()
    {
        Title = "Published Details Example", Description = "Relational Details regression",
        Location = "Campus Student Center", StartDateTime = new DateTime(2026, 6, 1, 12, 0, 0),
        EndDateTime = new DateTime(2026, 6, 1, 14, 0, 0), Capacity = capacity,
        Status = EventStatus.Published, CreatedAt = new DateTime(2026, 4, 15, 12, 0, 0),
        Category = new Category { Name = "Academic" },
        Organizer = new ApplicationUser { Id = "details-organizer", DisplayName = "Test Organizer" }
    };
}
