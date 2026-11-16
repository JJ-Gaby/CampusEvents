using CampusEvents.Controllers;
using CampusEvents.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace CampusEvents.Tests;

public class EventsControllerTests
{
	private static FakeTimeProvider FixedClock()
	{
		var clock = new FakeTimeProvider(new DateTimeOffset(2026, 4, 15, 16, 0, 0, TimeSpan.Zero));
		var zone = TimeZoneInfo.CreateCustomTimeZone(
			"CemsFixedUtcMinus4", TimeSpan.FromHours(-4), "CEMS fixed UTC-04 test zone",
			"CEMS fixed UTC-04 test zone");
		clock.SetLocalTimeZone(zone);
		return clock;
	}

	private static Event MakeEvent(string title, EventStatus status, DateTime start, int categoryId, string organizerId)
	{
		return new Event
		{
			Title = title,
			Description = "CEMS relational regression example",
			Location = "Campus Student Center",
			StartDateTime = start,
			EndDateTime = start.AddHours(2),
			Capacity = 50,
			Status = status,
			CreatedAt = start.AddDays(-7),
			CategoryId = categoryId,
			OrganizerId = organizerId
		};
	}

	[Fact]
	public async Task Index_ReturnsPublishedUpcomingEventsInChronologicalOrder()
	{
		var clock = FixedClock();
		var now = clock.GetLocalNow().DateTime;
		Assert.Equal(new DateTime(2026, 4, 15, 12, 0, 0), now);
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();

		int sameTimeAId;
		int sameTimeBId;
		await using (var arrange = database.CreateContext())
		{
			var category = new Category { Name = "Academic" };
			var organizer = new ApplicationUser
			{
				Id = "catalog-test-organizer", DisplayName = "Campus Events Office"
			};
			arrange.Categories.Add(category);
			arrange.Users.Add(organizer);
			await arrange.SaveChangesAsync();

			var sameTimeA = MakeEvent("Same-Time A", EventStatus.Published, now.AddDays(3), category.Id, organizer.Id);
			var sameTimeB = MakeEvent("Same-Time B", EventStatus.Published, now.AddDays(3), category.Id, organizer.Id);
			arrange.Events.AddRange(
				MakeEvent("Later Workshop", EventStatus.Published, now.AddDays(10), category.Id, organizer.Id),
				MakeEvent("Earlier Seminar", EventStatus.Published, now.AddDays(2), category.Id, organizer.Id),
				sameTimeB,
				sameTimeA,
				MakeEvent("Starts Now", EventStatus.Published, now, category.Id, organizer.Id),
				MakeEvent("Draft Planning", EventStatus.Draft, now.AddDays(3), category.Id, organizer.Id),
				MakeEvent("Cancelled Mixer", EventStatus.Cancelled, now.AddDays(4), category.Id, organizer.Id),
				MakeEvent("Past Panel", EventStatus.Published, now.AddDays(-2), category.Id, organizer.Id));
			await arrange.SaveChangesAsync();
			sameTimeAId = sameTimeA.Id;
			sameTimeBId = sameTimeB.Id;
		}

		await using var verify = database.CreateContext();
		var controller = new EventsController(NullLogger<EventsController>.Instance, verify, clock);
		var view = Assert.IsType<ViewResult>(await controller.Index());
		var events = Assert.IsType<List<Event>>(view.Model);

		var expectedTiedOrder = new[] { (Id: sameTimeAId, Title: "Same-Time A"),
			(Id: sameTimeBId, Title: "Same-Time B") }.OrderBy(item => item.Id).ToArray();
		var expectedTitles = new[] { "Earlier Seminar", expectedTiedOrder[0].Title,
			expectedTiedOrder[1].Title, "Later Workshop" };
		Assert.Equal(expectedTitles, events.Select(item => item.Title));
		Assert.Equal(expectedTiedOrder.Select(item => item.Id), events.Skip(1).Take(2).Select(item => item.Id));
		Assert.All(events, item => Assert.Equal(EventStatus.Published, item.Status));
		Assert.All(events, item => Assert.Equal("Academic", item.Category.Name));
	}

	[Fact]
	public async Task Details_DraftOrMissing_ReturnsEventNotFoundWith404()
	{
		var clock = FixedClock();
		var now = clock.GetLocalNow().DateTime;
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		int draftId;

		await using (var arrange = database.CreateContext())
		{
			var category = new Category { Name = "Social" };
			var organizer = new ApplicationUser
			{
				Id = "details-test-organizer", DisplayName = "Student Activities"
			};
			arrange.Categories.Add(category);
			arrange.Users.Add(organizer);
			await arrange.SaveChangesAsync();
			var draft = MakeEvent("Unpublished Mixer", EventStatus.Draft, now.AddDays(5), category.Id, organizer.Id);
			arrange.Events.Add(draft);
			await arrange.SaveChangesAsync();
			draftId = draft.Id;
		}

		await using var verify = database.CreateContext();
		foreach (int requestedId in new[] { draftId, int.MaxValue })
		{
			var controller = new EventsController(NullLogger<EventsController>.Instance, verify, clock)
			{
				ControllerContext = new ControllerContext
				{
					HttpContext = new DefaultHttpContext()
				}
			};
			var result = Assert.IsType<ViewResult>(await controller.Details(requestedId));
			Assert.Equal("EventNotFound", result.ViewName);
			Assert.Equal(StatusCodes.Status404NotFound, controller.Response.StatusCode);
		}
	}
}
