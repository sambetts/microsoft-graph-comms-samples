using Microsoft.AspNetCore.Mvc;
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
    public async Task OnIncomingRequestAsync()
    {
        await _callingBot.ProcessNotificationAsync(this.Request, this.Response).ConfigureAwait(false);
    }
}
