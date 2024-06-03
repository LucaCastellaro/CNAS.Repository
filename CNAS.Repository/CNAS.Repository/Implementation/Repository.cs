using CNAS.Repository.Models.Entities;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using SpRepo.Abstraction;
using System.Linq.Expressions;

namespace CNAS.Repository.Implementation;

public sealed class Repository<T> : IRepository<T> where T : BaseEntity
{
    private readonly IClientSessionHandle clientSession;

    private readonly string collectionName;

    private readonly IMongoDatabase database;

    public Repository(IMongoDatabase database, IClientSessionHandle clientSession, string? collectionName = default)
    {
        this.database = database ?? throw new ArgumentNullException(nameof(database));

        this.clientSession = clientSession ?? throw new ArgumentNullException(nameof(clientSession));
        this.collectionName = collectionName ?? typeof(T).Name;
    }

    private IMongoCollection<T> _collection => database.GetCollection<T>(collectionName);
    private IMongoQueryable<T> _collectionQueryable => _collection.AsQueryable();

    public T? GetLastAdded()
    {
        return _collectionQueryable.OrderByDescending(xx => xx.Id).FirstOrDefault();
    }

    public IMongoQueryable<T> GetAllAsQueryable()
    {
        return _collectionQueryable;
    }

    public IMongoQueryable<T> FindAllAsQueryable(Expression<Func<T, bool>> filter)
    {
        return _collectionQueryable.Where(filter);
    }

    public Task<List<T>> GetAllAsync(CancellationToken ct = default)
    {
        return FindAllAsync(s => true, ct);
    }

    public List<T> GetAll(CancellationToken ct = default)
    {
        return FindAll(s => true, ct);
    }

    public Task<T?> FindByIdAsync(string id, CancellationToken ct = default)
    {
        return _collection.Find(clientSession, Builders<T>.Filter.Eq(x => x.Id, id))
                   .SingleOrDefaultAsync(ct) as Task<T?>;
    }

    public T? FindById(string id, CancellationToken ct = default)
    {
        return (FindAll(Builders<T>.Filter.Eq(x => x.Id, id), ct)).SingleOrDefault();
    }

    public Task<List<T>> FindAllAsync(Expression<Func<T, bool>> filter, CancellationToken ct = default)
    {
        return _collection.Find(clientSession, filter)
                   .ToListAsync(ct);
    }

    public List<T> FindAll(Expression<Func<T, bool>> filter, CancellationToken ct = default)
    {
        return _collection.Find(clientSession, filter)
                   .ToList(ct);
    }

    public Task<List<T>> FindAllAsync(FilterDefinition<T> filter, CancellationToken ct = default)
    {
        return _collection.Find(clientSession, filter)
                   .ToListAsync(ct);
    }

    public List<T> FindAll(FilterDefinition<T> filter, CancellationToken ct = default)
    {
        return _collection.Find(clientSession, filter)
                   .ToList(ct);
    }

    public Task<T?> FindAsync(Expression<Func<T, bool>> filter, CancellationToken ct = default)
    {
        return _collection.Find(clientSession, filter)
                   .SingleOrDefaultAsync(ct) as Task<T?>;
    }

    public T? Find(Expression<Func<T, bool>> filter, CancellationToken ct = default)
    {
        return _collection.Find(clientSession, filter)
                   .SingleOrDefault(ct);
    }

    public Task<bool> Exists(string id, CancellationToken ct = default)
    {
        return Exists(s => s.Id == id, ct);
    }

    public async Task<bool> Exists(Expression<Func<T, bool>> filter, CancellationToken ct = default)
    {
        return 0 < await CountAsync(filter, ct);
    }

    public long Count(Expression<Func<T, bool>> filter, CancellationToken ct = default)
    {
        return _collection.CountDocuments(clientSession, filter, ct: ct);
    }

    public Task<long> CountAsync(Expression<Func<T, bool>> filter, CancellationToken ct = default)
    {
        return _collection.CountDocumentsAsync(clientSession, filter, ct: ct);
    }

