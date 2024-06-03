# CNAS.Repository

## Who
I am a software engineer and I like building custom solutions that fit my needs.

But as they are **not** professional solutions, i manually include my libraries over and over.

This is not only in conflict with the DRY principle, but also a waste of time.

## Why
To solve this *problem* i'm creating a set of libraries:
1. CNAS.Presentation
2. CNAS.Business
3. CNAS.Repository

## What
CNAS is an achronym for **C**lean .**N**ET **A**PI **S**ervice.

With the Repository library you can create a clean data access layer, following the repository pattern.

The idea is to create a nuget package for this library, to make it fast and easy to include in new projects.

The nuget package will be publicly deployed on the nuget store.

## How

### Install
1. Search for "CNAS.Repository" in the nuget store
2. Install said nuget

### Usage

#### Set the connection string in AppSettings.json
``` json
{
    "ConnectionStrings": {
        "MongoDb": "mongodb://localhost:27017/MyDatabase"
    }
}
``` 

#### Register in Program.cs

``` c#
using CNAS.Repository.Extensions;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("MongoDb")!;

// Add MongoDb
// "true" is for logging queries in console. Leave empty or "false" if you don't want to log queries.
builder.Services.AddMongoDb(connectionString, true);

// Add all your repositories
builder.Services.AddRepositories();
```

#### Create a repository

You don't have to create a Repository class for each repository you want to handle.
Just require the `IRepository<TEntity>` from the DI.

Of course, you can still create a Repository class if you want custom methods that are not in the library.
All your repositories must inherit from `Repository<TEntity>`.

In both vases, `TEntity` must be a type that inherits from `BaseEntity`.

``` c#
using CNAS.Repository.Extensions;
using MongoDb.Bson;
using MongoDb.Bson.Serialization.Attributes;
using CNAS.Repository.Models.Entities;

[BsonIgnoreExtraElements]
public sealed record Student : BaseEntity {
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required int Age { get; init; }
}
```

``` c#
public sealed class StudentService : IStudentService {
    
    // ...
    
    public StudentService(ILogger<StudentService> logger, IRepository<Student> studentRepo){
        // ...
    }

    // ...
}
```