namespace Meritum.Infrastructure.Services;

using Meritum.Core.Entities;
using Meritum.Core.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

public class CategoriesService
{
    private readonly IMongoCollection<Category> _categoriesCollection;

    public CategoriesService(IOptions<MeritumDatabaseSettings> settings)
    {
        var mongoClient = new MongoClient(settings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(settings.Value.DatabaseName);
        _categoriesCollection = mongoDatabase.GetCollection<Category>(settings.Value.CategoriesCollectionName);
    }

    // 1. Obtener todas
    public async Task<List<Category>> GetAllAsync() =>
        await _categoriesCollection.Find(_ => true).ToListAsync();

    // 2. Obtener una por ID (Necesario para editar/borrar)
    public async Task<Category?> GetByIdAsync(string id) =>
        await _categoriesCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

    // 3. Obtener por Nombre (PARA EVITAR DUPLICADOS)
    public async Task<Category?> GetByNameAsync(string name) =>
        await _categoriesCollection.Find(x => x.Name == name).FirstOrDefaultAsync();

    // 4. Crear
    public async Task CreateAsync(Category newCategory) =>
        await _categoriesCollection.InsertOneAsync(newCategory);

    // 5. Actualizar 
    public async Task UpdateAsync(string id, Category updatedCategory) =>
        await _categoriesCollection.ReplaceOneAsync(x => x.Id == id, updatedCategory);

    // 6. Eliminar 
    public async Task RemoveAsync(string id) =>
        await _categoriesCollection.DeleteOneAsync(x => x.Id == id);
}