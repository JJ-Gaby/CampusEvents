using CampusEvents.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Data;

public static class DbInitializer
{
	private const string OrganizerId = "seed-organizer-001";

	public static async Task SeedAsync(ApplicationDbContext context, TimeProvider timeProvider)
	{
		// Seed independently: an unrelated Event must not suppress missing classroom records.
		var organizer = await context.Users.SingleOrDefaultAsync(user => user.Id == OrganizerId);
		if (organizer is null)
		{
			organizer = new ApplicationUser { Id = OrganizerId, DisplayName = "Campus Events Office" };
			context.Users.Add(organizer);
		}

		async Task<Category> EnsureCategoryAsync(string name)
		{
			var existing = await context.Categories.SingleOrDefaultAsync(category => category.Name == name);
			if (existing is not null) return existing;
			var category = new Category { Name = name };
			context.Categories.Add(category);
			return category;
		}

		var academic = await EnsureCategoryAsync("Academic");
		var social = await EnsureCategoryAsync("Social");
		var career = await EnsureCategoryAsync("Career");
		await context.SaveChangesAsync();

		var createdAt = timeProvider.GetLocalNow().DateTime;
		var today = createdAt.Date;

		var libraryStart = today.AddDays(30).AddHours(18);
		var careerStart = today.AddDays(50).AddHours(12);
		var studySkillsStart = today.AddDays(60).AddHours(16);
		var draftStart = today.AddDays(70).AddHours(15);
		var cancelledStart = today.AddDays(90).AddHours(20);
		var pastStart = today.AddDays(-10).AddHours(13);

		var events = new[]
		{
			new Event
			{
				Title = "Library Research Night",
				Description = "An evening workshop on research databases and source evaluation.",
				Location = "Main Library, Room 204",
				StartDateTime = libraryStart,
				EndDateTime = libraryStart.AddHours(2),
				Capacity = 60,
				Status = EventStatus.Published,
				CreatedAt = createdAt,
				CategoryId = academic.Id,
				OrganizerId = organizer.Id
			},
			new Event
			{
				Title = "Career Networking Lunch",
				Description = "Meet local employers and practice short professional introductions.",
				Location = "Student Center Ballroom",
				StartDateTime = careerStart,
				EndDateTime = careerStart.AddHours(2),
				Capacity = 120,
				Status = EventStatus.Published,
				CreatedAt = createdAt,
				CategoryId = career.Id,
				OrganizerId = organizer.Id
			},
			new Event
			{
				Title = "Study Skills Workshop",
				Description = "Practice study planning and effective learning strategies.",
				Location = "Main Library, Room 204",
				StartDateTime = studySkillsStart,
				EndDateTime = studySkillsStart.AddHours(1),
				Capacity = 40,
				Status = EventStatus.Published,
				CreatedAt = createdAt,
				CategoryId = academic.Id,
				OrganizerId = organizer.Id
			},
			new Event
			{
				Title = "Welcome Mixer Planning Draft",
				Description = "A draft event used to demonstrate that Draft Events are not public.",
				Location = "Student Center Patio",
				StartDateTime = draftStart,
				EndDateTime = draftStart.AddHours(2),
				Capacity = 80,
				Status = EventStatus.Draft,
				CreatedAt = createdAt,
				CategoryId = social.Id,
				OrganizerId = organizer.Id
			},
			new Event
			{
				Title = "Outdoor Movie Night",
				Description = "A cancelled event retained in the database for lifecycle examples.",
				Location = "Campus Green",
				StartDateTime = cancelledStart,
				EndDateTime = cancelledStart.AddHours(3),
				Capacity = 200,
				Status = EventStatus.Cancelled,
				CreatedAt = createdAt,
				CategoryId = social.Id,
				OrganizerId = organizer.Id
			},
			new Event
			{
				Title = "Alumni Panel Archive",
				Description = "A past Published event used to demonstrate upcoming-event filtering.",
				Location = "Business Building Auditorium",
				StartDateTime = pastStart,
				EndDateTime = pastStart.AddHours(2),
				Capacity = 100,
				Status = EventStatus.Published,
				CreatedAt = createdAt,
				CategoryId = career.Id,
				OrganizerId = organizer.Id
			}
		};

		// These titles are stable identifiers for the *Development sample dataset*, not
		// uniqueness requirements on Event titles created by real users.
		var existingTitles = (await context.Events.AsNoTracking()
			.Where(item => item.OrganizerId == OrganizerId)
			.Select(item => item.Title)
			.ToListAsync()).ToHashSet(StringComparer.Ordinal);
		var missingEvents = events.Where(item => !existingTitles.Contains(item.Title)).ToList();
		if (missingEvents.Count > 0)
		{
			context.Events.AddRange(missingEvents);
			await context.SaveChangesAsync();
		}
	}
}
