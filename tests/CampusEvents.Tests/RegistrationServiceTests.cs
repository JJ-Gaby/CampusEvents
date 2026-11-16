using CampusEvents.Models;
using CampusEvents.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace CampusEvents.Tests;

public class RegistrationServiceTests
{
	private static FakeTimeProvider FixedClock()
	{
		var clock = new FakeTimeProvider(new DateTimeOffset(2026, 4, 15, 16, 0, 0, TimeSpan.Zero));
		clock.SetLocalTimeZone(TimeZoneInfo.CreateCustomTimeZone("RegistrationUtcMinus4", TimeSpan.FromHours(-4),
			"CEMS UTC-04 test zone", "CEMS UTC-04 test zone"));
		return clock;
	}

	private static async Task<(int eventId, string userA, string userB)> ScenarioAsync(
		IsolatedSqlServerDatabase database, EventStatus status, int capacity, FakeTimeProvider clock)
	{
		await using var db = database.CreateContext();
		var organizer = new ApplicationUser { Id = Guid.NewGuid().ToString("N"), UserName = "organizer@test.local",
			Email = "organizer@test.local", DisplayName = "Test Organizer" };
		var a = new ApplicationUser { Id = Guid.NewGuid().ToString("N"), UserName = "a@test.local",
			Email = "a@test.local", DisplayName = "Test A" };
		var b = new ApplicationUser { Id = Guid.NewGuid().ToString("N"), UserName = "b@test.local",
			Email = "b@test.local", DisplayName = "Test B" };
		db.Users.AddRange(organizer, a, b);
		var category = new Category { Name = "Concurrency Test Category" };
		db.Categories.Add(category);
		await db.SaveChangesAsync();
		var start = clock.GetLocalNow().DateTime.AddDays(1);
		var item = new Event { Title = "Capacity Test Event", Description = "Current Registration test",
			Location = "Library Room", CategoryId = category.Id, OrganizerId = organizer.Id,
			Status = status, StartDateTime = start, EndDateTime = start.AddHours(2), Capacity = capacity,
			CreatedAt = clock.GetLocalNow().DateTime };
		db.Events.Add(item);
		await db.SaveChangesAsync();
		return (item.Id, a.Id, b.Id);
	}

	[Fact]
	public async Task DraftEvent_IsUnavailable()
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		var clock = FixedClock();
		var (eventId, userA, _) = await ScenarioAsync(database, EventStatus.Draft, 2, clock);
		await using var db = database.CreateContext();
		var result = await new RegistrationService(db, clock).TryRegisterAsync(userA, eventId);
		Assert.Equal(RegistrationAttemptStatus.Unavailable, result);
		Assert.Equal(0, await db.Registrations.CountAsync());
	}

	[Fact]
	public async Task SameUser_CannotRegisterTwice()
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		var clock = FixedClock();
		var (eventId, userA, _) = await ScenarioAsync(database, EventStatus.Published, 2, clock);
		await using (var first = database.CreateContext())
			Assert.Equal(RegistrationAttemptStatus.Success,
				await new RegistrationService(first, clock).TryRegisterAsync(userA, eventId));
		await using (var second = database.CreateContext())
			Assert.Equal(RegistrationAttemptStatus.Duplicate,
				await new RegistrationService(second, clock).TryRegisterAsync(userA, eventId));
		await using var check = database.CreateContext();
		Assert.Equal(1, await check.Registrations.CountAsync(r => r.UserId == userA && r.EventId == eventId));
	}

	[Fact]
	public async Task CancelledEvent_IsUnavailable()
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		var clock = FixedClock();
		var (eventId, userA, _) = await ScenarioAsync(database, EventStatus.Cancelled, 2, clock);
		await using (var db = database.CreateContext())
			Assert.Equal(RegistrationAttemptStatus.Unavailable,
				await new RegistrationService(db, clock).TryRegisterAsync(userA, eventId));
		await using var verify = database.CreateContext();
		Assert.Empty(await verify.Registrations.ToListAsync());
	}

	[Fact]
	public async Task Register_ExactlyAtStart_LeavesRowsUnchanged()
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		var clock = FixedClock();
		var (eventId, userA, _) = await ScenarioAsync(database, EventStatus.Published, 2, clock);
		await using (var setup = database.CreateContext())
		{
			var item = await setup.Events.SingleAsync(e => e.Id == eventId);
			item.StartDateTime = clock.GetLocalNow().DateTime;
			item.EndDateTime = item.StartDateTime.AddHours(2);
			await setup.SaveChangesAsync();
		}
		await using (var db = database.CreateContext())
			Assert.Equal(RegistrationAttemptStatus.Unavailable,
				await new RegistrationService(db, clock).TryRegisterAsync(userA, eventId));
		await using var verify = database.CreateContext();
		Assert.Empty(await verify.Registrations.ToListAsync());
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task SqlConstraints_RejectDuplicatePairOrMissingEvent(bool duplicate)
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		var clock = FixedClock();
		var (eventId, userA, _) = await ScenarioAsync(database, EventStatus.Published, 2, clock);
		if (duplicate)
		{
			await using var setup = database.CreateContext();
			setup.Registrations.Add(new Registration { UserId = userA, EventId = eventId,
				CreatedAt = clock.GetLocalNow().DateTime });
			await setup.SaveChangesAsync();
		}
		await using (var db = database.CreateContext())
		{
			db.Registrations.Add(new Registration { UserId = userA,
				EventId = duplicate ? eventId : int.MaxValue, CreatedAt = clock.GetLocalNow().DateTime });
			var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
			var sql = Assert.IsType<Microsoft.Data.SqlClient.SqlException>(exception.InnerException);
			if (duplicate) Assert.Contains(sql.Number, new[] { 2601, 2627 });
			else Assert.Equal(547, sql.Number);
		}
		await using var verify = database.CreateContext();
		Assert.Equal(duplicate ? 1 : 0, await verify.Registrations.CountAsync());
	}

	[Fact]
	public async Task CompetingRequests_ForLastSeat_OnlyOnePersists()
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		var clock = FixedClock();
		var (eventId, userA, userB) = await ScenarioAsync(database, EventStatus.Published, 1, clock);
		var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		async Task<RegistrationAttemptStatus> AttemptAsync(string userId)
		{
			await gate.Task;
			await using var db = database.CreateContext();
			return await new RegistrationService(db, clock).TryRegisterAsync(userId, eventId);
		}
		var first = AttemptAsync(userA);
		var second = AttemptAsync(userB);
		gate.SetResult(true);
		var outcomes = await Task.WhenAll(first, second);
		Assert.Equal(1, outcomes.Count(x => x == RegistrationAttemptStatus.Success));
		Assert.Equal(1, outcomes.Count(x => x == RegistrationAttemptStatus.Full));
		await using var verify = database.CreateContext();
		var count = await verify.Registrations.CountAsync(r => r.EventId == eventId);
		var capacity = await verify.Events.Where(e => e.Id == eventId).Select(e => e.Capacity).SingleAsync();
		Assert.Equal(1, count);
		Assert.True(count <= capacity);
	}
}
