using CNAS.Repository.Models.Entities;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Linq.Expressions;

namespace SpRepo.Abstraction;

public interface IRepository<TEntity> where TEntity : BaseEntity
{
    TEntity? GetLastAdded();

    IMongoQueryable<TEntity> GetAllAsQueryable();

    IMongoQueryable<TEntity> FindAllAsQueryable(Expression<Func<TEntity, bool>> filter);

    Task<List<TEntity>> GetAllAsync(CancellationToken ct = default);

    List<TEntity> GetAll(CancellationToken ct = default);

    Task<TEntity?> FindByIdAsync(string id, CancellationToken ct = default);

    TEntity? FindById(string id, CancellationToken ct = default);

    Task<List<TEntity>> FindAllAsync(Expression<Func<TEntity, bool>> filter, CancellationToken ct = default);

    Task<List<TEntity>> FindAllAsync(FilterDefinition<TEntity> filter, CancellationToken ct = default);

    List<TEntity> FindAll(Expression<Func<TEntity, bool>> filter, CancellationToken ct = default);

    List<TEntity> FindAll(FilterDefinition<TEntity> filter, CancellationToken ct = default);

    Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> filter, CancellationToken ct = default);

    TEntity? Find(Expression<Func<TEntity, bool>> filter, CancellationToken ct = default);

    Task<bool> Exists(string id, CancellationToken ct = default);

    Task<bool> Exists(Expression<Func<TEntity, bool>> filter, CancellationToken ct = default);

    long Count(Expression<Func<TEntity, bool>> filter, CancellationToken ct = default);

    Task<long> CountAsync(Expression<Func<TEntity, bool>> filter, CancellationToken ct = default);

    void CopyFrom(string fromCollection, RenameCollectionOptions? options = null, string? newCollectionName = null, CancellationToken ct = default);

    void Drop(string? newcollectionName = null, CancellationToken ct = default);

    void Add(TEntity t, InsertOneOptions? options = null, CancellationToken ct = default);

    Task AddAsync(TEntity t, InsertOneOptions? options = null, CancellationToken ct = default);

    void AddAll(IEnumerable<TEntity> t, InsertManyOptions? options = null, CancellationToken ct = default);

    Task AddAllAsync(IEnumerable<TEntity> t, InsertManyOptions? options = null, CancellationToken ct = default);

    TEntity? AddOrUpdate(Expression<Func<TEntity, bool>> condition, TEntity entity, CancellationToken ct = default);

    Task<TEntity?> AddOrUpdateAsync(Expression<Func<TEntity, bool>> condition, TEntity entity, CancellationToken ct = default);

    long BulkAddOrUpdate(Expression<Func<TEntity, bool>> condition, IEnumerable<TEntity?> records, BulkWriteOptions? options = null, CancellationToken ct = default);

    Task<long> BulkAddOrUpdateAsync(Expression<Func<TEntity, bool>> condition, IEnumerable<TEntity?> records, BulkWriteOptions? options = null, CancellationToken ct = default);

    long BulkInsert(Expression<Func<TEntity, bool>> condition, IEnumerable<TEntity?> records, BulkWriteOptions? options = null, CancellationToken ct = default);

    Task<long> BulkInsertAsync(Expression<Func<TEntity, bool>> condition, IEnumerable<TEntity?> records, BulkWriteOptions? options = null, CancellationToken ct = default);

    bool Update(TEntity updated, string key, CancellationToken ct = default);

    bool UpdateAll(FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> updateDefinition, CancellationToken ct = default);

    Task<bool> UpdateAllAsync(FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> updateDefinition, CancellationToken ct = default);

    Task<TEntity?> UpdateAsync(TEntity updated, string key, CancellationToken ct = default);

    Task<TEntity?> UpdateAsync(Expression<Func<TEntity, bool>> condition, TEntity entity, CancellationToken ct = default);

    bool UpdateFields(Expression<Func<TEntity, bool>> condition, UpdateDefinition<TEntity> updateDefinition, CancellationToken ct = default);

    Task<bool> UpdateFieldsAsync(Expression<Func<TEntity, bool>> condition, UpdateDefinition<TEntity> updateDefinition, CancellationToken ct = default);

    Task<bool> DeleteAsync(string key, CancellationToken ct = default);

    Task<bool> DeleteAsync(Expression<Func<TEntity, bool>> condition, CancellationToken ct = default);
}