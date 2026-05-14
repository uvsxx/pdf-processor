using System.ComponentModel.DataAnnotations;

namespace PdfProcessor.Infrastructure.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    [Required, MinLength(1)]
    public string RootPath { get; set; } = default!;
}
