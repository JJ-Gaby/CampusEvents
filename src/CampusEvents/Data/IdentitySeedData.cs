using CampusEvents.Models;
using CampusEvents.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace CampusEvents.Data;

public static class IdentitySeedData
{
	private const string ExistingOrganizerId = "seed-organizer-001";
	private const string ClassroomAdministratorId = "seed-administrator-001";
	private const string OrganizerDisplayName = "Campus Events Office";
	private const string AdministratorDisplayName = "CEMS Administrator";

	public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
	{
		var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
		var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

		var administratorEmail = configuration["SeedAccounts:Administrator:Email"];
		var administratorPassword = configuration["SeedAccounts:Administrator:Password"];
		var organizerEmail = configuration["SeedAccounts:Organizer:Email"];
		var organizerPassword = configuration["SeedAccounts:Organizer:Password"];

		if (string.IsNullOrWhiteSpace(administratorEmail) || string.IsNullOrWhiteSpace(administratorPassword)
			|| string.IsNullOrWhiteSpace(organizerEmail) || string.IsNullOrWhiteSpace(organizerPassword))
		{
			throw new InvalidOperationException("Configure both classroom accounts in Development user secrets.");
		}

		var normalizedAdministratorEmail = userManager.NormalizeEmail(administratorEmail);
		var normalizedOrganizerEmail = userManager.NormalizeEmail(organizerEmail);
		var normalizedAdministratorName = userManager.NormalizeName(administratorEmail);
		var normalizedOrganizerName = userManager.NormalizeName(organizerEmail);
		if (string.Equals(normalizedAdministratorEmail, normalizedOrganizerEmail, StringComparison.Ordinal))
		{
			throw new InvalidOperationException("Administrator and Organizer seed emails must be different.");
		}

		var organizerById = await userManager.FindByIdAsync(ExistingOrganizerId);
		var administratorById = await userManager.FindByIdAsync(ClassroomAdministratorId);

		// Reconcile legacy Development records with uninitialized normalized fields.
		// Routine account lookup uses UserManager and indexed Identity fields.
		var context = services.GetRequiredService<ApplicationDbContext>();
		var accountIdentifiers = await context.Users.AsNoTracking()
			.Select(user => new { user.Id, user.Email, user.UserName }).ToListAsync();

		foreach (var account in accountIdentifiers)
		{
			var email = userManager.NormalizeEmail(account.Email ?? string.Empty);
			var userName = userManager.NormalizeName(account.UserName ?? string.Empty);
			var matchesAdministrator = email == normalizedAdministratorEmail || userName == normalizedAdministratorName;
			var matchesOrganizer = email == normalizedOrganizerEmail || userName == normalizedOrganizerName;

			if (matchesAdministrator && account.Id != ClassroomAdministratorId)
			{
				throw new InvalidOperationException("Administrator identity conflicts with an existing account.");
			}

			if (matchesOrganizer && account.Id != ExistingOrganizerId)
			{
				throw new InvalidOperationException("Organizer identity conflicts with an existing account.");
			}
		}

		if (administratorById is not null &&
			!string.Equals(userManager.NormalizeEmail(administratorById.Email ?? ""), normalizedAdministratorEmail,
				StringComparison.Ordinal))
		{
			throw new InvalidOperationException("Classroom Administrator email differs from the configured address.");
		}

		var existingOrganizerEmail = organizerById?.Email;
		if (!string.IsNullOrWhiteSpace(existingOrganizerEmail))
		{
			var normalizedExistingEmail = userManager.NormalizeEmail(existingOrganizerEmail);
			if (accountIdentifiers.Any(account => account.Id != ExistingOrganizerId
				&& userManager.NormalizeEmail(account.Email ?? string.Empty) == normalizedExistingEmail))
			{
				throw new InvalidOperationException("Existing Organizer email belongs to another account.");
			}
		}

		await EnsureRoleAsync(roleManager, RoleNames.Administrator);
		await EnsureRoleAsync(roleManager, RoleNames.Organizer);
		await EnsureAdministratorAsync(userManager, administratorEmail, administratorPassword);
		await EnsureOrganizerAsync(userManager, organizerEmail, organizerPassword);
	}

	private static void EnsureSucceeded(IdentityResult result, string operation)
	{
		if (result.Succeeded)
		{
			return;
		}

		var descriptions = string.Join("; ", result.Errors.Select(error => error.Description));

		throw new InvalidOperationException($"Identity seeding failed while {operation}: {descriptions}");
	}

	private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
	{
		if (await roleManager.RoleExistsAsync(roleName))
		{
			return;
		}

		var result = await roleManager.CreateAsync(new IdentityRole(roleName));

		EnsureSucceeded(result, $"creating role '{roleName}'");
	}

	private static async Task EnsureAdministratorAsync(
		UserManager<ApplicationUser> userManager, string email, string password)
	{
		var user = await userManager.FindByIdAsync(ClassroomAdministratorId);

		if (user is null)
		{
			user = new ApplicationUser
			{
				Id = ClassroomAdministratorId,
				UserName = email,
				Email = email,
				DisplayName = AdministratorDisplayName
			};

			var createResult = await userManager.CreateAsync(user, password);

			EnsureSucceeded(createResult, "creating the classroom Administrator account");
		}
		else
		{
			if (string.IsNullOrEmpty(user.SecurityStamp))
			{
				user.SecurityStamp = Guid.NewGuid().ToString();
			}

			user.UserName = email;
			user.Email = email;
			user.DisplayName = AdministratorDisplayName;

			var updateResult = await userManager.UpdateAsync(user);

			EnsureSucceeded(updateResult, "updating the classroom Administrator account");

			if (!await userManager.HasPasswordAsync(user))
			{
				var passwordResult = await userManager.AddPasswordAsync(user, password);

				EnsureSucceeded(passwordResult, "adding the classroom Administrator password");
			}
		}

		if (!await userManager.IsInRoleAsync(user, RoleNames.Administrator))
		{
			var roleResult = await userManager.AddToRoleAsync(user, RoleNames.Administrator);

			EnsureSucceeded(roleResult, "assigning the Administrator role");
		}
	}

	private static async Task EnsureOrganizerAsync(UserManager<ApplicationUser> userManager, string email, string password)
	{
		var user = await userManager.FindByIdAsync(ExistingOrganizerId);

		if (user is null)
		{
			user = new ApplicationUser
			{
				Id = ExistingOrganizerId,
				UserName = email,
				Email = email,
				DisplayName = OrganizerDisplayName
			};

			var createResult = await userManager.CreateAsync(user, password);

			EnsureSucceeded(createResult, "creating the classroom Organizer account");
		}
		else
		{
			if (string.IsNullOrEmpty(user.SecurityStamp))
			{
				user.SecurityStamp = Guid.NewGuid().ToString();
			}

			user.UserName = email;
			user.Email = email;
			user.DisplayName = OrganizerDisplayName;

			var updateResult = await userManager.UpdateAsync(user);

			EnsureSucceeded(updateResult, "updating the existing seed Organizer identity");

			if (!await userManager.HasPasswordAsync(user))
			{
				var passwordResult = await userManager.AddPasswordAsync(user, password);

				EnsureSucceeded(passwordResult, "adding the classroom Organizer password");
			}
		}

		if (!await userManager.IsInRoleAsync(user, RoleNames.Organizer))
		{
			var roleResult = await userManager.AddToRoleAsync(user, RoleNames.Organizer);

			EnsureSucceeded(roleResult, "assigning the Organizer role");
		}
	}
}
