using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Meritum.Core.Settings;

namespace Meritum.Infrastructure.Services;

public class EmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task SendVerificationEmailAsync(string toEmail, string userName, string verificationUrl)
    {
        try
        {
            var message = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName ?? "Meritum"),
                Subject = "Verifica tu correo en Meritum",
                IsBodyHtml = true,
                Body = GetVerificationEmailTemplate(userName, verificationUrl)
            };

            message.To.Add(new MailAddress(toEmail));

            using var smtpClient = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
            {
                Credentials = new NetworkCredential(_emailSettings.SmtpUser, _emailSettings.SmtpPass),
                EnableSsl = true
            };

            await smtpClient.SendMailAsync(message);
            _logger.LogInformation("Correo de verificación enviado exitosamente a {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar el correo de verificación a {Email}", toEmail);
            // Dependiendo del requerimiento, podrías lanzar la excepción o simplemente loggearla.
            // Para Meritum, lo asumo como log-only para no frenar completamente el flujo si falla el correo.
        }
    }

    private string GetVerificationEmailTemplate(string userName, string verificationUrl)
    {
        var nameToShow = string.IsNullOrWhiteSpace(userName) ? "Usuario" : userName;
        
        return $@"
        <!DOCTYPE html>
        <html lang=""es"">
        <head>
            <meta charset=""UTF-8"">
            <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
            <title>Verifica tu correo en Meritum</title>
        </head>
        <body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #F8F7F5; color: #1C140D;"">
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background-color: #F8F7F5; padding: 40px 20px;"">
                <tr>
                    <td align=""center"">
                        <table width=""100%"" max-width=""600"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 16px rgba(0,0,0,0.06);"">
                            
                            <!-- Header -->
                            <tr>
                                <td align=""center"" style=""background-color: #F48C25; padding: 40px 20px;"">
                                    <h1 style=""color: #ffffff; margin: 0; font-size: 28px; letter-spacing: 1px;"">Meritum</h1>
                                    <p style=""color: rgba(255,255,255,0.8); margin-top: 5px; font-size: 14px;"">Plataforma de evaluación universitaria</p>
                                </td>
                            </tr>
                            
                            <!-- Body -->
                            <tr>
                                <td align=""center"" style=""padding: 40px 30px;"">
                                    <h2 style=""margin: 0 0 20px; font-size: 22px; color: #1C140D;"">¡Bienvenido(a), {nameToShow}!</h2>
                                    <p style=""margin: 0 0 30px; line-height: 1.6; color: #4b5563; font-size: 16px;"">
                                        Estás a un solo paso de unirte a <strong>Meritum</strong>. Para garantizar la seguridad de nuestra comunidad educativa, necesitamos que confirmes tu correo institucional dando click en el botón de abajo.
                                    </p>
                                    
                                    <!-- Button -->
                                    <a href=""{verificationUrl}"" target=""_blank"" style=""display: inline-block; background-color: #F48C25; color: #ffffff; text-decoration: none; padding: 16px 36px; border-radius: 9999px; font-weight: bold; font-size: 16px; letter-spacing: 0.5px;"">
                                        VERIFICAR MI CORREO
                                    </a>
                                    
                                    <p style=""margin: 30px 0 0; font-size: 13px; color: #9ca3af; text-align: center;"">
                                        Si el botón no funciona, copia y pega el siguiente enlace en tu navegador:<br>
                                        <a href=""{verificationUrl}"" style=""color: #F48C25; word-break: break-all;"">{verificationUrl}</a>
                                    </p>
                                </td>
                            </tr>
                            
                            <!-- Footer -->
                            <tr>
                                <td align=""center"" style=""background-color: #2c241b; padding: 20px;"">
                                    <p style=""margin: 0; font-size: 12px; color: #9ca3af;"">
                                        © 2026 Universidad Tecnológica Metropolitana. Todos los derechos reservados.
                                    </p>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>";
    }
}
