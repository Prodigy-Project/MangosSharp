//
// Copyright (C) 2013-2025 getMaNGOS <https://www.getmangos.eu>
//
// This program is free software. You can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation. either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY. Without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//

using Mangos.Configuration;
using Mangos.Logging;
using Mangos.MySql.Connections;
using MySqlConnector;

namespace Mangos.MySql;

internal sealed class ConnectionFactory
{
    private readonly MangosConfiguration mangosConfiguration;
    private readonly IMangosLogger logger;

    public ConnectionFactory(MangosConfiguration mangosConfiguration, IMangosLogger logger)
    {
        this.mangosConfiguration = mangosConfiguration;
        this.logger = logger;
    }

    public AccountConnection ConnectToAccountDataBase()
    {
        var connectionString = mangosConfiguration.AccountDataBaseConnectionString;
        logger.Debug($"Opening account database connection");

        try
        {
            var mySqlConnection = new MySqlConnection(connectionString);
            mySqlConnection.Open();
            
            // Test the connection with a simple query
            TestConnection(mySqlConnection);
            
            logger.Information("Account database connection established and tested successfully");
            return new AccountConnection(mySqlConnection, logger);
        }
        catch (MySqlException ex)
        {
            logger.Error(ex, "Failed to connect to account database");
            throw;
        }
    }

    public CharacterConnection ConnectToCharacterDataBase()
    {
        var connectionString = mangosConfiguration.CharacterDataBaseConnectionString;
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Character database connection string is not configured");
        }
        
        logger.Debug($"Opening character database connection");

        try
        {
            var mySqlConnection = new MySqlConnection(connectionString);
            mySqlConnection.Open();
            
            // Test the connection with a simple query
            TestConnection(mySqlConnection);
            
            logger.Information("Character database connection established and tested successfully");
            return new CharacterConnection(mySqlConnection, logger);
        }
        catch (MySqlException ex)
        {
            logger.Error(ex, "Failed to connect to character database");
            throw;
        }
    }

    public WorldConnection ConnectToWorldDataBase()
    {
        var connectionString = mangosConfiguration.WorldDataBaseConnectionStrings;
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("World database connection string is not configured");
        }
        
        logger.Debug($"Opening world database connection");

        try
        {
            var mySqlConnection = new MySqlConnection(connectionString);
            mySqlConnection.Open();
            
            // Test the connection with a simple query
            TestConnection(mySqlConnection);
            
            logger.Information("World database connection established and tested successfully");
            return new WorldConnection(mySqlConnection, logger);
        }
        catch (MySqlException ex)
        {
            logger.Error(ex, "Failed to connect to world database");
            throw;
        }
    }

    private void TestConnection(MySqlConnection connection)
    {
        try
        {
            using var command = new MySqlCommand("SELECT 1", connection);
            var result = command.ExecuteScalar();
            if (result == null || Convert.ToInt32(result) != 1)
            {
                throw new InvalidOperationException("Connection test failed: unexpected result from test query");
            }

            logger.Debug("Database connection test passed");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Database connection test failed");
            throw;
        }
    }
}
