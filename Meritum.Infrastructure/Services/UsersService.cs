namespace Meritum.Infrastructure.Services;
using Meritum.Core.Entities;
using Meritum.Core.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

public class UsersService
{
    private readonly IMongoCollection<User> _usersCollection;

    public UsersService(IOptions<MeritumDatabaseSettings> settings)
    {
        var mongoClient = new MongoClient(settings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(settings.Value.DatabaseName);
        
        // Asegúrate de agregar "UsersCollectionName" en tu clase Settings y en el JSON
        // O por ahora usa hardcode "Users" para probar rápido:
        _usersCollection = mongoDatabase.GetCollection<User>("Users");
    }

    // Método 1: Buscar por correo (Equivalente a FirstOrDefault)
    public async Task<User?> GetByEmailAsync(string email) =>
        await _usersCollection.Find(x => x.Email == email).FirstOrDefaultAsync();

    // Método 2: Crear usuario nuevo (Equivalente a Add + SaveChanges)
    public async Task CreateAsync(User newUser) =>
        await _usersCollection.InsertOneAsync(newUser);
}