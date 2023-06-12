
namespace Sample.IncidentBot.Bot;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PstnBot;
using Sample.IncidentBot.Data;

/// <summary>
/// The core bot logic.
/// </summary>
public class CallingBot
{
    /// <summary>
    /// The prompt audio name for responder notification.
    /// </summary>
    /// <remarks>
    /// message: "There is an incident occured. Press '1' to join the incident meeting. Press '0' to listen to the instruction again. ".
    /// </remarks>
    public const string NotificationPromptName = "NotificationPrompt";

    /// <summary>
    /// The prompt audio name for responder transfering.
    /// </summary>
    /// <remarks>
    /// message: "Your call will be transferred to the incident meeting. Please don't hang off. ".
    /// </remarks>
    public const string TransferingPromptName = "TransferingPrompt";

    /// <summary>
    /// The prompt audio name for bot incoming calls.
    /// </summary>
    /// <remarks>
    /// message: "You are calling an incident application. It's a sample for incoming call with audio prompt.".
    /// </remarks>
    public const string BotIncomingPromptName = "BotIncomingPrompt";

    /// <summary>
    /// The prompt audio name for bot endpoint incoming calls.
    /// </summary>
    /// <remarks>
    /// message: "You are calling an incident application endpoint. It's a sample for incoming call with audio prompt.".
    /// </remarks>
    public const string BotEndpointIncomingPromptName = "BotEndpointIncomingPrompt";


    private readonly LinkedList<string> callbackLogs = new LinkedList<string>();
    private readonly string _tenantId;

    /// <summary>
    /// Initializes a new instance of the <see cref="CallingBot" /> class.
    /// </summary>
    /// <param name="options">The bot options.</param>
    /// <param name="graphLogger">The graph logger.</param>
    public CallingBot(string botBaseUrl, string tenantId)
    {
        this.BotInstanceUri = botBaseUrl;
        _tenantId = tenantId;
        var instanceNotificationUri = botBaseUrl + HttpRouteConstants.OnIncomingRequestRoute;

        var name = this.GetType().Assembly.GetName().Name;
        
        this.MediaMap[TransferingPromptName] = new Microsoft.Graph.MediaPrompt
        {
            MediaInfo = new Microsoft.Graph.MediaInfo
            {
                Uri = new Uri(botBaseUrl + "audio/responder-transfering.wav").ToString(),
                ResourceId = Guid.NewGuid().ToString(),
            },
        };

        this.MediaMap[NotificationPromptName] = new Microsoft.Graph.MediaPrompt
        {
            MediaInfo = new Microsoft.Graph.MediaInfo
            {
                Uri = new Uri(botBaseUrl + "audio/responder-notification.wav").ToString(),
                ResourceId = Guid.NewGuid().ToString(),
            },
        };

        this.MediaMap[BotIncomingPromptName] = new Microsoft.Graph.MediaPrompt
        {
            MediaInfo = new Microsoft.Graph.MediaInfo
            {
                Uri = new Uri(botBaseUrl + "audio/bot-incoming.wav").ToString(),
                ResourceId = Guid.NewGuid().ToString(),
            },
        };

        this.MediaMap[BotEndpointIncomingPromptName] = new Microsoft.Graph.MediaPrompt
        {
            MediaInfo = new Microsoft.Graph.MediaInfo
            {
                Uri = new Uri(botBaseUrl + "audio/bot-endpoint-incoming.wav").ToString(),
                ResourceId = Guid.NewGuid().ToString(),
            },
        };
    }

    /// <summary>
    /// Gets the prompts dictionary.
    /// </summary>
    public Dictionary<string, Microsoft.Graph.MediaPrompt> MediaMap { get; } = new();

    /// <summary>
    /// Gets the bots instance URI.
    /// </summary>
    public string BotInstanceUri { get; }

    /// <summary>
    /// Raise an incident.
    /// </summary>
    /// <param name="incidentRequestData">The incident data.</param>
    /// <returns>The task for await.</returns>
    public async Task<PstnBot.Call> StartP2PCall(string phoneNumber)
    {
        var target =
            new ParticipantInfo
            {
                Identity = new IdentitySet
                {
                    Phone = new Identity
                    {
                        Id = phoneNumber,
                    },
                },
            };

        var mediaToPrefetch = new List<Microsoft.Graph.MediaInfo>();
        foreach (var m in this.MediaMap)
        {
            mediaToPrefetch.Add(m.Value.MediaInfo);
        }

        var call = new Call
        {
            Targets = new List<ParticipantInfo> { target },
            MediaConfig = new MediaConfig { PreFetchMedia = mediaToPrefetch },
            RequestedModalities = new List<string> { "audio" },
            TenantId = _tenantId,
        };

        return call;   
    }
}
