using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PdfProcessor.Domain;

namespace PdfProcessor.Infrastructure.Persistence.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> b)
    {
        b.ToTable("documents");
        b.HasKey(x => x.Id);

        b.Property(x => x.FileName).IsRequired().HasMaxLength(512);
        b.Property(x => x.StorageKey).IsRequired().HasMaxLength(1024);
        b.Property(x => x.SizeBytes).IsRequired();
        b.Property(x => x.PageCount);
        b.Property(x => x.Status).HasConversion<short>().IsRequired();
        b.Property(x => x.ErrorMessage).HasMaxLength(2000);
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt).IsRequired();
        b.Property(x => x.ProcessedAt);

        b.HasIndex(x => new { x.Status, x.CreatedAt });

        b.HasOne(x => x.Content)
            .WithOne()
            .HasForeignKey<DocumentContent>(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
