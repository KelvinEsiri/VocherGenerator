using Microsoft.JSInterop;

namespace VoucherGenerator.Web.Components.Pages;

public partial class Generator
{
    private int? Count;
    private int? Digits;
    private string NetworkName = string.Empty;
    private string VoucherType = string.Empty;
    private string ValidTill = string.Empty;
    private List<string> GeneratedNumbers = new();
    private bool IsSaved = false;
    private bool IsSaving = false;
    private bool IsGenerating = false;
    private string ErrorMessage = string.Empty;
    private DateTime? GeneratedAt;

    private bool HasGeneratedNumbers => GeneratedNumbers.Count > 0;

    private bool CanClear => HasGeneratedNumbers
        || Count.HasValue
        || Digits.HasValue
        || !string.IsNullOrWhiteSpace(NetworkName)
        || !string.IsNullOrWhiteSpace(VoucherType)
        || !string.IsNullOrWhiteSpace(ValidTill);

    private async Task GenerateVouchers()
    {
        ErrorMessage = string.Empty;
        IsSaved = false;

        if (!TryValidateInputs())
        {
            return;
        }

        var count = Count!.Value;
        var digits = Digits!.Value;

        IsGenerating = true;
        try
        {
            await Task.Yield();
            GeneratedNumbers = VoucherService.GenerateVoucherNumbers(count, digits);
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
        Count = null;
        Digits = null;
        NetworkName = string.Empty;
        VoucherType = string.Empty;
        ValidTill = string.Empty;
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
            await VoucherService.SaveVouchersAsync(GeneratedNumbers, NetworkName, VoucherType, ValidTill);
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
            GeneratedNumbers.Select(BuildVoucherDetail));
        await JS.InvokeVoidAsync("voucherApp.copyText", allDetails);
    }

    private string BuildVoucherDetail(string voucherNumber)
    {
        return string.Join(", ",
            BuildMetaLine("Network Name", NetworkName),
            $"Voucher Number: {voucherNumber}",
            $"Voucher Pass: {voucherNumber}",
            BuildMetaLine("Voucher Type", VoucherType),
            BuildMetaLine("Valid Till", ValidTill));
    }

    private static string BuildMetaLine(string label, string? value)
    {
        return $"{label}: {FormatValue(value)}";
    }

    private bool TryValidateInputs()
    {
        if (string.IsNullOrWhiteSpace(NetworkName))
        {
            ErrorMessage = "Network Name is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(VoucherType))
        {
            ErrorMessage = "Voucher Type is required.";
            return false;
        }

        if (Count is null or < 1 or > 1000)
        {
            ErrorMessage = "Voucher count must be between 1 and 1000.";
            return false;
        }

        if (Digits is null or < 1 or > 50)
        {
            ErrorMessage = "Digits per code must be between 1 and 50.";
            return false;
        }

        return true;
    }

    private static string FormatValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "N/A" : value.Trim();
    }
}
