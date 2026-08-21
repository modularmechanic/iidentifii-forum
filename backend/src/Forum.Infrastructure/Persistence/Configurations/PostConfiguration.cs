using Forum.Domain.Comments;
using Forum.Domain.Posts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forum.Infrastructure.Persistence.Configurations;

public sealed class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.HasKey(post => post.Id);

        builder.Property(post => post.Title).HasMaxLength(Post.TitleMaxLength).IsRequired();
        builder.Property(post => post.Body).HasMaxLength(Post.BodyMaxLength).IsRequired();

        builder.HasOne(post => post.Author)
            .WithMany()
            .HasForeignKey(post => post.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(post => post.Likes)
            .WithOne()
            .HasForeignKey(like => like.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(post => post.Tags)
            .WithOne()
            .HasForeignKey(tag => tag.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(post => post.Comments)
            .WithOne()
            .HasForeignKey(comment => comment.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(post => post.Likes).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(post => post.Tags).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(post => post.Comments).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Listing orders by recency and filters by author, so both carry an index.
        builder.HasIndex(post => post.CreatedAt);
        builder.HasIndex(post => post.AuthorId);
    }
}