    public void CopyFrom(string fromCollection, CancellationToken ct = default, RenameCollectionOptions? options = null, string? newCollectionName = null)
    {
        Drop(ct);
        database.RenameCollection(fromCollection, string.IsNullOrEmpty(newCollectionName) ? collectionName : newCollectionName, options, ct);
    }

    public void Drop(CancellationToken ct = default, string? newcollectionName = null)
    {
        database.DropCollection(string.IsNullOrEmpty(newcollectionName) ? collectionName : newcollectionName, ct);
    }

    public void Add(T t, CancellationToken ct = default, InsertOneOptions? options = null)
    {
        _collection.InsertOne(clientSession, t, options, ct);
    }

    public Task AddAsync(T t, CancellationToken ct = default, InsertOneOptions? options = null)
    {
        return _collection.InsertOneAsync(clientSession, t, options, ct);
    }

    public void AddAll(IEnumerable<T> t, CancellationToken ct = default, InsertManyOptions? options = null)
    {
        _collection.InsertMany(clientSession, t, options, ct);
    }

    public Task AddAllAsync(IEnumerable<T> t, CancellationToken ct = default, InsertManyOptions? options = null)
    {
        return _collection.InsertManyAsync(clientSession, t, options, ct);
    }

    public T? AddOrUpdate(Expression<Func<T, bool>> condition, T entity, CancellationToken ct = default)
    {
        var resultOk = _collection.ReplaceOne(clientSession, condition, entity, new ReplaceOptions
        {
            IsUpsert = true
        }, ct);

        return resultOk.IsAcknowledged ? entity : null;
    }

