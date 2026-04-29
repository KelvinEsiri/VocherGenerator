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

// Raise SignalR receive limit so camera frame data:URLs (can be several MB) don't crash the circuit.
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10 MB
});

// Database (SQLite)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=vouchers.db"));

// Application services
builder.Services.AddScoped<IVoucherRepository, VoucherRepository>();
builder.Services.AddScoped<IVoucherService, VoucherService>();
builder.Services.AddScoped<IImageCaptureRepository, ImageCaptureRepository>();
builder.Services.AddScoped<IImageCaptureService, ImageCaptureService>();

var app = builder.Build();

// Ensure SQLite schema is present for existing databases without migrations.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    var connection = db.Database.GetDbConnection();
    connection.Open();

    if (!HasColumn(connection, "Vouchers", "ValidTill"))
    {
        using var alterCmd = connection.CreateCommand();
        alterCmd.CommandText = "ALTER TABLE \"Vouchers\" ADD COLUMN \"ValidTill\" TEXT NOT NULL DEFAULT ''";
        alterCmd.ExecuteNonQuery();
    }

    if (!HasTable(connection, "CapturedImages"))
    {
        using var createCmd = connection.CreateCommand();
        createCmd.CommandText =
            "CREATE TABLE \"CapturedImages\" (" +
            "\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_CapturedImages\" PRIMARY KEY AUTOINCREMENT," +
            "\"FileName\" TEXT NOT NULL," +
            "\"ContentType\" TEXT NOT NULL," +
            "\"ImageData\" BLOB NOT NULL," +
            "\"ThumbnailData\" BLOB," +
            "\"CreatedAt\" TEXT NOT NULL" +
            ");";
        createCmd.ExecuteNonQuery();
    }

    if (!HasColumn(connection, "CapturedImages", "ThumbnailData"))
    {
        using var alterThumbCmd = connection.CreateCommand();
        alterThumbCmd.CommandText = "ALTER TABLE \"CapturedImages\" ADD COLUMN \"ThumbnailData\" BLOB";
        alterThumbCmd.ExecuteNonQuery();
    }

    // Backfill legacy rows created before ThumbnailData existed.
    using (var backfillThumbCmd = connection.CreateCommand())
    {
        backfillThumbCmd.CommandText =
            "UPDATE \"CapturedImages\" " +
            "SET \"ThumbnailData\" = \"ImageData\" " +
            "WHERE \"ThumbnailData\" IS NULL";
        backfillThumbCmd.ExecuteNonQuery();
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

static bool HasColumn(System.Data.Common.DbConnection connection, string tableName, string columnName)
{
    using var pragmaCmd = connection.CreateCommand();
    pragmaCmd.CommandText = $"PRAGMA table_info(\"{tableName}\")";

    using var reader = pragmaCmd.ExecuteReader();
    while (reader.Read())
    {
        if (reader.GetString(1).Equals(columnName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
    }

    return false;
}

static bool HasTable(System.Data.Common.DbConnection connection, string tableName)
{
    using var tableCmd = connection.CreateCommand();
    tableCmd.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = $name";

    var parameter = tableCmd.CreateParameter();
    parameter.ParameterName = "$name";
    parameter.Value = tableName;
    tableCmd.Parameters.Add(parameter);

    var result = tableCmd.ExecuteScalar();
    return result is not null && result != DBNull.Value;
}
