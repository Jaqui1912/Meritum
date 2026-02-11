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
        _usersCollection = mongoDatabase.GetCollection<User>(settings.Value.UsersCollectionName);
    }

    // 1. Crear usuario
    public async Task CreateAsync(User newUser) =>
        await _usersCollection.InsertOneAsync(newUser);

    // 2. Buscar por Email (Para Login)
    public async Task<User?> GetByEmailAsync(string email) =>
        await _usersCollection.Find(x => x.Email == email).FirstOrDefaultAsync();

    // 3. Buscar por ID (Para validar al Juez en Evaluaciones y ver Perfil)
    public async Task<User?> GetByIdAsync(string id) =>
        await _usersCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

    // 4. Obtener TODOS (Para el Admin y pruebas)
    public async Task<List<User>> GetAllAsync() =>
        await _usersCollection.Find(_ => true).ToListAsync();
}