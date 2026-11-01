using System.Net.Http.Json;

namespace ZoneClan.Web.Features.Shared;

public static class ApiClient
{
    public static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        using (response)
        {
            await EnsureAsync(response);
            return (await response.Content.ReadFromJsonAsync<T>())!;
        }
    }

    public static async Task EnsureAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        string message;
        try
        {
            var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
            message = string.IsNullOrWhiteSpace(body?.Error)
                ? $"Сервер ответил {(int)response.StatusCode}"
                : body.Error;
        }
        catch
        {
            message = $"Сервер ответил {(int)response.StatusCode}";
        }

        throw new InvalidOperationException(message);
    }

    private sealed class ErrorBody
    {
        public string Error { get; set; } = "";
    }
}
