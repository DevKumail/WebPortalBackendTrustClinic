using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Coherent.Infrastructure.Data;

/// <summary>
/// Factory for creating database connections for both Primary and Secondary databases
/// </summary>
public class DatabaseConnectionFactory
{
    private readonly IConfiguration _configuration;

    public DatabaseConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IDbConnection CreatePrimaryConnection()
    {
        var connectionString = _configuration.GetConnectionString("PrimaryDatabase");
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Primary database connection string is not configured");
        
        return new SqlConnection(connectionString);
    }

    public IDbConnection CreateSecondaryConnection()
    {
        var connectionString = _configuration.GetConnectionString("SecondaryDatabase");
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Secondary database connection string is not configured");
        
        return new SqlConnection(connectionString);
    }

    public IDbConnection CreateConnection(string databaseSource)
    {
        return databaseSource.ToLower() switch
        {
            "primary" => CreatePrimaryConnection(),
            "secondary" => CreateSecondaryConnection(),
            _ => throw new ArgumentException($"Invalid database source: {databaseSource}")
        };
    }
}
