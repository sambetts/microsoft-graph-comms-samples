using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using PstnBot;
using Sample.IncidentBot.Bot;
using Sample.IncidentBot.Data;

namespace IcMBot.Controllers;
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
    public async Task<Call> PostIncidentAsync([FromBody] StartCallData incidentRequestData)
    {

        var call = await this._callingBot.StartP2PCall("+34682796913").ConfigureAwait(false);

        return call;
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
