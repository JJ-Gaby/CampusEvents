using CampusEvents.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Tests;

/// <summary>
/// Each test owns a GUID-named SQL Server database, created from real migrations.
/// LocalDB is the default; an explicit environment override can target an authorized test server.
/// </summary>
public sealed class IsolatedSqlServerDatabase : IAsyncDisposable
{
	private const string Prefix = "CemsL02Test_";
	private const string DefaultMasterConnection =
		@"Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=True;TrustServerCertificate=True";
	private readonly string _masterConnection;
	private readonly string _databaseName;
	private bool _migrationAttempted;

	private IsolatedSqlServerDatabase(string masterConnection, string databaseName, string eventConnection)
	{
		_masterConnection = masterConnection;
		_databaseName = databaseName;
		ConnectionString = eventConnection;
	}

	public string ConnectionString { get; }

	public ApplicationDbContext CreateContext()
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(ConnectionString).Options;
		return new ApplicationDbContext(options);
	}

	public static async Task<IsolatedSqlServerDatabase> CreateAsync()
	{
		string raw = Environment.GetEnvironmentVariable("CEMS_TEST_SQLSERVER") ?? DefaultMasterConnection;
		var master = new SqlConnectionStringBuilder(raw);

		// Operate from master, never from an existing application database or an attached file.
		if (!string.Equals(master.InitialCatalog, "master", StringComparison.OrdinalIgnoreCase) ||
			!string.IsNullOrEmpty(master.AttachDBFilename))
		{
			throw new InvalidOperationException(
				"CEMS_TEST_SQLSERVER must target the master catalog without AttachDBFilename. " +
				"By default CEMS tests use (localdb)\\MSSQLLocalDB; no configuration is required.");
		}

		string name = Prefix + Guid.NewGuid().ToString("N");
		var target = new SqlConnectionStringBuilder(master.ConnectionString)
		{
			InitialCatalog = name
		};
		var database = new IsolatedSqlServerDatabase(master.ConnectionString, name, target.ConnectionString);

		try
		{
			database._migrationAttempted = true;
			await using var context = database.CreateContext();
			await context.Database.MigrateAsync();
			return database;
		}
		catch
		{
			await database.DisposeAsync();
			throw;
		}
	}

	public async ValueTask DisposeAsync()
	{
		if (!_migrationAttempted)
		{
			return;
		}

		// Release pooled connections before dropping this fixture-owned database.
		await using (var targetConnection = new SqlConnection(ConnectionString))
		{
			SqlConnection.ClearPool(targetConnection);
		}
		await using var connection = new SqlConnection(_masterConnection);
		await connection.OpenAsync();
		await using var exists = new SqlCommand("SELECT DB_ID(@name)", connection);
		exists.Parameters.AddWithValue("@name", _databaseName);
		var databaseId = await exists.ExecuteScalarAsync();
		if (databaseId is not null && databaseId is not DBNull)
		{
			// The identifier is fixture-generated from a GUID and never accepted from a caller.
			string identifier = "[" + _databaseName + "]";
			string sql = "ALTER DATABASE " + identifier + " SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
				"DROP DATABASE " + identifier + ";";
			await using var drop = new SqlCommand(sql, connection);
			await drop.ExecuteNonQueryAsync();
		}
		_migrationAttempted = false;
	}
}
