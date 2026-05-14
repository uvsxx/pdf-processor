using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PdfProcessor.Domain;

namespace PdfProcessor.Infrastructure.Persistence.Configurations;

public sealed class DocumentContentConfiguration : IEntityTypeConfiguration<DocumentContent>
{
    public void Configure(EntityTypeBuilder<DocumentContent> b)
    {
        b.ToTable("document_contents");
        b.HasKey(x => x.DocumentId);
        b.Property(x => x.Text).IsRequired().HasColumnType("text");
    }
}
