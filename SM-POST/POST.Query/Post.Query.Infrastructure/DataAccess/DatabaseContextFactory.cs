using System;
using Microsoft.EntityFrameworkCore;

namespace Post.Query.Infrastructure.DataAccess;

public class DatabaseContextFactory(Action<DbContextOptionsBuilder> ConfigureDbContext)
{
    public DatabaseContext CreateDbContext()
    {
        DbContextOptionsBuilder<DatabaseContext> optionsBuilder = new();
        ConfigureDbContext(optionsBuilder);

        return new DatabaseContext(optionsBuilder.Options);
    }

}
