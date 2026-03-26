using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public sealed class PushDeviceTokenService : IPushDeviceTokenService
{
    private readonly ApplicationDbContext _db;

    public PushDeviceTokenService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task RegisterAsync(RegisterPushTokenRequest request, CancellationToken ct = default)
    {
        var existing = await _db.PushDeviceTokens
            .FirstOrDefaultAsync(t => t.Token == request.Token && t.TenantId == request.TenantId, ct);

        if (existing != null)
        {
            existing.UserId = request.UserId;
            existing.PatientId = request.PatientId;
            existing.Platform = request.Platform;
            existing.DeviceId = request.DeviceId;
            existing.IsActive = true;
            existing.LastUsedAt = DateTime.UtcNow;
        }
        else
        {
            _db.PushDeviceTokens.Add(new PushDeviceToken
            {
                TenantId = request.TenantId,
                UserId = request.UserId,
                PatientId = request.PatientId,
                Token = request.Token,
                Platform = request.Platform,
                DeviceId = request.DeviceId,
                IsActive = true,
                LastUsedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task UnregisterAsync(Guid tenantId, string userId, string token, CancellationToken ct = default)
    {
        var existing = await _db.PushDeviceTokens
            .FirstOrDefaultAsync(t => t.Token == token && t.TenantId == tenantId && t.UserId == userId, ct);
        if (existing != null)
        {
            existing.IsActive = false;
            await _db.SaveChangesAsync(ct);
        }
    }
}
