using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Graph.Communications.Client;
using Sample.IncidentBot.Bot;


namespace Sample.IncidentBot.Http;

/// <summary>
/// Entry point for handling call-related web hook requests from the stateful client.
/// </summary>
public class PlatformCallController : ControllerBase
{
    private readonly CallingBot _callingBot;

    public PlatformCallController(CallingBot callingBot)
    {
        _callingBot = callingBot;
    }

    /// <summary>
    /// Handle a callback for an existing call.
    /// </summary>
    [HttpPost]
    [Route(HttpRouteConstants.OnIncomingRequestRoute)]
    public async Task<IActionResult> OnIncomingRequestAsync()
    {
        // Pass the incoming message to the sdk. The sdk takes care of what to do with it.
        var response = await _callingBot.Client.ProcessNotificationAsync(Request.ToHttpRequestMessage()).ConfigureAwait(false);
        return await this.GetActionResultAsync(response);
    }
}


public static class RequestTranscriptHelpers
{
    public static HttpRequestMessage ToHttpRequestMessage(this HttpRequest req)
        => new HttpRequestMessage()
            .SetMethod(req)
            .SetAbsoluteUri(req)
            .SetHeaders(req)
            .SetContent(req)
            .SetContentType(req);

    private static HttpRequestMessage SetAbsoluteUri(this HttpRequestMessage msg, HttpRequest req)
        => msg.Set(m => m.RequestUri = new UriBuilder
        {
            Scheme = req.Scheme,
            Host = req.Host.Host,
            Port = req.Host.Port.HasValue ? req.Host.Port.Value : throw new ArgumentNullException(nameof(req.Host.Port)),
            Path = req.PathBase.Add(req.Path),
            Query = req.QueryString.ToString()
        }.Uri);

    private static HttpRequestMessage SetMethod(this HttpRequestMessage msg, HttpRequest req)
        => msg.Set(m => m.Method = new HttpMethod(req.Method));

    private static HttpRequestMessage SetHeaders(this HttpRequestMessage msg, HttpRequest req)
        => req.Headers.Aggregate(msg, (acc, h) => acc.Set(m => m.Headers.TryAddWithoutValidation(h.Key, h.Value.AsEnumerable())));

    private static HttpRequestMessage SetContent(this HttpRequestMessage msg, HttpRequest req)
        => msg.Set(m => m.Content = new StreamContent(req.Body));

    private static HttpRequestMessage SetContentType(this HttpRequestMessage msg, HttpRequest req)
        => msg.Set(m => m.Content.Headers.Add("Content-Type", req.ContentType), applyIf: req.Headers.ContainsKey("Content-Type"));

    private static HttpRequestMessage Set(this HttpRequestMessage msg, Action<HttpRequestMessage> config, bool applyIf = true)
    {
        if (applyIf)
        {
            config.Invoke(msg);
        }

        return msg;
    }
}
