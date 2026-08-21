using Forum.Domain.Comments;
using Forum.Domain.Posts;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Forum.Infrastructure.Persistence.Configurations;

public sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.HasKey(comment => comment.Id);

        builder.Property(comment => comment.Body).HasMaxLength(Comment.BodyMaxLength).IsRequired();

        builder.HasOne(comment => comment.Author)
            .WithMany()
            .HasForeignKey(comment => comment.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        // Replies are always read for one discussion, in order.
        builder.HasIndex(comment => new { comment.PostId, comment.CreatedAt });
    }
}
