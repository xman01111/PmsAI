using SqlSugar;

namespace PmsAI.Api.Db.Repositories;

public class SqlSugarRepositoryBase<T> where T : class, new()
{
    protected readonly ISqlSugarClient _db;

    public SqlSugarRepositoryBase(ISqlSugarClient db) => _db = db;

    public Task<T> GetByIdAsync(dynamic id) => _db.Queryable<T>().InSingleAsync(id);
    public Task<List<T>> GetListAsync() => _db.Queryable<T>().ToListAsync();
    public Task<int> InsertAsync(T entity) => _db.Insertable(entity).ExecuteCommandAsync();
    public Task<int> UpdateAsync(T entity) => _db.Updateable(entity).ExecuteCommandAsync();
    public Task<int> DeleteAsync(dynamic id) => _db.Deleteable<T>().In(id).ExecuteCommandAsync();
}
