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
    public string? Password { get; set; } // Opcional porque los alumnos no tienen contraseña (Entran con oauth en flutter posiblemente)
    public string Role { get; set; } = "Invitado";
}