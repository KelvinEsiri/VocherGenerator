using Microsoft.EntityFrameworkCore;
using VoucherGenerator.Domain.Entities;
using VoucherGenerator.Domain.Interfaces;
using VoucherGenerator.Infrastructure.Data;

namespace VoucherGenerator.Infrastructure.Repositories;

public class ImageCaptureRepository : IImageCaptureRepository
{
    private readonly AppDbContext _db;

    public ImageCaptureRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CapturedImage> AddAsync(CapturedImage image)
    {
        _db.CapturedImages.Add(image);
        await _db.SaveChangesAsync();
        return image;
    }

    public Task<List<CapturedImage>> GetAllAsync() =>
        _db.CapturedImages
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new CapturedImage
            {
                Id = i.Id,
                FileName = i.FileName,
                ContentType = i.ContentType,
                CreatedAt = i.CreatedAt,
                ImageData = Array.Empty<byte>(),
                ThumbnailData = i.ThumbnailData ?? Array.Empty<byte>()
            })
            .ToListAsync();

    public Task<CapturedImage?> GetByIdAsync(int id) =>
        _db.CapturedImages.FirstOrDefaultAsync(i => i.Id == id);

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.CapturedImages.FirstOrDefaultAsync(i => i.Id == id);
        if (entity is null)
        {
            return false;
        }

        _db.CapturedImages.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }
}
