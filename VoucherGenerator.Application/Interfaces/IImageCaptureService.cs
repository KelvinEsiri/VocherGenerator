using VoucherGenerator.Domain.Entities;

namespace VoucherGenerator.Application.Interfaces;

public interface IImageCaptureService
{
    Task<CapturedImage> SaveImageAsync(string fileName, string contentType, byte[] imageData, byte[] thumbnailData);
    Task<List<CapturedImage>> GetAllAsync();
    Task<CapturedImage?> GetByIdAsync(int id);
    Task<bool> DeleteImageAsync(int id);
}
