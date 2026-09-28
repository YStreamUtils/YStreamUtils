using Microsoft.EntityFrameworkCore;

namespace YStreamUtils.Core.Data;

public class DbContextFactoryBridge<TContext>(IDbContextFactory<TContext> concreteFactory) 
    : IDbContextFactory<AppDbContext> where TContext : AppDbContext
{
    public AppDbContext CreateDbContext() => concreteFactory.CreateDbContext();
}