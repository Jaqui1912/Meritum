using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Meritum.Core.Entities;

public class User
{
    // CAMBIO CRÍTICO: El Id ahora es string y tiene los atributos de Mongo
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    public string Email { get; set; }
    public string Role { get; set; } = "Invitado";
}