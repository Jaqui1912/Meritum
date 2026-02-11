using Meritum.Core.Settings;           
using Meritum.Infrastructure.Services; 

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------
// 1. Configuración de Base de Datos (Mongo)
// ---------------------------------------------------------
builder.Services.Configure<MeritumDatabaseSettings>(
    builder.Configuration.GetSection("MeritumDatabase"));

// ---------------------------------------------------------
// 2. Inyección de Servicios (Tus "Especialistas")
// ---------------------------------------------------------
// Singleton es correcto para Mongo (reutiliza la conexión)
builder.Services.AddSingleton<CategoriesService>();
builder.Services.AddSingleton<ProjectsService>();
builder.Services.AddSingleton<EvaluationsService>();
builder.Services.AddSingleton<UsersService>();

// ---------------------------------------------------------
// 3. Configuración de CORS (¡Vital para que el Frontend se conecte!)
// ---------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirTodo",
        policy =>
        {
            policy.AllowAnyOrigin()  // Permite que la App Móvil/Web entre desde cualquier lado
                  .AllowAnyMethod()  // Permite GET, POST, PUT, DELETE
                  .AllowAnyHeader();
        });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ---------------------------------------------------------
// 4. Pipeline de Peticiones HTTP
// ---------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ¡IMPORTANTE! Usa CORS antes de la autorización
app.UseCors("PermitirTodo");

app.UseAuthorization();

app.MapControllers();

app.Run();