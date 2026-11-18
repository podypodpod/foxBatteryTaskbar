using System.Net;
using System.Text;

namespace FoxBatteryTaskbar.Tests;

/// <summary>Returns canned responses and records requests, so tests never touch the network.</summary>
sealed class FakeHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<(HttpRequestMessage Request, string Body)> Requests { get; } = [];

    public static FakeHttpHandler Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(_ => new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") });

    public static FakeHttpHandler Sample(string fileName) =>
        Json(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Samples", fileName)));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct);
        Requests.Add((request, body));
        return respond(request);
    }
}
