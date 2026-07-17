using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.Records;

public interface IFileAttachmentContentStore
{
    void Store(FileAttachment attachment, byte[] content);
    Task<byte[]?> ReadAsync(Guid attachmentId, string expectedStatus, CancellationToken cancellationToken);
    void Delete(FileAttachment attachment);
}

public sealed class PostgresFileAttachmentContentStore(OpenBusinessPlatformDbContext dbContext) : IFileAttachmentContentStore
{
    public void Store(FileAttachment attachment, byte[] content)
    {
        attachment.StorageProvider = AttachmentStorageProviders.Postgres;
        attachment.StorageKey = attachment.Id.ToString("N");
        attachment.Content = content;
    }

    public Task<byte[]?> ReadAsync(Guid attachmentId, string expectedStatus, CancellationToken cancellationToken) =>
        dbContext.FileAttachments.AsNoTracking().Where(item => item.Id == attachmentId && item.Status == expectedStatus).Select(item => item.Content).FirstOrDefaultAsync(cancellationToken);

    public void Delete(FileAttachment attachment) => attachment.Content = Array.Empty<byte>();
}
