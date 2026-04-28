using Microsoft.EntityFrameworkCore;
using VoucherGenerator.Domain.Entities;
using VoucherGenerator.Domain.Interfaces;
using VoucherGenerator.Infrastructure.Data;

namespace VoucherGenerator.Infrastructure.Repositories;

public class VoucherRepository : IVoucherRepository
{
    private readonly AppDbContext _db;

    public VoucherRepository(AppDbContext db) => _db = db;

    public async Task AddRangeAsync(IEnumerable<Voucher> vouchers)
    {
        await _db.Vouchers.AddRangeAsync(vouchers);
        await _db.SaveChangesAsync();
    }

    public Task<List<Voucher>> GetAllAsync() =>
        _db.Vouchers.OrderByDescending(v => v.GeneratedAt).ToListAsync();

    public Task<List<Voucher>> SearchAsync(string query)
    {
        var q = query.ToLower();
        return _db.Vouchers
            .Where(v => v.VoucherNumber.ToLower().Contains(q)
                     || v.NetworkName.ToLower().Contains(q)
                     || v.VoucherType.ToLower().Contains(q)
                     || v.ValidTill.ToLower().Contains(q))
            .OrderByDescending(v => v.GeneratedAt)
            .ToListAsync();
    }
}
