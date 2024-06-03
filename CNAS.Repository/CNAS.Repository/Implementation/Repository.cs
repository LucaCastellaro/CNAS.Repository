using CNAS.Repository.Models.Entities;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using SpRepo.Abstraction;
using System.Linq.Expressions;

namespace CNAS.Repository.Implementation;

public sealed class Repository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
{
    private readonly IClientSessionHandle clientSession;

    private readonly string collectionName;

    private readonly IMongoDatabase database;

    public Repository(IMongoDatabase database, IClientSessionHandle clientSession, string? collectionName = default)
    {
        this.database = database ?? throw new ArgumentNullException(nameof(database));

        this.clientSession = clientSession ?? throw new ArgumentNullException(nameof(clientSession));
        this.collectionName = collectionName ?? typeof(TEntity).Name;
    }

    private IMongoCollection<TEntity> Collection => database.GetCollection<TEntity>(collectionName);
    private IMongoQueryable<TEntity> CollectionQueryable => Collection.AsQueryable();

    public TEntity? GetLastAdded()
    {
        return CollectionQueryable.OrderByDescending(xx => xx.Id).FirstOrDefault();
    }

    public IMongoQueryable<TEntity> GetAllAsQueryable()
    {
        return CollectionQueryable;
    }

    public IMongoQueryable<TEntity> FindAllAsQueryable(Expression<Func<TEntity, bool>> filter)
    {
        return CollectionQueryable.Where(filter);
    }

