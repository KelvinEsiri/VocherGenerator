using System.Text;
using VoucherGenerator.Application.Interfaces;
using VoucherGenerator.Domain.Entities;
using VoucherGenerator.Domain.Interfaces;

namespace VoucherGenerator.Application.Services;

public class VoucherService : IVoucherService
{
    private readonly IVoucherRepository _repo;
    private readonly Random _rng = new();

    public VoucherService(IVoucherRepository repo) => _repo = repo;

    public List<string> GenerateVoucherNumbers(int count, int digits)
    {
        // All digit combinations allowed (including leading zeros): 10^digits unique values
        // Only feasible to overflow when digits <= 3 given our max count of 1000
        if (digits <= 3)
        {
            long maxPossible = 1;
            for (int p = 0; p < digits; p++) maxPossible *= 10;
            if (count > maxPossible)
                throw new InvalidOperationException(
                    $"Cannot generate {count} unique {digits}-digit vouchers: " +
                    $"only {maxPossible} unique values exist.");
        }

        var seen = new HashSet<string>(count);
        while (seen.Count < count)
        {
            var sb = new StringBuilder(digits);
            for (int d = 0; d < digits; d++)
                sb.Append((char)('0' + _rng.Next(10)));
            seen.Add(sb.ToString());
        }
        return new List<string>(seen);
    }

    public async Task SaveVouchersAsync(List<string> numbers, string networkName, string voucherType)
    {
        var vouchers = numbers.Select(n => new Voucher
        {
            VoucherNumber = n,
            NetworkName = networkName,
            VoucherType = voucherType,
            GeneratedAt = DateTime.UtcNow
        });
        await _repo.AddRangeAsync(vouchers);
    }

    public Task<List<Voucher>> GetHistoryAsync() => _repo.GetAllAsync();

    public Task<List<Voucher>> SearchHistoryAsync(string query) => _repo.SearchAsync(query);
}
