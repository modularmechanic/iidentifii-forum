using Forum.Domain.Posts;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Forum.Infrastructure.Persistence.Configurations;

public sealed class PostTagConfiguration : IEntityTypeConfiguration<PostTag>
{
    public void Configure(EntityTypeBuilder<PostTag> builder)
    {
        builder.HasKey(tag => new { tag.PostId, tag.Tag });

        builder.Property(tag => tag.Tag).HasConversion<string>().HasMaxLength(64).IsRequired();

        builder.HasOne(tag => tag.TaggedByUser)
            .WithMany()
            .HasForeignKey(tag => tag.TaggedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Filtering the list by flag reads this.
        builder.HasIndex(tag => tag.Tag);
    }
}
