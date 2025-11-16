using Azure.Communication.Email;
using Microsoft.Extensions.Options;
using ContestApi.Services;
using Azure;

namespace ContestApi.Services
{


    public class AcsEmailService
    {
        private readonly EmailClient _client;
        private readonly EmailOptions _opts;
        private readonly ILogger<AcsEmailService>? _logger;

        public AcsEmailService(IOptions<EmailOptions> opts, ILogger<AcsEmailService>? logger = null)
        {
            _opts = opts.Value;
            _client = new EmailClient(_opts.AcsConnectionString ?? throw new InvalidOperationException("Missing ACS connection string."));
            _logger = logger;
        }

        public Task SendSubmissionConfirmationAsync(
             string toEmail,
             string toName,
             Guid submissionId,
             CancellationToken ct = default)
        {
            // Run asynchronously in background
            _ = Task.Run(async () =>
            {
                var subject = "Thanks for entering the contest!";
                var htmlBody = $@"
                                <!DOCTYPE html>
                                <html lang='en'>
                                <head>
                                    <meta charset='UTF-8'>
                                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                                    <title>{subject}</title>
                                    <style>
                                        body {{
                                            font-family: 'Segoe UI', Arial, sans-serif;
                                            background-color: #f4f4f4;
                                            color: #333333;
                                            padding: 0;
                                            margin: 0;
                                        }}
                                        .container {{
                                            max-width: 600px;
                                            margin: 40px auto;
                                            background-color: #ffffff;
                                            border-radius: 12px;
                                            overflow: hidden;
                                            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
                                        }}
                                        .header {{
                                            background-color: #006241; /* Starbucks green */
                                            color: white;
                                            padding: 24px;
                                            text-align: center;
                                        }}
                                        .header img {{
                                            max-width: 120px;
                                            margin-bottom: 12px;
                                        }}
                                        .content {{
                                            padding: 24px;
                                            font-size: 16px;
                                            line-height: 1.5;
                                        }}
                                        .cta {{
                                            display: inline-block;
                                            margin-top: 20px;
                                            background-color: #006241;
                                            color: white !important;
                                            text-decoration: none;
                                            padding: 10px 24px;
                                            border-radius: 6px;
                                            font-weight: 600;
                                        }}
                                        .footer {{
                                            text-align: center;
                                            font-size: 12px;
                                            color: #777777;
                                            padding: 16px;
                                            border-top: 1px solid #eaeaea;
                                        }}
                                    </style>
                                </head>
                                <body>
                                    <div class='container'>
                                        <div class='header'>
                                            <img src='https://upload.wikimedia.org/wikipedia/en/4/45/Starbucks_Coffee_Logo.svg' alt='Starbucks Logo'>
                                            <h1>Gana con Starbucks ☕</h1>
                                        </div>
                                        <div class='content'>
                                            <p>Hola <strong>{System.Net.WebUtility.HtmlEncode(toName)}</strong>,</p>
                                            <p>Gracias por participar en nuestro concurso <strong>Gana con Starbucks</strong>.</p>
                                            <p>Tu número de confirmación es:</p>
                                            <p style='font-size: 22px; font-weight: bold; color: #006241;'>{submissionId}</p>
                                            <p>Nos encanta tenerte con nosotros. Mantente pendiente de nuestras redes sociales para conocer a los ganadores.</p>
                                            <a href='https://ganaconstarbucks.com' class='cta'>Visita el sitio del concurso</a>
                                        </div>
                                        <div class='footer'>
                                            © {DateTime.UtcNow.Year} Starbucks Puerto Rico.<br>
                                            Este correo fue enviado automáticamente, por favor no respondas a este mensaje.
                                        </div>
                                    </div>
                                </body>
                                </html>";

                var plainText = $"Hola {toName}, gracias por participar en Gana con Starbucks. " +
                                $"Tu número de confirmación es {submissionId}. ¡Buena suerte!";
                try
                {
                    var content = new EmailContent(subject)
                    {
                        Html = htmlBody,
                        PlainText = plainText
                    };

                    var recipients = new EmailRecipients(new List<EmailAddress>
                    {
                        new EmailAddress(toEmail, toName)
                    });

                    var message = new EmailMessage(_opts.FromAddress, recipients, content);

                    EmailSendOperation op = await _client.SendAsync(WaitUntil.Completed, message, ct);
                    EmailSendResult result = op.Value;

                    _logger?.LogInformation(
                        "Email sent to {Email}. OpId={OpId}, Status={Status}",
                        toEmail,
                        op.Id,
                        result.Status);
                }
                catch (RequestFailedException rfe)
                {
                    _logger?.LogError(rfe, "ACS email failed. Code={Code}, Status={Status}", rfe.ErrorCode, rfe.Status);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Unexpected failure sending ACS email to {Email}", toEmail);
                }
            }, ct);

            // Fire and forget, so return immediately
            return Task.CompletedTask;
        }
    }
}
