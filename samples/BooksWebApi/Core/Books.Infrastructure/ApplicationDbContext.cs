using System.Reflection;
using Books.Domain;
using Books.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using MitMediator.AppAuthorize.Domain;

namespace Books.Infrastructure;

/// <summary>
/// Application Db context.
/// </summary>
public class ApplicationDbContext : DbContext
{
    /// <summary>
    /// Authors.
    /// </summary>
    public DbSet<Author> Authors { get; set; }

    /// <summary>
    /// Genres.
    /// </summary>
    public DbSet<Genre> Genres { get; set; }
    
    /// <summary>
    /// Books.
    /// </summary>
    public DbSet<Book> Books { get; set; }
    
    /// <summary>
    /// User roles.
    /// </summary>
    public DbSet<UserRole> UserRoles { get; set; }
    
    /// <summary>
    /// Tenants.
    /// </summary>
    public DbSet<Tenant> Tenants { get; set; }
    
    /// <summary>
    /// Users.
    /// </summary>
    public DbSet<User> Users { get; set; }
    
    /// <summary>
    /// Refresh tokens.
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    
    /// <summary>
    /// Initializes a new instance of <see cref="ApplicationDbContext"/>.
    /// </summary>
    /// <param name="options"><see cref="DbContextOptions"/></param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {

    }
    
    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}