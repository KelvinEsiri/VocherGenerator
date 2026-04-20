namespace VoucherGenerator.Domain.Entities;

public class Voucher
{
    public int Id { get; set; }
    public string VoucherNumber { get; set; } = string.Empty;
    public string NetworkName { get; set; } = string.Empty;
    public string VoucherType { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
