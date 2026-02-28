using PmsAI.Api.Db.Entities;

namespace PmsAI.Api.Db.Repositories;

public interface IDocumentRepository
{
    Task<DocumentEntity?> GetByIdAsync(Guid docId);
    Task<DocumentEntity?> GetByHashAsync(Guid tenantId, string hash);
    Task<int> InsertAsync(DocumentEntity entity);
    Task<int> UpdateAsync(DocumentEntity entity);
}
