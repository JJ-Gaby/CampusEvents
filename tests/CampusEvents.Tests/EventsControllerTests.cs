using System.Security.Claims;
using CampusEvents.Controllers;
using CampusEvents.Data;
using CampusEvents.Models;
using CampusEvents.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
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
			"CemsFixedUtcMinus4", TimeSpan.FromHours(-4), "CEMS fixed UTC-04 test zone", "CEMS fixed UTC-04 test zone");
		clock.SetLocalTimeZone(zone);
		return clock;
	}

	private static Event MakeEvent(string title, EventStatus status, DateTime start,
		int categoryId, string organizerId, string location = "Campus Student Center")
	{
		return new Event
		{
			Title = title, Description = "CEMS relational regression example",
			Location = location, StartDateTime = start, EndDateTime = start.AddHours(2), Capacity = 50,
			Status = status, CreatedAt = start.AddDays(-7), CategoryId = categoryId, OrganizerId = organizerId
		};
	}

	private static EventsController CreateController(
		ApplicationDbContext context, FakeTimeProvider clock, string queryString = "")
	{
		var httpContext = new DefaultHttpContext();
		httpContext.Request.QueryString = new QueryString(queryString);

		var controller = new EventsController(NullLogger<EventsController>.Instance, context, clock, null!)
		{
			ControllerContext = new ControllerContext
			{
				HttpContext = httpContext
			}
		};

		// Provide a return URL for the error View.
		controller.Url = new EventsIndexUrlHelper(controller.ControllerContext);
		return controller;
	}

	// Supply the return link for direct controller action tests.
	private sealed class EventsIndexUrlHelper : IUrlHelper
	{
		public EventsIndexUrlHelper(ActionContext actionContext)
		{
			ActionContext = actionContext;
		}

		public ActionContext ActionContext { get; }
		public string? Action(UrlActionContext context) => "/Events";
		public string? Content(string? contentPath) => contentPath;
		public bool IsLocalUrl(string? url) => url is not null && url.StartsWith("/", StringComparison.Ordinal);
		public string? Link(string? routeName, object? values) => null;
		public string? RouteUrl(UrlRouteContext context) => null;
	}

	[Fact]
	public async Task Index_ReturnsPublishedUpcomingEventsInChronologicalOrder()
	{
		var clock = FixedClock();
		var now = clock.GetLocalNow().DateTime;
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();

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

			arrange.Events.AddRange(
				MakeEvent("Later Workshop", EventStatus.Published, now.AddDays(10), category.Id, organizer.Id),
				MakeEvent("Earlier Seminar", EventStatus.Published, now.AddDays(2), category.Id, organizer.Id),
				MakeEvent("Draft Planning", EventStatus.Draft, now.AddDays(3), category.Id, organizer.Id),
				MakeEvent("Cancelled Mixer", EventStatus.Cancelled, now.AddDays(4), category.Id, organizer.Id),
				MakeEvent("Past Panel", EventStatus.Published, now.AddDays(-2), category.Id, organizer.Id));
			await arrange.SaveChangesAsync();
		}

		await using var verify = database.CreateContext();
		var controller = CreateController(verify, clock);
		var view = Assert.IsType<ViewResult>(await controller.Index(null, null, null, null));
		var model = Assert.IsType<EventSearchViewModel>(view.Model);

		Assert.Collection(model.Events, item => Assert.Equal("Earlier Seminar", item.Title),
			item => Assert.Equal("Later Workshop", item.Title));
		Assert.Equal(2, model.TotalCount);
		Assert.Equal(1, model.TotalPages);
		Assert.Equal(10, model.PageSize);
		Assert.All(model.Events, item => Assert.Equal(EventStatus.Published, item.Status));
		Assert.All(model.Events, item => Assert.NotNull(item.Category));
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
		foreach (var requestedId in new[] { draftId, int.MaxValue })
		{
			var controller = CreateController(verify, clock);
			var result = Assert.IsType<ViewResult>(await controller.Details(requestedId));
			Assert.Equal("EventNotFound", result.ViewName);
			Assert.Equal(StatusCodes.Status404NotFound, controller.Response.StatusCode);
		}
	}

	[Fact]
	public async Task Index_SearchAndCategory_ReturnsOnlyMatchingPublicEvents()
	{
		var clock = FixedClock();
		var now = clock.GetLocalNow().DateTime;
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		int academicId;

		await using (var arrange = database.CreateContext())
		{
			var academic = new Category { Name = "Academic" };
			var social = new Category { Name = "Social" };
			var organizer = new ApplicationUser
			{
				Id = "filter-test-organizer", DisplayName = "Student Learning Office"
			};
			arrange.Categories.AddRange(academic, social);
			arrange.Users.Add(organizer);
			await arrange.SaveChangesAsync();
			academicId = academic.Id;

			arrange.Events.AddRange(
				MakeEvent("Library Workshop", EventStatus.Published, now.AddDays(2), academic.Id, organizer.Id),
				MakeEvent("Study Skills", EventStatus.Published, now.AddDays(3), academic.Id, organizer.Id, "Library Annex"),
				MakeEvent("Library Mixer", EventStatus.Published, now.AddDays(4), social.Id, organizer.Id),
				MakeEvent("Library Draft", EventStatus.Draft, now.AddDays(5), academic.Id, organizer.Id),
				MakeEvent("Library Cancelled", EventStatus.Cancelled, now.AddDays(6), academic.Id, organizer.Id),
				MakeEvent("Library Archive", EventStatus.Published, now.AddDays(-2), academic.Id, organizer.Id),
				MakeEvent("Career Fair", EventStatus.Published, now.AddDays(7), academic.Id, organizer.Id));
			await arrange.SaveChangesAsync();
		}

		await using var verify = database.CreateContext();
		var controller = CreateController(verify, clock, $"?search=%20Library%20&categoryId={academicId}&page=1&pageSize=10");
		var view = Assert.IsType<ViewResult>(await controller.Index(" Library ", academicId, 1, 10));
		var model = Assert.IsType<EventSearchViewModel>(view.Model);

		Assert.Collection(model.Events, item => Assert.Equal("Library Workshop", item.Title),
			item => Assert.Equal("Study Skills", item.Title));
		Assert.Equal(2, model.TotalCount);
		Assert.Equal(1, model.TotalPages);
		Assert.Equal(" Library ", model.Search);
		Assert.Equal(academicId, model.CategoryId);
		Assert.Equal(10, model.PageSize);
	}

	[Theory]
	[InlineData(10, 6, 5)]
	[InlineData(25, 3, 5)]
	[InlineData(50, 2, 5)]
	public async Task Index_Pagination_UsesAllowedSizesAndStablePageBoundaries(
		int pageSize, int expectedPages, int expectedLastPageCount)
	{
		var clock = FixedClock();
		var now = clock.GetLocalNow().DateTime;
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();

		await using (var arrange = database.CreateContext())
		{
			var category = new Category { Name = "Academic" };
			var organizer = new ApplicationUser
			{
				Id = "paging-test-organizer", DisplayName = "Campus Programs"
			};
			arrange.Categories.Add(category);
			arrange.Users.Add(organizer);
			await arrange.SaveChangesAsync();

			for (var number = 1; number <= 55; number++)
			{
				arrange.Events.Add(MakeEvent( $"Campus Workshop {number:00}", EventStatus.Published,
					now.AddDays(1 + (number - 1) / 2), category.Id, organizer.Id));
			}
			await arrange.SaveChangesAsync();
		}

		await using var verify = database.CreateContext();
		var controller = CreateController(verify, clock, $"?page=999&pageSize={pageSize}");
		var lastView = Assert.IsType<ViewResult>(await controller.Index(null, null, 999, pageSize));
		var last = Assert.IsType<EventSearchViewModel>(lastView.Model);

		Assert.Equal(55, last.TotalCount);
		Assert.Equal(expectedPages, last.TotalPages);
		Assert.Equal(expectedPages, last.CurrentPage);
		Assert.Equal(expectedLastPageCount, last.Events.Count);
		Assert.Equal(pageSize, last.PageSize);
		Assert.Equal(new[] { 10, 25, 50 }, last.PageSizeOptions);
		Assert.True(last.Events.Zip(last.Events.Skip(1), (a, b) => a.StartDateTime < b.StartDateTime ||
			(a.StartDateTime == b.StartDateTime && a.Id < b.Id)).All(ordered => ordered));

		controller.Request.QueryString = new QueryString($"?page=0&pageSize={pageSize}");
		var firstView = Assert.IsType<ViewResult>(await controller.Index(null, null, 0, pageSize));
		var first = Assert.IsType<EventSearchViewModel>(firstView.Model);
		Assert.Equal(1, first.CurrentPage);
		Assert.Equal(pageSize, first.Events.Count);
	}


	[Theory]
	[InlineData(null, 1)]
	[InlineData(0, 1)]
	[InlineData(-3, 1)]
	[InlineData(999, 2)]
	public async Task Index_PageNumber_NormalizesAgainstFilteredMatches(int? requestedPage, int expectedPage)
	{
		var clock = FixedClock();
		var now = clock.GetLocalNow().DateTime;
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		int academicId;

		await using (var arrange = database.CreateContext())
		{
			var category = new Category { Name = "Academic" };
			var organizer = new ApplicationUser { Id = "page-boundary-organizer", DisplayName = "Campus Office" };
			arrange.Categories.Add(category);
			arrange.Users.Add(organizer);
			await arrange.SaveChangesAsync();
			academicId = category.Id;

			for (var number = 1; number <= 11; number++)
			{
				arrange.Events.Add(MakeEvent($"Academic Workshop {number:00}", EventStatus.Published,
					now.AddDays(number), category.Id, organizer.Id));
			}
			arrange.Events.Add(MakeEvent("Social Workshop", EventStatus.Published,
				now.AddDays(1), category.Id, organizer.Id, "Remote Campus"));
			await arrange.SaveChangesAsync();
		}

		await using var verify = database.CreateContext();
		var controller = CreateController(verify, clock, $"?search=Academic&categoryId={academicId}");
		var view = Assert.IsType<ViewResult>(await controller.Index("Academic", academicId, requestedPage, 10));
		var model = Assert.IsType<EventSearchViewModel>(view.Model);
		Assert.Equal(11, model.TotalCount);
		Assert.Equal(2, model.TotalPages);
		Assert.Equal(expectedPage, model.CurrentPage);
		Assert.Equal(expectedPage == 2 ? 1 : 10, model.Events.Count);
	}

	[Theory]
	[InlineData("1", 1)]
	[InlineData("abc", null)]
	[InlineData("-10", -10)]
	public async Task Index_UnsupportedPageSize_ReturnsBadRequestViewAnd400(string requestedSize, int? boundValue)
	{
		var clock = FixedClock();
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		await using var verify = database.CreateContext();
		var controller = CreateController(verify, clock, $"?pageSize={requestedSize}");

		var view = Assert.IsType<ViewResult>(await controller.Index(null, null, null, boundValue));
		var error = Assert.IsType<ErrorPageViewModel>(view.Model);
		Assert.Equal("BadRequest", view.ViewName);
		Assert.Equal(StatusCodes.Status400BadRequest, controller.Response.StatusCode);
		Assert.Equal("Invalid Page Size", error.Title);
		Assert.Equal("/Events", error.ReturnUrl);
	}

	[Fact]
	public async Task Index_NoPublicMatches_NormalizesPageAndReturnsEmptyState()
	{
		var clock = FixedClock();
		var now = clock.GetLocalNow().DateTime;
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		int academicId;

		await using (var arrange = database.CreateContext())
		{
			var academic = new Category { Name = "Academic" };
			var organizer = new ApplicationUser
			{
				Id = "zero-results-organizer", DisplayName = "CEMS Test Organizer"
			};
			arrange.Categories.Add(academic);
			arrange.Users.Add(organizer);
			await arrange.SaveChangesAsync();
			academicId = academic.Id;

			arrange.Events.AddRange(MakeEvent("Secret Research", EventStatus.Draft,
					now.AddDays(3), academic.Id, organizer.Id), MakeEvent("Public Seminar", EventStatus.Published,
					now.AddDays(4), academic.Id, organizer.Id));
			await arrange.SaveChangesAsync();
		}

		await using var verify = database.CreateContext();
		var controller = CreateController(verify, clock, $"?search=Secret&categoryId={academicId}&page=999&pageSize=10");
		var view = Assert.IsType<ViewResult>(await controller.Index("Secret", academicId, 999, 10));
		var model = Assert.IsType<EventSearchViewModel>(view.Model);

		Assert.Empty(model.Events);
		Assert.Equal(0, model.TotalCount);
		Assert.Equal(0, model.TotalPages);
		Assert.Equal(1, model.CurrentPage);
		Assert.Equal(10, model.PageSize);
		Assert.Equal(StatusCodes.Status200OK, controller.Response.StatusCode);
	}

	[Fact]
	public async Task Index_OmittedPageSize_DefaultsToTenAndReturnsSecondPage()
	{
		var clock = FixedClock();
		var now = clock.GetLocalNow().DateTime;
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		await using (var arrange = database.CreateContext())
		{
			var category = new Category { Name = "Academic" };
			var organizer = new ApplicationUser { Id = "default-size-organizer", DisplayName = "Campus Office" };
			arrange.Categories.Add(category);
			arrange.Users.Add(organizer);
			await arrange.SaveChangesAsync();
			for (var number = 1; number <= 12; number++)
			{
				arrange.Events.Add(MakeEvent($"Default Boundary {number:00}", EventStatus.Published,
					now.AddDays(number), category.Id, organizer.Id));
			}
			arrange.Events.Add(MakeEvent("Default Boundary starts now", EventStatus.Published,
				now, category.Id, organizer.Id));
			await arrange.SaveChangesAsync();
		}

		await using var verify = database.CreateContext();
		var controller = CreateController(verify, clock, "?search=Default%20Boundary&page=2");
		var view = Assert.IsType<ViewResult>(await controller.Index("Default Boundary", null, 2, null));
		var model = Assert.IsType<EventSearchViewModel>(view.Model);
		Assert.Equal(12, model.TotalCount);
		Assert.Equal(2, model.CurrentPage);
		Assert.Equal(10, model.PageSize);
		Assert.Equal(2, model.TotalPages);
		Assert.Equal(new[] { "Default Boundary 11", "Default Boundary 12" }, model.Events.Select(item => item.Title));
	}

	[Fact]
	public async Task Details_PublishedEvent_ProvidesCurrentAvailabilityModel()
	{
		var clock = FixedClock();
		var now = clock.GetLocalNow().DateTime;
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		int publishedId;

		await using (var arrange = database.CreateContext())
		{
			var category = new Category { Name = "Details Capacity" };
			var organizer = new ApplicationUser { Id = "details-capacity-owner", DisplayName = "Events Team" };
			var participant = new ApplicationUser { Id = "details-capacity-participant", DisplayName = "Participant" };
			arrange.Categories.Add(category);
			arrange.Users.AddRange(organizer, participant);
			await arrange.SaveChangesAsync();

			var eventItem = MakeEvent("Capacity Workshop", EventStatus.Published, now.AddDays(5),
				category.Id, organizer.Id);
			eventItem.Capacity = 2;
			arrange.Events.Add(eventItem);
			await arrange.SaveChangesAsync();
			publishedId = eventItem.Id;

			arrange.Registrations.Add(new Registration
			{
				UserId = participant.Id,
				EventId = publishedId,
				CreatedAt = now.AddHours(-1)
			});
			await arrange.SaveChangesAsync();
		}

		await using var verify = database.CreateContext();
		var controller = CreateController(verify, clock);
		controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
		var view = Assert.IsType<ViewResult>(await controller.Details(publishedId));
		var model = Assert.IsType<EventDetailsViewModel>(view.Model);

		Assert.Equal(publishedId, model.EventItem.Id);
		Assert.Equal(EventStatus.Published, model.EventItem.Status);
		Assert.Equal(1, model.CurrentRegistrationCount);
		Assert.Equal(1, model.RemainingCapacity);
		Assert.False(model.IsCurrentUserRegistered);
	}
}
