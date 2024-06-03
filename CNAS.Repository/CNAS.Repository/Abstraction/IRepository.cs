﻿using CNAS.Repository.Models.Entities;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Linq.Expressions;

namespace SpRepo.Abstraction;

public interface IRepository<T> where T : BaseEntity
{
    T? GetLastAdded();

    IMongoQueryable<T> GetAllAsQueryable();

    IMongoQueryable<T> FindAllAsQueryable(Expression<Func<T, bool>> filter);

    Task<List<T>> GetAllAsync(CancellationToken ct = default);

    List<T> GetAll(CancellationToken ct = default);

    Task<T?> FindByIdAsync(string id, CancellationToken ct = default);

    T? FindById(string id, CancellationToken ct = default);

    Task<List<T>> FindAllAsync(Expression<Func<T, bool>> filter, CancellationToken ct = default);

    Task<List<T>> FindAllAsync(FilterDefinition<T> filter, CancellationToken ct = default);

    List<T> FindAll(Expression<Func<T, bool>> filter, CancellationToken ct = default);

    List<T> FindAll(FilterDefinition<T> filter, CancellationToken ct = default);

    Task<T?> FindAsync(Expression<Func<T, bool>> filter, CancellationToken ct = default);

    T? Find(Expression<Func<T, bool>> filter, CancellationToken ct = default);

    Task<bool> Exists(string id, CancellationToken ct = default);

    Task<bool> Exists(Expression<Func<T, bool>> filter, CancellationToken ct = default);

    long Count(Expression<Func<T, bool>> filter, CancellationToken ct = default);

    Task<long> CountAsync(Expression<Func<T, bool>> filter, CancellationToken ct = default);

    void CopyFrom(string fromCollection, RenameCollectionOptions? options = null, string? newCollectionName = null, CancellationToken ct = default);

    void Drop(string? newcollectionName = null, CancellationToken ct = default);

    void Add(T t, InsertOneOptions? options = null, CancellationToken ct = default);

    Task AddAsync(T t, InsertOneOptions? options = null, CancellationToken ct = default);

    void AddAll(IEnumerable<T> t, InsertManyOptions? options = null, CancellationToken ct = default);

    Task AddAllAsync(IEnumerable<T> t, InsertManyOptions? options = null, CancellationToken ct = default);

    T? AddOrUpdate(Expression<Func<T, bool>> condition, T entity, CancellationToken ct = default);

    Task<T?> AddOrUpdateAsync(Expression<Func<T, bool>> condition, T entity, CancellationToken ct = default);

    long BulkAddOrUpdate(Expression<Func<T, bool>> condition, IEnumerable<T?> records, BulkWriteOptions? options = null, CancellationToken ct = default);

    Task<long> BulkAddOrUpdateAsync(Expression<Func<T, bool>> condition, IEnumerable<T?> records, BulkWriteOptions? options = null, CancellationToken ct = default);

    long BulkInsert(Expression<Func<T, bool>> condition, IEnumerable<T?> records, BulkWriteOptions? options = null, CancellationToken ct = default);

    Task<long> BulkInsertAsync(Expression<Func<T, bool>> condition, IEnumerable<T?> records, BulkWriteOptions? options = null, CancellationToken ct = default);

    bool Update(T updated, string key, CancellationToken ct = default);

    bool UpdateAll(FilterDefinition<T> filter, UpdateDefinition<T> updateDefinition, CancellationToken ct = default);

    Task<bool> UpdateAllAsync(FilterDefinition<T> filter, UpdateDefinition<T> updateDefinition, CancellationToken ct = default);

    Task<T?> UpdateAsync(T updated, string key, CancellationToken ct = default);

    Task<T?> UpdateAsync(Expression<Func<T, bool>> condition, T entity, CancellationToken ct = default);

    bool UpdateFields(Expression<Func<T, bool>> condition, UpdateDefinition<T> updateDefinition, CancellationToken ct = default);

    Task<bool> UpdateFieldsAsync(Expression<Func<T, bool>> condition, UpdateDefinition<T> updateDefinition, CancellationToken ct = default);

    Task<bool> DeleteAsync(string key, CancellationToken ct = default);

    Task<bool> DeleteAsync(Expression<Func<T, bool>> condition, CancellationToken ct = default);
}