using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CNAS.Repository.Models.Entities;

[Serializable]
[BsonIgnoreExtraElements]
public abstract record BaseEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfDefault]
    public string Id { get; set; } = default!;
}