

namespace Sample.IncidentBot.Http;

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Entry point for handling call-related web hook requests from the stateful client.
/// </summary>
public class PlatformCallController : ControllerBase
{

    /// <summary>
    /// Handle a callback for an existing call.
    /// </summary>
    [HttpPost]
    [Route(HttpRouteConstants.OnIncomingRequestRoute)]
    public IActionResult OnIncomingRequestAsync()
    {
        // Convert the status code, content of HttpResponseMessage to IActionResult,
        // and copy the headers from response to HttpContext.Response.Headers.
        return Accepted();
    }
}
