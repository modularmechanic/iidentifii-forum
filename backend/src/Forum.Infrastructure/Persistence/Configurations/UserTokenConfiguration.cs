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

        // The read pattern behind the cooldown check and retirement alike.
        builder.HasIndex(token => new { token.UserId, token.Purpose });

        // Redemption has only the secret the emailed link carries, so that way in is indexed too.
        builder.HasIndex(token => token.SecretHash);

        // At most one token per purpose may be outstanding. Issuing retires the previous one first,
        // so this only ever refuses a second request racing the first, which is the point of it.
        builder.HasIndex(token => new { token.UserId, token.Purpose }, "IX_UserTokens_Outstanding")
            .IsUnique()
            .HasFilter("\"ConsumedAt\" IS NULL");
    }
}