    public async Task<T?> AddOrUpdateAsync(Expression<Func<T, bool>> condition, T entity, CancellationToken ct = default)
    {
        try
        {
            var resultOk = await _collection.ReplaceOneAsync(clientSession, condition, entity, new ReplaceOptions
            {
                IsUpsert = true
            }, ct);

            return resultOk.IsAcknowledged ? entity : null;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public long BulkAddOrUpdate(Expression<Func<T, bool>> condition, IEnumerable<T?> records, CancellationToken ct = default, BulkWriteOptions? options = null)
    {
        var bulkOps = new List<WriteModel<T>>();

        foreach (var record in records)
        {
            if (record == null) continue;
            var upsertOne = new ReplaceOneModel<T>(condition, record)
            {
                IsUpsert = true
            };

            bulkOps.Add(upsertOne);
        }

        options ??= new BulkWriteOptions
        {
            BypassDocumentValidation = false,
            IsOrdered = false
        };

        return _collection.BulkWrite(clientSession, bulkOps, options, ct).InsertedCount;
    }

    public async Task<long> BulkAddOrUpdateAsync(Expression<Func<T, bool>> condition, IEnumerable<T?> records, CancellationToken ct = default, BulkWriteOptions? options = null)
    {
        var bulkOps = new List<WriteModel<T>>();

        foreach (var record in records)
        {
            if (record == null) continue;
            var upsertOne = new ReplaceOneModel<T>(condition, record)
            {
                IsUpsert = true
            };

            bulkOps.Add(upsertOne);
        }

        options ??= new BulkWriteOptions
        {
            BypassDocumentValidation = false,
            IsOrdered = false
        };

        var result = await _collection.BulkWriteAsync(clientSession, bulkOps, options, ct);

        return result.InsertedCount;
    }

    public long BulkInsert(Expression<Func<T, bool>> condition, IEnumerable<T?> records, CancellationToken ct = default, BulkWriteOptions? options = null)
    {
        var bulkOps = new List<WriteModel<T>>();

        foreach (var record in records)
        {
            if (record == null) continue;
            var upsertOne = new ReplaceOneModel<T>(condition, record)
            {
                IsUpsert = false
            };

            bulkOps.Add(upsertOne);
        }

        options ??= new BulkWriteOptions
        {
            BypassDocumentValidation = false,
            IsOrdered = false
        };

        return _collection.BulkWrite(clientSession, bulkOps, options, ct).InsertedCount;
    }

    public async Task<long> BulkInsertAsync(Expression<Func<T, bool>> condition, IEnumerable<T?> records, CancellationToken ct = default, BulkWriteOptions? options = null)
    {
        var bulkOps = new List<WriteModel<T>>();

        foreach (var record in records)
        {
            if (record == null) continue;
            var upsertOne = new ReplaceOneModel<T>(condition, record)
            {
                IsUpsert = false
            };

            bulkOps.Add(upsertOne);
        }

        options ??= new BulkWriteOptions
        {
            BypassDocumentValidation = false,
            IsOrdered = false
        };

        var result = await _collection.BulkWriteAsync(clientSession, bulkOps, options, ct);

        return result.InsertedCount;
    }

    public bool Update(T entity, string key, CancellationToken ct = default)
    {
        var result = _collection.ReplaceOne(clientSession, x => x.Id == key, entity, new ReplaceOptions
        {
            IsUpsert = false
        }, ct);

        return result.IsAcknowledged;
    }

    public bool UpdateAll(FilterDefinition<T> filter, UpdateDefinition<T> updateDefinition, CancellationToken ct = default)
    {
        var result = _collection.UpdateMany(clientSession, filter, updateDefinition, ct: ct);

        return result.IsAcknowledged;
    }

    public async Task<bool> UpdateAllAsync(FilterDefinition<T> filter, UpdateDefinition<T> updateDefinition, CancellationToken ct = default)
    {
        var result = await _collection.UpdateManyAsync(clientSession, filter, updateDefinition, ct: ct);

        return result.IsAcknowledged;
    }

    public async Task<T?> UpdateAsync(T entity, string key, CancellationToken ct = default)
    {
        var resultOk = await _collection.ReplaceOneAsync(clientSession, x => x.Id == key, entity, new ReplaceOptions
        {
            IsUpsert = false
        }, ct);

        return (resultOk.IsAcknowledged) ? entity : null;
    }

    public async Task<T?> UpdateAsync(Expression<Func<T, bool>> condition, T entity, CancellationToken ct = default)
    {
        var resultOk = await _collection.ReplaceOneAsync(clientSession, condition, entity, new ReplaceOptions
        {
            IsUpsert = false
        }, ct);

        return (resultOk.IsAcknowledged) ? entity : null;
    }

    public bool UpdateFields(Expression<Func<T, bool>> condition, UpdateDefinition<T> updateDefinition, CancellationToken ct = default)
    {
        //example usage
        //var upd = Builders<CandeleDoc>.Update.Set(r => r.PrezzoApertura, cc.PrezzoApertura)
        //    .Set(r => r.Volumi, cc.pre)
        //    .Set(r => r.PrezzoChiusura, cc.PrezzoChiusura)
        //    .Set(r => r.PrezzoMinimo, cc.PrezzoMinimo)
        //    .Set(r => r.PrezzoMassimo, cc.PrezzoMassimo);

        var result = _collection.UpdateMany(clientSession, condition, updateDefinition, new UpdateOptions
        {
            IsUpsert = false
        });
        return result.IsAcknowledged;
    }

    public async Task<bool> UpdateFieldsAsync(Expression<Func<T, bool>> condition, UpdateDefinition<T> updateDefinition, CancellationToken ct = default)
    {
        //example usage
        //var upd = Builders<CandeleDoc>.Update.Set(r => r.PrezzoApertura, cc.PrezzoApertura)
        //    .Set(r => r.Volumi, cc.pre)
        //    .Set(r => r.PrezzoChiusura, cc.PrezzoChiusura)
        //    .Set(r => r.PrezzoMinimo, cc.PrezzoMinimo)
        //    .Set(r => r.PrezzoMassimo, cc.PrezzoMassimo);

        var result = await _collection.UpdateManyAsync(clientSession, condition, updateDefinition, new UpdateOptions
        {
            IsUpsert = false
        }, ct);

        return result.IsAcknowledged;
    }

    public async Task<bool> DeleteAsync(string key, CancellationToken ct = default)
    {
        var result = await _collection.DeleteOneAsync(key, ct);
        return result.IsAcknowledged;
    }

    public async Task<bool> DeleteAsync(Expression<Func<T, bool>> key, CancellationToken ct = default)
    {
        var result = await _collection.DeleteManyAsync(key, ct);
        return result.IsAcknowledged;
    }
}