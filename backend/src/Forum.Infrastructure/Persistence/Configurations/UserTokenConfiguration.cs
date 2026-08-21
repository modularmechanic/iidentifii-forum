using Forum.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forum.Infrastructure.Persistence.Configurations;

public sealed class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
    public void Configure(EntityTypeBuilder<UserToken> builder)
    {
        builder.HasKey(token => token.Id);

        builder.Property(token => token.Purpose).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(token => token.SecretHash).HasMaxLength(128).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Looking up the newest usable token for one purpose is the only read pattern.
        builder.HasIndex(token => new { token.UserId, token.Purpose });
    }
}
