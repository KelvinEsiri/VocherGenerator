using VoucherGenerator.Application.Interfaces;
using VoucherGenerator.Domain.Entities;
using VoucherGenerator.Domain.Interfaces;

namespace VoucherGenerator.Application.Services;

public class ImageCaptureService : IImageCaptureService
{
    private readonly IImageCaptureRepository _repo;

    public ImageCaptureService(IImageCaptureRepository repo)
    {
        _repo = repo;
    }

    public async Task<CapturedImage> SaveImageAsync(string fileName, string contentType, byte[] imageData, byte[] thumbnailData)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Filename is required.", nameof(fileName));
        }

        if (string.IsNullOrWhiteSpace(contentType) || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("A valid image content type is required.", nameof(contentType));
        }

        if (imageData.Length == 0)
        {
            throw new ArgumentException("Image data is empty.", nameof(imageData));
        }

        if (thumbnailData.Length == 0)
        {
            throw new ArgumentException("Thumbnail data is empty.", nameof(thumbnailData));
        }

        var image = new CapturedImage
        {
            FileName = fileName.Trim(),
            ContentType = contentType.Trim(),
            ImageData = imageData,
            ThumbnailData = thumbnailData,
            CreatedAt = DateTime.UtcNow
        };

        return await _repo.AddAsync(image);
    }

    public Task<List<CapturedImage>> GetAllAsync() => _repo.GetAllAsync();

    public Task<CapturedImage?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);

    public Task<bool> DeleteImageAsync(int id) => _repo.DeleteAsync(id);
}
