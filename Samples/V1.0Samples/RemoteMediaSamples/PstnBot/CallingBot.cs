using System.Text.Json;
using Microsoft.Graph.Communications.Client.Authentication;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Common.Transport;
using Microsoft.Graph.Communications.Core.Serialization;
using PstnBot;
using ServiceHostedMediaBot.Authentication;
using Microsoft.Graph;
using ServiceHostedMediaBot.Transport;
using Microsoft.Graph.Communications.Client.Transport;
using System.Net.Http.Headers;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Resources;
using System.Collections.Concurrent;

namespace Sample.IncidentBot.Bot;
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

    private readonly BotOptions _botOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="CallingBot" /> class.
    /// </summary>
    /// <param name="options">The bot options.</param>
    /// <param name="graphLogger">The graph logger.</param>
    public CallingBot(BotOptions botOptions, IGraphLogger graphLogger)
    {
        _botOptions = botOptions;
        GraphLogger = graphLogger;
        _callbackUri = _botOptions.BotBaseUrl + HttpRouteConstants.OnIncomingRequestRoute;

        var name = this.GetType().Assembly.GetName().Name ?? "CallingBot";
        this.AuthenticationProvider = new AuthenticationProvider(name, _botOptions.AppId, _botOptions.AppSecret, graphLogger);
        this.Serializer = new CommsSerializer();

        var authenticationWrapper = new AuthenticationWrapper(this.AuthenticationProvider);
        this.RequestBuilder = new Microsoft.Graph.GraphServiceClient("https://graph.microsoft.com/v1.0", authenticationWrapper);

        this.MediaMap[NotificationPromptName] = new Microsoft.Graph.MediaPrompt
        {
            MediaInfo = new Microsoft.Graph.MediaInfo
            {
                Uri = new Uri(botOptions.BotBaseUrl + "/audio/responder-notification.wav").ToString(),
                ResourceId = Guid.NewGuid().ToString(),
            },
        };

        var defaultProperties = new List<IGraphProperty<IEnumerable<string>>>();
        using (HttpClient tempClient = GraphClientFactory.Create(authenticationWrapper))
        {
            defaultProperties.AddRange(tempClient.DefaultRequestHeaders.Select(header => GraphProperty.RequestProperty(header.Key, header.Value)));
        }

        var productInfo = new ProductInfoHeaderValue(
            typeof(CallingBot).Assembly.GetName().Name!,
            typeof(CallingBot).Assembly.GetName().Version?.ToString());
        this.GraphApiClient = new GraphAuthClient(
            this.GraphLogger,
            this.Serializer.JsonSerializerSettings,
            new HttpClient(),
            this.AuthenticationProvider,
            productInfo,
            defaultProperties);

        var builder = new CommunicationsClientBuilder(
                name,
                botOptions.AppId,
                graphLogger);


        builder.SetAuthenticationProvider(this.AuthenticationProvider);
        builder.SetNotificationUrl(new Uri(_callbackUri));
        builder.SetServiceBaseUrl(new Uri(_callbackUri));

        this.Client = builder.Build();
        this.Client.Calls().OnUpdated += this.CallsOnUpdated;
    }
    public IGraphLogger GraphLogger { get; set; }
    public ICommunicationsClient Client { get; }

    public IRequestAuthenticationProvider AuthenticationProvider { get; }

    public CommsSerializer Serializer { get; }

    public IGraphClient GraphApiClient { get; }

    /// <summary>
    /// Gets the prompts dictionary.
    /// </summary>
    public Dictionary<string, Microsoft.Graph.MediaPrompt> MediaMap { get; } = new();

    public ConcurrentDictionary<string, CallHandler> CallHandlers { get; } = new ConcurrentDictionary<string, CallHandler>();
    private readonly string _callbackUri;

    public GraphServiceClient RequestBuilder { get; }



    /// <summary>
    /// Updated call handler.
    /// </summary>
    /// <param name="sender">The <see cref="ICallCollection"/> sender.</param>
    /// <param name="args">The <see cref="CollectionEventArgs{ICall}"/> instance containing the event data.</param>
    private void CallsOnUpdated(ICallCollection sender, CollectionEventArgs<ICall> args)
    {
        foreach (var call in args.RemovedResources)
        {
            if (this.CallHandlers.TryRemove(call.Id, out CallHandler handler))
            {
                handler.Dispose();
            }
        }
    }

    /// <summary>
    /// Raise an incident.
    /// </summary>
    /// <param name="incidentRequestData">The incident data.</param>
    /// <returns>The task for await.</returns>
    public async Task<Call> StartP2PCall(string phoneNumber)
    {

        var scenarioId = Guid.NewGuid();
        var target = new IdentitySet();
        target.SetPhone(
            new Identity
            {
                Id = phoneNumber,
                DisplayName = phoneNumber
            });

        var mediaToPrefetch = new List<Microsoft.Graph.MediaInfo>();
        foreach (var m in this.MediaMap)
        {
            mediaToPrefetch.Add(m.Value.MediaInfo);
        }

        var newCall = new Call
        {
            Targets = new List<InvitationParticipantInfo>()
                {
                    new InvitationParticipantInfo
                    {
                        Identity = target
                    },
                },
            MediaConfig = new ServiceHostedMediaConfig { PreFetchMedia = mediaToPrefetch },
            RequestedModalities = new List<Modality> { Modality.Audio },
            TenantId = _botOptions.TenantId,
            CallbackUri = _callbackUri,
            Direction = CallDirection.Outgoing,
            Source = new ParticipantInfo
            {
                Identity = new IdentitySet
                {
                    Application = new Identity { Id = _botOptions.AppId },
                },
            }
        };

        newCall.Source.Identity.SetApplicationInstance(
            new Identity
            {
                Id = _botOptions.AppInstanceObjectId,
                DisplayName = _botOptions.AppInstanceObjectName,
            });

        var opts = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var jsonPayload = System.Text.Json.JsonSerializer.Serialize(newCall, opts);

        var callRequest = this.RequestBuilder.Communications.Calls;
        var request = new GraphRequest<Call>(new Uri(callRequest.RequestUrl), newCall, RequestType.Create);
        var r = await this.GraphApiClient.SendAsync<Call, Call>(request, newCall.TenantId, scenarioId).ConfigureAwait(false);

        return r.Content;
    }

}
