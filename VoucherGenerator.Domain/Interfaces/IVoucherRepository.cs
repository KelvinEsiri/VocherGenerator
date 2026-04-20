using VoucherGenerator.Domain.Entities;

namespace VoucherGenerator.Domain.Interfaces;

public interface IVoucherRepository
{
    Task AddRangeAsync(IEnumerable<Voucher> vouchers);
    Task<List<Voucher>> GetAllAsync();
    Task<List<Voucher>> SearchAsync(string query);
}