    public Task<List<TEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return FindAllAsync(s => true, ct);
    }

    public List<TEntity> GetAll(CancellationToken ct = default)
    {
        return FindAll(s => true, ct);
    }

    public Task<TEntity?> FindByIdAsync(string id, CancellationToken ct = default)
    {
        return Collection.Find(clientSession, Builders<TEntity>.Filter.Eq(x => x.Id, id))
                   .SingleOrDefaultAsync(ct) as Task<TEntity?>;
    }

    public TEntity? FindById(string id, CancellationToken ct = default)
    {
        return (FindAll(Builders<TEntity>.Filter.Eq(x => x.Id, id), ct)).SingleOrDefault();
    }

    public Task<List<TEntity>> FindAllAsync(Expression<Func<TEntity, bool>> filter, CancellationToken ct = default)
    {
        return Collection.Find(clientSession, filter)
                   .ToListAsync(ct);
    }

    public List<TEntity> FindAll(Expression<Func<TEntity, bool>> filter, CancellationToken ct = default)
    {
        return Collection.Find(clientSession, filter)
                   .ToList(ct);
    }

    public Task<List<TEntity>> FindAllAsync(FilterDefinition<TEntity> filter, CancellationToken ct = default)
    {
        return Collection.Find(clientSession, filter)
                   .ToListAsync(ct);
    }

    public List<TEntity> FindAll(FilterDefinition<TEntity> filter, CancellationToken ct = default)
    {
        return Collection.Find(clientSession, filter)
                   .ToList(ct);
    }

    public Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> filter, CancellationToken ct = default)
    {
        return Collection.Find(clientSession, filter)
                   .SingleOrDefaultAsync(ct) as Task<TEntity?>;
    }

    public TEntity? Find(Expression<Func<TEntity, bool>> filter, CancellationToken ct = default)
    {
        return Collection.Find(clientSession, filter)
                   .SingleOrDefault(ct);
    }

    public Task<bool> Exists(string id, CancellationToken ct = default)
    {
        return Exists(s => s.Id == id, ct);
    }

    public async Task<bool> Exists(Expression<Func<TEntity, bool>> filter, CancellationToken ct = default)
    {
        return 0 < await CountAsync(filter, ct);
    }

    public long Count(Expression<Func<TEntity, bool>> filter, CancellationToken ct = default)
    {
        return Collection.CountDocuments(clientSession, filter, cancellationToken: ct);
    }

    public Task<long> CountAsync(Expression<Func<TEntity, bool>> filter, CancellationToken ct = default)
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

    public void Add(TEntity t, InsertOneOptions? options = null, CancellationToken ct = default)
    {
        Collection.InsertOne(clientSession, t, options, ct);
    }

    public Task AddAsync(TEntity t, InsertOneOptions? options = null, CancellationToken ct = default)
    {
        return Collection.InsertOneAsync(clientSession, t, options, ct);
    }

    public void AddAll(IEnumerable<TEntity> t, InsertManyOptions? options = null, CancellationToken ct = default)
    {
        Collection.InsertMany(clientSession, t, options, ct);
    }

    public Task AddAllAsync(IEnumerable<TEntity> t, InsertManyOptions? options = null, CancellationToken ct = default)
    {
        return Collection.InsertManyAsync(clientSession, t, options, ct);
    }

    public TEntity? AddOrUpdate(Expression<Func<TEntity, bool>> condition, TEntity entity, CancellationToken ct = default)
    {
        var resultOk = Collection.ReplaceOne(clientSession, condition, entity, new ReplaceOptions
        {
            IsUpsert = true
        }, ct);

        return resultOk.IsAcknowledged ? entity : null;
    }

    public async Task<TEntity?> AddOrUpdateAsync(Expression<Func<TEntity, bool>> condition, TEntity entity, CancellationToken ct = default)
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

    public long BulkAddOrUpdate(Expression<Func<TEntity, bool>> condition, IEnumerable<TEntity?> records, BulkWriteOptions? options = null, CancellationToken ct = default)
    {
        var bulkOps = new List<WriteModel<TEntity>>();

        foreach (var record in records)
        {
            if (record == null) continue;
            var upsertOne = new ReplaceOneModel<TEntity>(condition, record)
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

    public async Task<long> BulkAddOrUpdateAsync(Expression<Func<TEntity, bool>> condition, IEnumerable<TEntity?> records, BulkWriteOptions? options = null, CancellationToken ct = default)
    {
        var bulkOps = new List<WriteModel<TEntity>>();

        foreach (var record in records)
        {
            if (record == null) continue;
            var upsertOne = new ReplaceOneModel<TEntity>(condition, record)
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

    public long BulkInsert(Expression<Func<TEntity, bool>> condition, IEnumerable<TEntity?> records, BulkWriteOptions? options = null, CancellationToken ct = default)
    {
        var bulkOps = new List<WriteModel<TEntity>>();

        foreach (var record in records)
        {
            if (record == null) continue;
            var upsertOne = new ReplaceOneModel<TEntity>(condition, record)
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

    public async Task<long> BulkInsertAsync(Expression<Func<TEntity, bool>> condition, IEnumerable<TEntity?> records, BulkWriteOptions? options = null, CancellationToken ct = default)
    {
        var bulkOps = new List<WriteModel<TEntity>>();

        foreach (var record in records)
        {
            if (record == null) continue;
            var upsertOne = new ReplaceOneModel<TEntity>(condition, record)
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

    public bool Update(TEntity entity, string key, CancellationToken ct = default)
    {
        var result = Collection.ReplaceOne(clientSession, x => x.Id == key, entity, new ReplaceOptions
        {
            IsUpsert = false
        }, ct);

        return result.IsAcknowledged;
    }

    public bool UpdateAll(FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> updateDefinition, CancellationToken ct = default)
    {
        var result = Collection.UpdateMany(clientSession, filter, updateDefinition, cancellationToken: ct);

        return result.IsAcknowledged;
    }

    public async Task<bool> UpdateAllAsync(FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> updateDefinition, CancellationToken ct = default)
    {
        var result = await Collection.UpdateManyAsync(clientSession, filter, updateDefinition, cancellationToken: ct);

        return result.IsAcknowledged;
    }

    public async Task<TEntity?> UpdateAsync(TEntity entity, string key, CancellationToken ct = default)
    {
        var resultOk = await Collection.ReplaceOneAsync(clientSession, x => x.Id == key, entity, new ReplaceOptions
        {
            IsUpsert = false
        }, ct);

        return (resultOk.IsAcknowledged) ? entity : null;
    }

    public async Task<TEntity?> UpdateAsync(Expression<Func<TEntity, bool>> condition, TEntity entity, CancellationToken ct = default)
    {
        var resultOk = await Collection.ReplaceOneAsync(clientSession, condition, entity, new ReplaceOptions
        {
            IsUpsert = false
        }, ct);

        return (resultOk.IsAcknowledged) ? entity : null;
    }

    public bool UpdateFields(Expression<Func<TEntity, bool>> condition, UpdateDefinition<TEntity> updateDefinition, CancellationToken ct = default)
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

    public async Task<bool> UpdateFieldsAsync(Expression<Func<TEntity, bool>> condition, UpdateDefinition<TEntity> updateDefinition, CancellationToken ct = default)
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

    public async Task<bool> DeleteAsync(Expression<Func<TEntity, bool>> key, CancellationToken ct = default)
    {
        var result = await Collection.DeleteManyAsync(key, ct);
        return result.IsAcknowledged;
    }
}