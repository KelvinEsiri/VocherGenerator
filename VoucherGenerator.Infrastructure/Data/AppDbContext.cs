using Microsoft.EntityFrameworkCore;
using VoucherGenerator.Domain.Entities;

namespace VoucherGenerator.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Voucher> Vouchers => Set<Voucher>();
}
