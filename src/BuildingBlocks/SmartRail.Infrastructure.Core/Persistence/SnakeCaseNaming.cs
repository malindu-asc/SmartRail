using System.Text;
using Microsoft.EntityFrameworkCore;

namespace SmartRail.Infrastructure.Core.Persistence;

public static class SnakeCaseNaming
{
    // Gives the entities whose CLR type lives under a namespace prefix snake_case column names
    // (CreatedAtUtc becomes created_at_utc). Columns already named explicitly are left alone, and so are
    // entities from other namespaces such as the outbox table owned by the base context.
    public static ModelBuilder UseSnakeCaseColumns(this ModelBuilder modelBuilder, string namespacePrefix)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            if (entity.ClrType.Namespace?.StartsWith(namespacePrefix, StringComparison.Ordinal) != true)
                continue;

            foreach (var property in entity.GetProperties())
            {
                if (property.GetColumnName() is { } name && name == property.Name)
                    property.SetColumnName(ToSnakeCase(name));
            }
        }

        return modelBuilder;
    }

    public static string ToSnakeCase(string name)
    {
        var builder = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0 && (char.IsLower(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1]))))
                builder.Append('_');

            builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }
}
