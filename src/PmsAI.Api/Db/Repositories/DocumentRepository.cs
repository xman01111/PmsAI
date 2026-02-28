using PmsAI.Api.Db.Entities;
using SqlSugar;

namespace PmsAI.Api.Db.Repositories;

public class DocumentRepository : SqlSugarRepositoryBase<DocumentEntity>, IDocumentRepository
{
    public DocumentRepository(ISqlSugarClient db) : base(db) { }

    public async Task<DocumentEntity?> GetByIdAsync(Guid docId)
    {
        return await _db.Queryable<DocumentEntity>()
            .Where(d => d.DocId == docId)
            .FirstAsync();
    }

    public async Task<DocumentEntity?> GetByHashAsync(Guid tenantId, string hash)
    {
        return await _db.Queryable<DocumentEntity>()
            .Where(d => d.TenantId == tenantId && d.Hash == hash)
            .FirstAsync();
    }

    public new Task<int> InsertAsync(DocumentEntity entity)
        => _db.Insertable(entity).ExecuteCommandAsync();

    public new Task<int> UpdateAsync(DocumentEntity entity)
        => _db.Updateable(entity).ExecuteCommandAsync();
}
