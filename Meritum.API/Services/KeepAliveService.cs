using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Meritum.API.Services
{
    public class KeepAliveService : BackgroundService
    {
        private readonly ILogger<KeepAliveService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string? _urlToPing;

        public KeepAliveService(ILogger<KeepAliveService> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("KeepAliveClient");
            
            // Render inyecta automáticamente esta variable con la URL pública de la app.
            // Si la pruebas localmente, será null y no hará pings innecesarios a menos que la definas.
            _urlToPing = Environment.GetEnvironmentVariable("RENDER_EXTERNAL_URL");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (string.IsNullOrEmpty(_urlToPing))
            {
                _logger.LogInformation("KeepAliveService: No se encontró la variable RENDER_EXTERNAL_URL. El servicio de Keep-Alive no se ejecutará.");
                return;
            }

            var pingUrl = $"{_urlToPing.TrimEnd('/')}/ping";

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("KeepAliveService: Ejecutando ping a {Url} a las {Time}", pingUrl, DateTimeOffset.Now);
                    var response = await _httpClient.GetAsync(pingUrl, stoppingToken);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("KeepAliveService: Ping exitoso. Status: {StatusCode}", response.StatusCode);
                    }
                    else
                    {
                        _logger.LogWarning("KeepAliveService: El ping falló. Status: {StatusCode}", response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "KeepAliveService: Ocurrió un error al intentar hacer ping.");
                }

                // Esperar 10 minutos antes del próximo ping (Render se apaga tras 15 min de inactividad)
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }
    }
}
