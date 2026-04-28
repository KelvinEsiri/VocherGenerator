using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VoucherGenerator.Application.Interfaces;
using VoucherGenerator.Application.Services;
using VoucherGenerator.Domain.Interfaces;
using VoucherGenerator.Infrastructure.Data;
using VoucherGenerator.Infrastructure.Repositories;
using VoucherGenerator.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Database (SQLite)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=vouchers.db"));

// Application services
builder.Services.AddScoped<IVoucherRepository, VoucherRepository>();
builder.Services.AddScoped<IVoucherService, VoucherService>();

var app = builder.Build();

// Ensure the SQLite database is created and the new column exists
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    var connection = db.Database.GetDbConnection();
    connection.Open();
    using var pragmaCmd = connection.CreateCommand();
    pragmaCmd.CommandText = "PRAGMA table_info(\"Vouchers\")";

    var hasValidTill = false;
    using (var reader = pragmaCmd.ExecuteReader())
    {
        while (reader.Read())
        {
            if (reader.GetString(1).Equals("ValidTill", StringComparison.OrdinalIgnoreCase))
            {
                hasValidTill = true;
                break;
            }
        }
    }

    if (!hasValidTill)
    {
        using var alterCmd = connection.CreateCommand();
        alterCmd.CommandText = "ALTER TABLE \"Vouchers\" ADD COLUMN \"ValidTill\" TEXT NOT NULL DEFAULT ''";
        alterCmd.ExecuteNonQuery();
    }
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
