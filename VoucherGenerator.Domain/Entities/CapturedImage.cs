namespace VoucherGenerator.Domain.Entities;

public class CapturedImage
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "image/png";
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public byte[] ThumbnailData { get; set; } = Array.Empty<byte>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
