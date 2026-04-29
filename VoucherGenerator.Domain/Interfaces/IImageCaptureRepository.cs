using VoucherGenerator.Domain.Entities;

namespace VoucherGenerator.Domain.Interfaces;

public interface IImageCaptureRepository
{
    Task<CapturedImage> AddAsync(CapturedImage image);
    Task<List<CapturedImage>> GetAllAsync();
    Task<CapturedImage?> GetByIdAsync(int id);
    Task<bool> DeleteAsync(int id);
}
