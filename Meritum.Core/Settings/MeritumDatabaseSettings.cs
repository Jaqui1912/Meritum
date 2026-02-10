
namespace Meritum.Core.Settings;


public class MeritumDatabaseSettings
{
    public string ConnectionString { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;
    public string ProjectsCollectionName { get; set; } = null!;
    public string EvaluationsCollectionName { get; set; } = null!;

    public string CategoriesCollectionName { get; set; } = null!; 
    public string UsersCollectionName { get; set; } = null!;


}