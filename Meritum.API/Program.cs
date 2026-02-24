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
builder.Services.AddScoped<Meritum.Infrastructure.Services.FileStorageService>();
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
                  .AllowAnyHeader(); // Permite cualquier tipo de dato
        });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient("KeepAliveClient");
builder.Services.AddHostedService<Meritum.API.Services.KeepAliveService>();

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        // 1. Decimos que hubo un error de servidor (500)
        context.Response.StatusCode = 500;
        // 2. Le decimos a la app móvil que le vamos a responder en formato JSON
        context.Response.ContentType = "application/json";

        // 3. El mensaje bonito y robusto que la app móvil sí puede entender
        var errorResponse = new
        {
            error = true,
            message = "¡Ups! Ocurrió un problema interno en el servidor de Meritum. Por favor, intenta de nuevo más tarde."
        };

        await context.Response.WriteAsJsonAsync(errorResponse);
    });
});


// 4. Peticiones HTTP

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseStaticFiles();// Para descarga de los videos/documentos


app.UseCors("PermitirTodo");

app.UseAuthorization();

// Endpoint super sencillo para responder al Keep Alive
app.MapGet("/ping", () => Results.Ok("pong"));

app.MapControllers();

app.Run();
