using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Data; 

public class HypertableColumnAttribute : Attribute { }

public static class TimeScaleExtensions {
    public static void ApplyHyperTables(this DataContext context) {
        // Add timescale extension to the database if it doesn't exist
        context.Database.ExecuteSqlRaw("CREATE EXTENSION IF NOT EXISTS timescaledb CASCADE;");

        var entityTypes = context.Model.GetEntityTypes();
        foreach (var entityType in entityTypes) {
            foreach (var property in entityType.GetProperties()) {
                if (property.PropertyInfo?.GetCustomAttribute(typeof(HypertableColumnAttribute)) is null) continue;

                var tableName = entityType.GetTableName();
                var columnName = property.GetColumnName();
                context.Database.ExecuteSqlRaw($"SELECT create_hypertable('\"{tableName}\"', '{columnName}');");
            }
        }
    }
}