using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using import.api.Contracts;

namespace import.api.Services;

/// <summary>
/// Передаёт готовый JSON в management.api. Сам SQL не собирает.
/// </summary>
public sealed class ManagementClient
{
    readonly IConfiguration configuration;

    /// <summary>Берёт адрес management.api из конфигурации.</summary>
    public ManagementClient(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    /// <summary>
    /// Отдаёт сообщение метаданных в POST /admin/metadata.
    /// management.api пишет скрипт таблиц и выполняет его.
    /// </summary>
    public Task<IActionResult> Metadata(string json)
    {
        Console.WriteLine("ManagementClient.Metadata");
        return Post("/admin/metadata", json);
    }

    /// <summary>
    /// Отдаёт пачки строк в POST /admin/data. versionId — номер новой секции.
    /// </summary>
    public Task<IActionResult> Data(string json, int versionId)
    {
        Console.WriteLine($"ManagementClient.Data versionId={versionId}");
        return Post("/admin/data?versionId=" + versionId.ToString(System.Globalization.CultureInfo.InvariantCulture), json);
    }

    async Task<IActionResult> Post(string path, string json)
    {
        var root = configuration["ManagementApi"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(root))
            return new ObjectResult(new ImportErrorResponse { Error = "invalid_request", Detail = "ManagementApi" }) { StatusCode = 500 };
        try
        {
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            using var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
            using var content = new StringContent(json, Encoding.UTF8);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            using var response = await client.PostAsync(root + path, content);
            var body = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"ManagementClient status={(int)response.StatusCode}");
            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = body,
                ContentType = "application/json"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new ObjectResult(new ImportErrorResponse { Error = "script_failed", Detail = ex.Message }) { StatusCode = 500 };
        }
    }
}
