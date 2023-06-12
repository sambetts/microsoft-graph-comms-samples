
namespace IcMBot.Controllers;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Sample.IncidentBot;
using Sample.IncidentBot.Bot;
using Sample.IncidentBot.Data;

/// <summary>
/// The incidents controller class.
/// </summary>
[Route("[controller]")]
public class IncidentsController : Controller
{
    private readonly CallingBot _callingBot;

    public IncidentsController(CallingBot callingBot)
    {
        _callingBot = callingBot;
    }

    /// <summary>
    /// Raise a incident.
    /// </summary>
    /// <param name="incidentRequestData">The incident data.</param>
    /// <returns>The action result.</returns>
    [HttpPost("raise")]
    public async Task<IActionResult> PostIncidentAsync([FromBody] StartCallData incidentRequestData)
    {

        var call = await this._callingBot.StartP2PCall(incidentRequestData.PhoneNumber).ConfigureAwait(false);

        return this.Ok();
    }


    /// <summary>
    /// Add refresh headers for browsers to download content.
    /// </summary>
    /// <param name="seconds">Refresh rate.</param>
    private void AddRefreshHeader(int seconds)
    {
        this.Response.Headers.Add("Cache-Control", "private,must-revalidate,post-check=1,pre-check=2,no-cache");
        this.Response.Headers.Add("Refresh", seconds.ToString());
    }
}
