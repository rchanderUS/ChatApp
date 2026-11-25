using Microsoft.EntityFrameworkCore;

namespace ChatApp.Data;

public sealed class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(user => user.NormalizedName)
            .IsUnique();

        modelBuilder.Entity<Message>()
            .HasOne(message => message.User)
            .WithMany()
            .HasForeignKey(message => message.UserId);
    }
}
