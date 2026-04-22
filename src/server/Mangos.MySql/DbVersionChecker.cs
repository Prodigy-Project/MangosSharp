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

using Mangos.Common.Enums.Global;
using Mangos.Common.Globals;
using Mangos.Logging;
using System;
using System.Data;
using System.Threading.Tasks;

namespace Mangos.MySql;

/// <summary>
/// Checks if the database schema version matches the required core version.
/// Provides both synchronous and asynchronous version checking.
/// </summary>
public class DbVersionChecker
{
    private readonly MangosGlobalConstants _globalConstants;
    private readonly IMangosLogger _logger;

    /// <summary>
    /// Represents database version information.
    /// </summary>
    private record DbVersionInfo(int Version, int Structure, int Content);

    public DbVersionChecker(IMangosLogger logger, MangosGlobalConstants mangosGlobalConstants)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _globalConstants = mangosGlobalConstants ?? throw new ArgumentNullException(nameof(mangosGlobalConstants));
    }

    /// <summary>
    /// Checks if the database version matches the required core version.
    /// </summary>
    /// <param name="database">The database to check.</param>
    /// <param name="serverDb">The type of database being checked.</param>
    /// <returns>True if version is compatible or content mismatch (warnings), false if version mismatch (fatal).</returns>
    public bool CheckRequiredDbVersion(SQL database, ServerDb serverDb)
    {
        var result = new DataTable();
        var queryCode = database.Query("SELECT `version`,`structure`,`content` FROM db_version ORDER BY version DESC, structure DESC, content DESC LIMIT 0,1", ref result);

        if (queryCode != (int)SQL.ReturnState.Success)
        {
            LogDatabaseCheckFailure(database.SQLDBName, "Failed to query database version");
            return false;
        }

        var expectedVersion = GetExpectedVersion(serverDb);
        if (expectedVersion is null)
        {
            return false;
        }

        if (result.Rows.Count == 0)
        {
            LogMissingVersionTable(database.SQLDBName, expectedVersion);
            return false;
        }

        var dbVersion = ExtractVersionInfo(result);
        return ValidateVersion(database.SQLDBName, dbVersion, expectedVersion);
    }

    /// <summary>
    /// Checks if the database version matches asynchronously.
    /// </summary>
    public async Task<bool> CheckRequiredDbVersionAsync(SQL database, ServerDb serverDb)
    {
        var result = new DataTable();
        var queryCode = await database.QueryAsync("SELECT `version`,`structure`,`content` FROM db_version ORDER BY version DESC, structure DESC, content DESC LIMIT 0,1", result);

        if (queryCode != (int)SQL.ReturnState.Success)
        {
            LogDatabaseCheckFailure(database.SQLDBName, "Failed to query database version");
            return false;
        }

        var expectedVersion = GetExpectedVersion(serverDb);
        if (expectedVersion is null)
        {
            return false;
        }

        if (result.Rows.Count == 0)
        {
            LogMissingVersionTable(database.SQLDBName, expectedVersion);
            return false;
        }

        var dbVersion = ExtractVersionInfo(result);
        return ValidateVersion(database.SQLDBName, dbVersion, expectedVersion);
    }

    /// <summary>
    /// Gets the expected version for the given server database type.
    /// </summary>
    private DbVersionInfo? GetExpectedVersion(ServerDb serverDb)
    {
        return serverDb switch
        {
            ServerDb.Realm => new DbVersionInfo(
                _globalConstants.RevisionDbRealmVersion,
                _globalConstants.RevisionDbRealmStructure,
                _globalConstants.RevisionDbRealmContent),

            ServerDb.Character => new DbVersionInfo(
                _globalConstants.RevisionDbCharactersVersion,
                _globalConstants.RevisionDbCharactersStructure,
                _globalConstants.RevisionDbCharactersContent),

            ServerDb.World => new DbVersionInfo(
                _globalConstants.RevisionDbMangosVersion,
                _globalConstants.RevisionDbMangosStructure,
                _globalConstants.RevisionDbMangosContent),

            _ => null
        };
    }

    /// <summary>
    /// Extracts version information from a query result row.
    /// </summary>
    private static DbVersionInfo ExtractVersionInfo(DataTable result)
    {
        var row = result.Rows[0];
        return new DbVersionInfo(row.As<int>("version"), row.As<int>("structure"), row.As<int>("content"));
    }

    /// <summary>
    /// Validates that the database version matches the expected version.
    /// </summary>
    private bool ValidateVersion(string dbName, DbVersionInfo actual, DbVersionInfo expected)
    {
        // Perfect match
        if (actual.Version == expected.Version &&  actual.Structure == expected.Structure && actual.Content == expected.Content)
        {
            _logger.Trace($"[{DateTime.Now:hh:mm:ss}] Db version matched");
            return true;
        }

        // Content mismatch but compatible (warning level)
        if (actual.Version == expected.Version && actual.Structure == expected.Structure && actual.Content != expected.Content)
        {
            _logger.Warning("--------------------------------------------------------------");
            _logger.Warning("-- WARNING: CONTENT VERSION MISMATCH                        --");
            _logger.Warning("--------------------------------------------------------------");
            _logger.Warning($"Your database '{dbName}' requires updating.");
            _logger.Warning($"You have: Rev{actual.Version}.{actual.Structure}.{actual.Content}, " +
                             $"however the core expects Rev{expected.Version}.{expected.Structure}.{expected.Content}");
            _logger.Warning("The server will run, but you may be missing some database fixes");
            return true;
        }

        // Version or structure mismatch (fatal)
        _logger.Error("--------------------------------------------------------------");
        _logger.Error("-- FATAL ERROR: DATABASE VERSION MISMATCH                   --");
        _logger.Error("--------------------------------------------------------------");
        _logger.Error($"Your database '{dbName}' requires updating.");
        _logger.Error($"You have: Rev{actual.Version}.{actual.Structure}.{actual.Content}, " +
                      $"but the core requires Rev{expected.Version}.{expected.Structure}.{expected.Content}");
        _logger.Error("The server is unable to run until the required updates are applied.");
        _logger.Error("--------------------------------------------------------------");
        _logger.Error($"Please apply all updates after Rev{expected.Version}.{expected.Structure}.{expected.Content}");
        _logger.Error("These updates are located in the sql/updates folder.");
        _logger.Error("--------------------------------------------------------------");
        return false;
    }

    /// <summary>
    /// Logs when the database version check fails.
    /// </summary>
    private void LogDatabaseCheckFailure(string dbName, string reason)
    {
        _logger.Error("--------------------------------------------------------------");
        _logger.Error("-- DATABASE VERSION CHECK FAILURE                           --");
        _logger.Error("--------------------------------------------------------------");
        _logger.Error($"Failed to check version for database '{dbName}': {reason}");
        _logger.Error("--------------------------------------------------------------");
    }

    /// <summary>
    /// Logs when the db_version table is missing.
    /// </summary>
    private void LogMissingVersionTable(string dbName, DbVersionInfo expected)
    {
        _logger.Error("--------------------------------------------------------------");
        _logger.Error("-- MISSING VERSION TABLE                                    --");
        _logger.Error("--------------------------------------------------------------");
        _logger.Error($"The table 'db_version' is missing in database '{dbName}'");
        _logger.Error("This database is likely not properly set up or is too old.");
        _logger.Error($"The core requires database schema Rev{expected.Version}.{expected.Structure}.{expected.Content}");
        _logger.Error("--------------------------------------------------------------");
    }
}
