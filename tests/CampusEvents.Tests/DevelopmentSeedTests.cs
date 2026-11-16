using CampusEvents.Data;
using CampusEvents.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace CampusEvents.Tests;

public class DevelopmentSeedTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task SeedAsync_IsIdempotentWithEmptyAndPartialDevelopmentData(int scenario)
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 4, 15, 16, 0, 0, TimeSpan.Zero));
        clock.SetLocalTimeZone(TimeZoneInfo.CreateCustomTimeZone("CemsUtcMinus4", TimeSpan.FromHours(-4), "UTC-04", "UTC-04"));
        await using var database = await IsolatedSqlServerDatabase.CreateAsync();
        int? retainedId = null;
        var retainedStart = new DateTime(2026, 7, 1, 18, 0, 0);

        await using (var arrange = database.CreateContext())
        {
            if (scenario != 0)
            {
                var category = new Category { Name = "Academic" };
                var organizer = new ApplicationUser
                {
                    Id = scenario == 1 ? "unrelated-organizer" : "seed-organizer-001",
                    DisplayName = "Retained organizer"
                };
                arrange.Categories.Add(category);
                arrange.Users.Add(organizer);
                await arrange.SaveChangesAsync();
                var existing = new Event
                {
                    Title = "Library Research Night", Description = "Retain this existing row",
                    Location = "Retained location", StartDateTime = retainedStart,
                    EndDateTime = retainedStart.AddHours(2), Capacity = 40,
                    Status = EventStatus.Published, CreatedAt = retainedStart.AddDays(-1),
                    CategoryId = category.Id, OrganizerId = organizer.Id
                };
                arrange.Events.Add(existing);
                await arrange.SaveChangesAsync();
                retainedId = existing.Id;
            }
        }

        await using (var first = database.CreateContext())
            await DbInitializer.SeedAsync(first, clock);

        List<(int Id, DateTime Start)> before;
        await using (var snapshot = database.CreateContext())
            before = (await snapshot.Events.AsNoTracking().OrderBy(item => item.Id).ToListAsync())
                .Select(item => (item.Id, item.StartDateTime)).ToList();

        clock.Advance(TimeSpan.FromDays(7));
        await using (var second = database.CreateContext())
            await DbInitializer.SeedAsync(second, clock);

        await using var verify = database.CreateContext();
        var events = await verify.Events.AsNoTracking().OrderBy(item => item.Id).ToListAsync();
        Assert.Equal(before, events.Select(item => (item.Id, item.StartDateTime)).ToList());
        Assert.Equal(3, await verify.Categories.CountAsync());
        Assert.Equal(1, await verify.Users.CountAsync(item => item.Id == "seed-organizer-001"));
        var seedEvents = events.Where(item => item.OrganizerId == "seed-organizer-001").ToList();
        Assert.Equal(6, seedEvents.Count);
        Assert.Equal(6, seedEvents.Select(item => item.Title).Distinct().Count());
        Assert.Equal(scenario == 1 ? 1 : 0, events.Count(item => item.OrganizerId != "seed-organizer-001"));
        if (retainedId.HasValue)
        {
            var retained = Assert.Single(events, item => item.Id == retainedId.Value);
            Assert.Equal(retainedStart, retained.StartDateTime);
            Assert.Equal("Retain this existing row", retained.Description);
            Assert.Equal("Retained location", retained.Location);
        }
    }

    [Fact]
    public async Task SeedAsync_AddsWorkshopToOriginalSeedDataWithoutChangingExistingRows()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 4, 15, 16, 0, 0, TimeSpan.Zero));
        clock.SetLocalTimeZone(TimeZoneInfo.CreateCustomTimeZone("CemsUtcMinus4", TimeSpan.FromHours(-4), "UTC-04", "UTC-04"));
        await using var database = await IsolatedSqlServerDatabase.CreateAsync();
        await using (var arrange = database.CreateContext())
        {
            await DbInitializer.SeedAsync(arrange, clock);
            var workshop = await arrange.Events.SingleAsync(item => item.Title == "Study Skills Workshop");
            arrange.Events.Remove(workshop);
            await arrange.SaveChangesAsync();
        }

        List<(int Id, DateTime Start)> original;
        await using (var snapshot = database.CreateContext())
            original = (await snapshot.Events.AsNoTracking().OrderBy(item => item.Id).ToListAsync())
                .Select(item => (item.Id, item.StartDateTime)).ToList();
        Assert.Equal(5, original.Count);

        clock.Advance(TimeSpan.FromDays(1));
        await using (var first = database.CreateContext())
            await DbInitializer.SeedAsync(first, clock);
        await using (var second = database.CreateContext())
            await DbInitializer.SeedAsync(second, clock);

        await using var verify = database.CreateContext();
        var events = await verify.Events.AsNoTracking().OrderBy(item => item.Id).ToListAsync();
        var originalIds = original.Select(item => item.Id).ToHashSet();
        Assert.Equal(original, events.Where(item => originalIds.Contains(item.Id))
            .Select(item => (item.Id, item.StartDateTime)).ToList());
        Assert.Equal(6, events.Count);
        var added = Assert.Single(events, item => item.Title == "Study Skills Workshop");
        Assert.Equal(new DateTime(2026, 6, 15, 16, 0, 0), added.StartDateTime);
        Assert.Equal(added.StartDateTime.AddHours(1), added.EndDateTime);
        Assert.Equal(40, added.Capacity);
        Assert.Equal(EventStatus.Published, added.Status);
    }
}
