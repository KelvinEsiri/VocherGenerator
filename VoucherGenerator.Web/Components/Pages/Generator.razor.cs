using Microsoft.JSInterop;

namespace VoucherGenerator.Web.Components.Pages;

public partial class Generator
{
    private int? Count;
    private int? Digits;
    private string NetworkName = string.Empty;
    private string VoucherType = string.Empty;
    private List<string> GeneratedNumbers = new();
    private bool IsSaved = false;
    private bool IsSaving = false;
    private bool IsGenerating = false;
    private string ErrorMessage = string.Empty;
    private DateTime? GeneratedAt;

    private async Task GenerateVouchers()
    {
        ErrorMessage = string.Empty;
        IsSaved = false;

        // Validate inputs
        if (string.IsNullOrWhiteSpace(NetworkName))
        {
            ErrorMessage = "Network Name is required.";
            return;
        }
        if (string.IsNullOrWhiteSpace(VoucherType))
        {
            ErrorMessage = "Voucher Type is required.";
            return;
        }
        if (Count is null or < 1 or > 1000)
        {
            ErrorMessage = "Voucher count must be between 1 and 1000.";
            return;
        }
        if (Digits is null or < 1 or > 50)
        {
            ErrorMessage = "Digits per code must be between 1 and 50.";
            return;
        }

        IsGenerating = true;
        try
        {
            // Simulate async work (if service is synchronous, keep it quick)
            await Task.Delay(1); // Ensures UI updates
            GeneratedNumbers = VoucherService.GenerateVoucherNumbers(Count.Value, Digits.Value);
            GeneratedAt = DateTime.Now;
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private void ClearAll()
    {
        GeneratedNumbers.Clear();
        IsSaved = false;
        ErrorMessage = string.Empty;
        GeneratedAt = null;
    }

    private async Task SaveVouchers()
    {
        IsSaving = true;
        ErrorMessage = string.Empty;
        try
        {
            await VoucherService.SaveVouchersAsync(GeneratedNumbers, NetworkName, VoucherType);
            IsSaved = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Save failed: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task PrintVouchers()
    {
        await EnsureSaved();
        await JS.InvokeVoidAsync("voucherApp.print");
    }

    private async Task DownloadPdf()
    {
        await EnsureSaved();
        await JS.InvokeVoidAsync("voucherApp.downloadPdf");
    }

    private async Task EnsureSaved()
    {
        if (IsSaved || GeneratedNumbers.Count == 0) return;
        await SaveVouchers();
    }

    private async Task CopyLine(string text)
    {
        await JS.InvokeVoidAsync("voucherApp.copyText", text);
    }

    private async Task CopyCode(string code)
    {
        await JS.InvokeVoidAsync("voucherApp.copyText", code);
    }

    private async Task CopyAllCodes()
    {
        var allCodes = string.Join(Environment.NewLine, GeneratedNumbers);
        await JS.InvokeVoidAsync("voucherApp.copyText", allCodes);
    }

    private async Task CopyAllDetails()
    {
        var allDetails = string.Join(Environment.NewLine,
            GeneratedNumbers.Select(num => $"Voucher Number: {num}, Network Name: {NetworkName}, Voucher Type: {VoucherType}"));
        await JS.InvokeVoidAsync("voucherApp.copyText", allDetails);
    }
}
