namespace Meritum.Infrastructure.Services;

using Meritum.Core.Entities;
using Meritum.Core.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

public class CategoriesService
{
    // Esta variable es la conexión directa a la colección "Categories" en Mongo Atlas
    private readonly IMongoCollection<Category> _categoriesCollection;

    // El constructor recibe la configuración (cadena de conexión)
    public CategoriesService(IOptions<MeritumDatabaseSettings> settings)
    {
        var mongoClient = new MongoClient(settings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(settings.Value.DatabaseName);
        
        // Aquí le decimos: "Tú te encargas de la tabla/colección llamada 'Categories'"
        _categoriesCollection = mongoDatabase.GetCollection<Category>(settings.Value.CategoriesCollectionName);
    }

    // MÉTODOS (Lo que este especialista sabe hacer):

    // 1. Obtener todas las categorías (Para llenar el Dashboard)
    public async Task<List<Category>> GetAllAsync() =>
        await _categoriesCollection.Find(_ => true).ToListAsync();

    // 2. Crear una categoría nueva (Por si haces un panel de administrador luego)
    public async Task CreateAsync(Category newCategory) =>
        await _categoriesCollection.InsertOneAsync(newCategory);
}