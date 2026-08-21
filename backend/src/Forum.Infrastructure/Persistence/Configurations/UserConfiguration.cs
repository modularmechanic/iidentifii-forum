using Forum.Domain.Users;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Forum.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(user => user.Id);

        // citext compares case-insensitively in the database, so the unique index also
        // rejects "Alice" once "alice" exists, and lookups still use it.
        builder.Property(user => user.Username)
            .HasColumnType("citext")
            .HasMaxLength(User.UsernameMaxLength)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasColumnType("citext")
            .HasMaxLength(User.EmailMaxLength)
            .IsRequired();

        builder.Property(user => user.PasswordHash).IsRequired();
        builder.Property(user => user.Role).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.HasIndex(user => user.Username).IsUnique();
        builder.HasIndex(user => user.Email).IsUnique();
    }
}
