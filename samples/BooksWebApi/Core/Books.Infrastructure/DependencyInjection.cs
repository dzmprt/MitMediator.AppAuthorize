using Books.Application.Abstractions;
using Books.Application.Abstractions.Infrastructure;
using Books.Application.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MitMediator.AppAuthorize;

namespace Books.Infrastructure;

/// <summary>
/// Dependency injection.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Add application services.
    /// </summary>
    /// <param name="services"><see cref="IServiceProvider"/>.</param>
    /// <returns><see cref="IServiceProvider"/></returns>
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryDatabase>();
        return services.AddDbContext<DbContext, ApplicationDbContext>((provider, options) =>
            {
                options.UseSqlite(provider.GetRequiredService<InMemoryDatabase>().ConnectionString);
            })
            .AddTransient(typeof(IBaseProvider<>), typeof(BaseProvider<>))
            .AddTransient(typeof(IBaseRepository<>), typeof(BaseRepository<>))
            .AddTransient<IRefreshTokenRepository, RefreshTokenRepository>();
    }
    
    public static IServiceCollection AddPasswordHasher(this IServiceCollection services)
    {
        return services.AddTransient<IPasswordHasher, PasswordHasher>();
    }

    private sealed class InMemoryDatabase : IDisposable
    {
        private readonly SqliteConnection _keepAlive;

        public InMemoryDatabase()
        {
            ConnectionString = new SqliteConnectionStringBuilder
            {
                DataSource = $"books-{Guid.NewGuid():N}",
                Mode = SqliteOpenMode.Memory,
                Cache = SqliteCacheMode.Shared,
                Pooling = false
            }.ToString();

            // Keep the database alive without sharing a connection between contexts.
            _keepAlive = new SqliteConnection(ConnectionString);
            _keepAlive.Open();
        }

        public string ConnectionString { get; }

        public void Dispose() => _keepAlive.Dispose();
    }
}