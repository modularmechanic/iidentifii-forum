using Forum.Domain.Users;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

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

        // Looking up the newest token for one purpose is the read pattern behind redemption,
        // the cooldown check and retirement alike.
        builder.HasIndex(token => new { token.UserId, token.Purpose });

        // At most one token per purpose may be outstanding. Issuing retires the previous one first,
        // so this only ever refuses a second request racing the first, which is the point of it.
        builder.HasIndex(token => new { token.UserId, token.Purpose }, "IX_UserTokens_Outstanding")
            .IsUnique()
            .HasFilter("\"ConsumedAt\" IS NULL");
    }
}
