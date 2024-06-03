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

    private IMongoCollection<T> Collection => database.GetCollection<T>(collectionName);
    private IMongoQueryable<T> CollectionQueryable => Collection.AsQueryable();

    public T? GetLastAdded()
    {
        return CollectionQueryable.OrderByDescending(xx => xx.Id).FirstOrDefault();
    }

    public IMongoQueryable<T> GetAllAsQueryable()
    {
        return CollectionQueryable;
    }

    public IMongoQueryable<T> FindAllAsQueryable(Expression<Func<T, bool>> filter)
    {
        return CollectionQueryable.Where(filter);
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
        return Collection.Find(clientSession, Builders<T>.Filter.Eq(x => x.Id, id))
                   .SingleOrDefaultAsync(ct) as Task<T?>;
    }

    public T? FindById(string id, CancellationToken ct = default)
    {
        return (FindAll(Builders<T>.Filter.Eq(x => x.Id, id), ct)).SingleOrDefault();
    }

    public Task<List<T>> FindAllAsync(Expression<Func<T, bool>> filter, CancellationToken ct = default)
    {
        return Collection.Find(clientSession, filter)
                   .ToListAsync(ct);
    }

    public List<T> FindAll(Expression<Func<T, bool>> filter, CancellationToken ct = default)
    {
        return Collection.Find(clientSession, filter)
                   .ToList(ct);
    }

    public Task<List<T>> FindAllAsync(FilterDefinition<T> filter, CancellationToken ct = default)
    {
        return Collection.Find(clientSession, filter)
                   .ToListAsync(ct);
    }

    public List<T> FindAll(FilterDefinition<T> filter, CancellationToken ct = default)
    {
        return Collection.Find(clientSession, filter)
                   .ToList(ct);
    }

    public Task<T?> FindAsync(Expression<Func<T, bool>> filter, CancellationToken ct = default)
    {
        return Collection.Find(clientSession, filter)
                   .SingleOrDefaultAsync(ct) as Task<T?>;
    }

    public T? Find(Expression<Func<T, bool>> filter, CancellationToken ct = default)
    {
        return Collection.Find(clientSession, filter)
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
        return Collection.CountDocuments(clientSession, filter, cancellationToken: ct);
    }

    public Task<long> CountAsync(Expression<Func<T, bool>> filter, CancellationToken ct = default)
    {
        return Collection.CountDocumentsAsync(clientSession, filter, cancellationToken: ct);
    }

    public void CopyFrom(string fromCollection, RenameCollectionOptions? options = null, string? newCollectionName = null, CancellationToken ct = default)
    {
        Drop(ct: ct);
        database.RenameCollection(fromCollection, string.IsNullOrEmpty(newCollectionName) ? collectionName : newCollectionName, options, ct);
    }

    public void Drop(string? newcollectionName = null, CancellationToken ct = default)
    {
        database.DropCollection(string.IsNullOrEmpty(newcollectionName) ? collectionName : newcollectionName, ct);
    }

    public void Add(T t, InsertOneOptions? options = null, CancellationToken ct = default)
    {
        Collection.InsertOne(clientSession, t, options, ct);
    }

    public Task AddAsync(T t, InsertOneOptions? options = null, CancellationToken ct = default)
    {
        return Collection.InsertOneAsync(clientSession, t, options, ct);
    }

    public void AddAll(IEnumerable<T> t, InsertManyOptions? options = null, CancellationToken ct = default)
    {
        Collection.InsertMany(clientSession, t, options, ct);
    }

    public Task AddAllAsync(IEnumerable<T> t, InsertManyOptions? options = null, CancellationToken ct = default)
    {
        return Collection.InsertManyAsync(clientSession, t, options, ct);
    }

    public T? AddOrUpdate(Expression<Func<T, bool>> condition, T entity, CancellationToken ct = default)
    {
        var resultOk = Collection.ReplaceOne(clientSession, condition, entity, new ReplaceOptions
        {
            IsUpsert = true
        }, ct);

        return resultOk.IsAcknowledged ? entity : null;
    }

    public async Task<T?> AddOrUpdateAsync(Expression<Func<T, bool>> condition, T entity, CancellationToken ct = default)
    {
        try
        {
            var resultOk = await Collection.ReplaceOneAsync(clientSession, condition, entity, new ReplaceOptions
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

    public long BulkAddOrUpdate(Expression<Func<T, bool>> condition, IEnumerable<T?> records, BulkWriteOptions? options = null, CancellationToken ct = default)
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

        return Collection.BulkWrite(clientSession, bulkOps, options, ct).InsertedCount;
    }

    public async Task<long> BulkAddOrUpdateAsync(Expression<Func<T, bool>> condition, IEnumerable<T?> records, BulkWriteOptions? options = null, CancellationToken ct = default)
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

        var result = await Collection.BulkWriteAsync(clientSession, bulkOps, options, ct);

        return result.InsertedCount;
    }

    public long BulkInsert(Expression<Func<T, bool>> condition, IEnumerable<T?> records, BulkWriteOptions? options = null, CancellationToken ct = default)
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

        return Collection.BulkWrite(clientSession, bulkOps, options, ct).InsertedCount;
    }

    public async Task<long> BulkInsertAsync(Expression<Func<T, bool>> condition, IEnumerable<T?> records, BulkWriteOptions? options = null, CancellationToken ct = default)
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

        var result = await Collection.BulkWriteAsync(clientSession, bulkOps, options, ct);

        return result.InsertedCount;
    }

    public bool Update(T entity, string key, CancellationToken ct = default)
    {
        var result = Collection.ReplaceOne(clientSession, x => x.Id == key, entity, new ReplaceOptions
        {
            IsUpsert = false
        }, ct);

        return result.IsAcknowledged;
    }

    public bool UpdateAll(FilterDefinition<T> filter, UpdateDefinition<T> updateDefinition, CancellationToken ct = default)
    {
        var result = Collection.UpdateMany(clientSession, filter, updateDefinition, cancellationToken: ct);

        return result.IsAcknowledged;
    }

    public async Task<bool> UpdateAllAsync(FilterDefinition<T> filter, UpdateDefinition<T> updateDefinition, CancellationToken ct = default)
    {
        var result = await Collection.UpdateManyAsync(clientSession, filter, updateDefinition, cancellationToken: ct);

        return result.IsAcknowledged;
    }

    public async Task<T?> UpdateAsync(T entity, string key, CancellationToken ct = default)
    {
        var resultOk = await Collection.ReplaceOneAsync(clientSession, x => x.Id == key, entity, new ReplaceOptions
        {
            IsUpsert = false
        }, ct);

        return (resultOk.IsAcknowledged) ? entity : null;
    }

    public async Task<T?> UpdateAsync(Expression<Func<T, bool>> condition, T entity, CancellationToken ct = default)
    {
        var resultOk = await Collection.ReplaceOneAsync(clientSession, condition, entity, new ReplaceOptions
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

        var result = Collection.UpdateMany(clientSession, condition, updateDefinition, new UpdateOptions
        {
            IsUpsert = false
        }, ct);
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

        var result = await Collection.UpdateManyAsync(clientSession, condition, updateDefinition, new UpdateOptions
        {
            IsUpsert = false
        }, ct);

        return result.IsAcknowledged;
    }

    public async Task<bool> DeleteAsync(string key, CancellationToken ct = default)
    {
        var result = await Collection.DeleteOneAsync(key, ct);
        return result.IsAcknowledged;
    }

    public async Task<bool> DeleteAsync(Expression<Func<T, bool>> key, CancellationToken ct = default)
    {
        var result = await Collection.DeleteManyAsync(key, ct);
        return result.IsAcknowledged;
    }
}