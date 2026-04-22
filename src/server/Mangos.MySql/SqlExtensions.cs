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

using System;
using System.Data;

namespace Mangos.MySql;

/// <summary>
/// Extension methods for converting DataRow values to typed values with proper null handling.
/// </summary>
public static class SqlExtensions
{
    /// <summary>
    /// Converts a DataRow column value to the specified type.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="row">The DataRow to read from.</param>
    /// <param name="column">The column index.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when row is null or column value is null.</exception>
    public static T As<T>(this DataRow row, int column)
    {
        return row switch
        {
            null => throw new ArgumentNullException(nameof(row), "DataRow cannot be null."), _ => row[column] is null or DBNull
                ? throw new InvalidOperationException($"Column at index {column} contains null value.") : (T)Convert.ChangeType(row[column], typeof(T))
        };
    }

    /// <summary>
    /// Converts a DataRow column value to the specified type.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="row">The DataRow to read from.</param>
    /// <param name="field">The column name.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when row is null or field value is null.</exception>
    public static T As<T>(this DataRow row, string field)
    {
        if (row is null)
        {
            throw new ArgumentNullException(nameof(row), "DataRow cannot be null.");
        }

        if (!row.Table.Columns.Contains(field))
        {
            throw new ArgumentException($"Column '{field}' does not exist in the DataRow.", nameof(field));
        }

        return row[field] is null or DBNull
            ? throw new InvalidOperationException($"Column '{field}' contains null value.") : (T)Convert.ChangeType(row[field], typeof(T));
    }

    /// <summary>
    /// Converts a DataRow column value from one type to another.
    /// </summary>
    /// <typeparam name="T1">The intermediate type.</typeparam>
    /// <typeparam name="T2">The target type.</typeparam>
    /// <param name="row">The DataRow to read from.</param>
    /// <param name="field">The column name.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when row is null or field value is null.</exception>
    public static T2 As<T1, T2>(this DataRow row, string field)
    {
        if (row is null)
        {
            throw new ArgumentNullException(nameof(row), "DataRow cannot be null.");
        }

        if (!row.Table.Columns.Contains(field))
        {
            throw new ArgumentException($"Column '{field}' does not exist in the DataRow.", nameof(field));
        }

        if (row[field] is null or DBNull)
        {
            throw new InvalidOperationException($"Column '{field}' contains null value.");
        }

        var t1 = (T1)Convert.ChangeType(row[field], typeof(T1));
        return (T2)Convert.ChangeType(t1, typeof(T2));
    }
}
