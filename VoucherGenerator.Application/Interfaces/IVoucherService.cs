using VoucherGenerator.Domain.Entities;

namespace VoucherGenerator.Application.Interfaces;

public interface IVoucherService
{
    List<string> GenerateVoucherNumbers(int count, int digits);
    Task SaveVouchersAsync(List<string> numbers, string networkName, string voucherType, string validTill);
    Task<List<Voucher>> GetHistoryAsync();
    Task<List<Voucher>> SearchHistoryAsync(string query);
}
