// using Microsoft.AspNetCore.Components.Forms; // file upload — commented out
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using VoucherGenerator.Domain.Entities;

namespace VoucherGenerator.Web.Components.Pages;

public partial class ImageCapture : IAsyncDisposable
{
    // JS is injected via @inject IJSRuntime JS in ImageCapture.razor

    // ── State ──────────────────────────────────────────────────────────────────
    private readonly List<CapturedImage> Images = new();
    private const int PageSize = 12;
    private string FileName = string.Empty;
    private string _searchQuery = string.Empty;
    private string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (_searchQuery == value)
            {
                return;
            }

            _searchQuery = value;
            CurrentPage = 1;
        }
    }
    private string ErrorMessage = string.Empty;
    private string SuccessMessage = string.Empty;
    private bool IsLoading = true;
    private bool IsSaving;
    private bool IsDeleting;
    private int CurrentPage = 1;

    // Camera state
    private bool IsCameraActive;
    private string CapturedDataUrl = string.Empty;   // data:image/png;base64,…
    private string CapturedThumbnailDataUrl = string.Empty;

    // Saved-image preview
    private string? SelectedImageDataUrl;
    private string SelectedImageFileName = string.Empty;
    private int? SelectedImageId;
    private int? PendingDeleteImageId;
    private string PendingDeleteFileName = string.Empty;
    private bool IsPreviewOpen => !string.IsNullOrWhiteSpace(SelectedImageDataUrl);
    private bool IsDeleteConfirmOpen => PendingDeleteImageId.HasValue;
    private List<CapturedImage> FilteredImages =>
        string.IsNullOrWhiteSpace(SearchQuery)
            ? Images
            : Images
                .Where(i => i.FileName.Contains(SearchQuery.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
    private int FilteredCount => FilteredImages.Count;
    private int TotalPages => Math.Max(1, (int)Math.Ceiling(FilteredCount / (double)PageSize));
    private IEnumerable<CapturedImage> PagedImages => FilteredImages.Skip((CurrentPage - 1) * PageSize).Take(PageSize);
    private int PageStart => FilteredCount == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;
    private int PageEnd => Math.Min(CurrentPage * PageSize, FilteredCount);

    // ── File upload state — commented out; using camera capture instead ────────
    // private IBrowserFile? SelectedFile;
    // private string SelectedBrowserFileName = string.Empty;
    // private const long MaxUploadBytes = 10 * 1024 * 1024;
    //
    // private Task OnFileSelected(InputFileChangeEventArgs e)
    // {
    //     SelectedFile = e.File;
    //     SelectedBrowserFileName = SelectedFile?.Name ?? string.Empty;
    //     if (string.IsNullOrWhiteSpace(FileName) && SelectedFile is not null)
    //         FileName = Path.GetFileNameWithoutExtension(SelectedFile.Name);
    //     return Task.CompletedTask;
    // }

    // ── Lifecycle ──────────────────────────────────────────────────────────────
    protected override async Task OnInitializedAsync()
    {
        await LoadImages();
    }

    public async ValueTask DisposeAsync()
    {
        // Ensure camera stream is released when navigating away
        try { await JS.InvokeVoidAsync("imageCaptureApp.stopCamera", "cameraVideo"); }
        catch { /* component may already be torn down */ }
    }

    // ── Camera methods ─────────────────────────────────────────────────────────
    private async Task StartCameraAsync()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        var started = await JS.InvokeAsync<bool>("imageCaptureApp.startCamera", "cameraVideo");
        if (started)
        {
            IsCameraActive = true;
        }
        else
        {
            ErrorMessage = "Camera access denied or not available. Please allow camera permissions and try again.";
        }
    }

    private async Task StopCameraAsync()
    {
        await JS.InvokeVoidAsync("imageCaptureApp.stopCamera", "cameraVideo");
        IsCameraActive = false;
    }

    private async Task CaptureFrameAsync()
    {
        var captured = await JS.InvokeAsync<CaptureFrameResult?>("imageCaptureApp.captureFrame", "cameraVideo", "cameraCanvas", 220);
        if (captured is null || string.IsNullOrEmpty(captured.FullDataUrl))
        {
            ErrorMessage = "Could not capture frame. Ensure the camera feed is active.";
            return;
        }

        CapturedDataUrl = captured.FullDataUrl;
        CapturedThumbnailDataUrl = captured.ThumbnailDataUrl ?? string.Empty;
        await StopCameraAsync();
    }

    private void ClearCapture()
    {
        CapturedDataUrl = string.Empty;
        CapturedThumbnailDataUrl = string.Empty;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    // ── Save ───────────────────────────────────────────────────────────────────
    private async Task SaveImage()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(FileName))
        {
            ErrorMessage = "Filename is required.";
            return;
        }

        if (string.IsNullOrEmpty(CapturedDataUrl))
        {
            ErrorMessage = "Capture an image first.";
            return;
        }

        if (!TryParseDataUrl(CapturedDataUrl, out var contentType, out var imageBytes))
        {
            ErrorMessage = "Invalid image data.";
            return;
        }

        var hasThumb = TryParseDataUrl(CapturedThumbnailDataUrl, out _, out var thumbnailBytes);
        if (!hasThumb)
        {
            thumbnailBytes = imageBytes;
        }

        IsSaving = true;
        try
        {
            var saved = await ImageCaptureService.SaveImageAsync(FileName, contentType, imageBytes, thumbnailBytes);

            SuccessMessage = string.Empty;
            FileName = string.Empty;
            CapturedDataUrl = string.Empty;
            CapturedThumbnailDataUrl = string.Empty;

            await LoadImages();
            await SelectImage(saved.Id);
            await JS.InvokeVoidAsync("voucherApp.showToast", "Image saved.", "success");
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

    // ── List helpers ───────────────────────────────────────────────────────────
    private async Task LoadImages()
    {
        IsLoading = true;
        var loaded = await ImageCaptureService.GetAllAsync();
        Images.Clear();
        Images.AddRange(loaded);
        CurrentPage = Math.Min(CurrentPage, TotalPages);
        IsLoading = false;
    }

    private async Task SelectImage(int imageId)
    {
        ErrorMessage = string.Empty;

        var image = await ImageCaptureService.GetByIdAsync(imageId);
        if (image is null)
        {
            ErrorMessage = "Selected image was not found.";
            return;
        }

        SelectedImageId = image.Id;
        SelectedImageFileName = image.FileName;
        SelectedImageDataUrl = $"data:{image.ContentType};base64,{Convert.ToBase64String(image.ImageData)}";
    }

    private void PromptDelete(int imageId, string fileName)
    {
        if (IsDeleting)
        {
            return;
        }

        PendingDeleteImageId = imageId;
        PendingDeleteFileName = fileName;
    }

    private async Task ConfirmDeleteAsync()
    {
        if (IsDeleting || !PendingDeleteImageId.HasValue)
        {
            return;
        }

        var imageId = PendingDeleteImageId.Value;
        var fileName = PendingDeleteFileName;

        IsDeleting = true;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        try
        {
            var deleted = await ImageCaptureService.DeleteImageAsync(imageId);
            if (!deleted)
            {
                ErrorMessage = "Image not found or already deleted.";
                CancelDelete();
                return;
            }

            if (SelectedImageId == imageId)
            {
                ClosePreview();
            }

            await LoadImages();
            SuccessMessage = string.Empty;
            await JS.InvokeVoidAsync("voucherApp.showToast", $"Deleted {fileName}.", "success");
            CancelDelete();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Delete failed: {ex.Message}";
        }
        finally
        {
            IsDeleting = false;
        }
    }

    private void ClearSearch()
    {
        SearchQuery = string.Empty;
    }

    private sealed class CaptureFrameResult
    {
        public string? FullDataUrl { get; set; }
        public string? ThumbnailDataUrl { get; set; }
    }

    private static bool TryParseDataUrl(string dataUrl, out string contentType, out byte[] bytes)
    {
        contentType = string.Empty;
        bytes = Array.Empty<byte>();

        if (string.IsNullOrWhiteSpace(dataUrl))
        {
            return false;
        }

        var commaIndex = dataUrl.IndexOf(',');
        if (commaIndex < 0)
        {
            return false;
        }

        var meta = dataUrl[..commaIndex];
        var base64 = dataUrl[(commaIndex + 1)..];
        contentType = meta.Replace("data:", string.Empty)
                          .Replace(";base64", string.Empty);

        try
        {
            bytes = Convert.FromBase64String(base64);
            return true;
        }
        catch
        {
            contentType = string.Empty;
            bytes = Array.Empty<byte>();
            return false;
        }
    }

    private string BuildThumbnailDataUrl(CapturedImage image)
    {
        return $"data:{image.ContentType};base64,{Convert.ToBase64String(image.ThumbnailData)}";
    }

    private void GoToPage(int page)
    {
        if (page < 1 || page > TotalPages)
        {
            return;
        }

        CurrentPage = page;
    }

    private void PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
        }
    }

    private void NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
        }
    }

    private void CancelDelete()
    {
        PendingDeleteImageId = null;
        PendingDeleteFileName = string.Empty;
    }

    private void ClosePreview()
    {
        SelectedImageDataUrl = null;
        SelectedImageFileName = string.Empty;
        SelectedImageId = null;
    }

}
