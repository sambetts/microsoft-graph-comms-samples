using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;
using Microsoft.Graph.Communications.Client.Authentication;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Common.Transport;
using Microsoft.Graph.Communications.Core.Notifications;
using Microsoft.Graph.Communications.Core.Serialization;
using PstnBot;
using ServiceHostedMediaBot.Extensions;
using ServiceHostedMediaBot.Authentication;
using Microsoft.Graph.Communications.Common;
using ServiceHostedMediaBot.Common;
using Microsoft.Graph;
using ServiceHostedMediaBot.Transport;
using Newtonsoft.Json;
using Microsoft.Graph.Communications.Client.Transport;
using System.Net.Http.Headers;
using Microsoft.Graph.Communications.Client;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Communications.Calls;

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
        this.NotificationProcessor = new NotificationProcessor(authenticationWrapper, this.Serializer);
        this.NotificationProcessor.OnNotificationReceived += this.NotificationProcessor_OnNotificationReceived;
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
        this.Client.Calls().OnIncoming += this.CallsOnIncoming;
        this.Client.Calls().OnUpdated += this.CallsOnUpdated;
    }
    public IGraphLogger GraphLogger { get; set; }
    public ICommunicationsClient Client { get; }

    public IRequestAuthenticationProvider AuthenticationProvider { get; }

    public INotificationProcessor NotificationProcessor { get; }


    public CommsSerializer Serializer { get; }

    public IGraphClient GraphApiClient { get; }

    /// <summary>
    /// Gets the prompts dictionary.
    /// </summary>
    public Dictionary<string, Microsoft.Graph.MediaPrompt> MediaMap { get; } = new();

    private readonly string _callbackUri;

    public GraphServiceClient RequestBuilder { get; }

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


    public async Task ProcessNotificationAsync(
        HttpRequest request,
        HttpResponse response)
    {
        var headers = request.Headers.Select(
            pair => new KeyValuePair<string, IEnumerable<string>>(pair.Key, pair.Value));

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        this.GraphLogger.LogHttpMessage(
            TraceLevel.Verbose,
            TransactionDirection.Incoming,
            HttpTraceType.HttpRequest,
            request.GetDisplayUrl(),
            request.Method,
            obfuscatedContent: null,
            headers: headers);

        try
        {
            var httpRequest = request.CreateRequestMessage();
            var results = await this.AuthenticationProvider.ValidateInboundRequestAsync(httpRequest).ConfigureAwait(false);
            if (results.IsValid)
            {
                var httpResponse = await this.NotificationProcessor.ProcessNotificationAsync(httpRequest).ConfigureAwait(false);
                await httpResponse.CreateHttpResponseAsync(response).ConfigureAwait(false);
            }
            else
            {
                // This way is not working. Demands further investigation
                //var httpResponse = httpRequest.CreateResponse(HttpStatusCode.Forbidden);
                var httpResponse = new HttpResponseMessage(HttpStatusCode.Forbidden);
                await httpResponse.CreateHttpResponseAsync(response).ConfigureAwait(false);
            }

            headers = response.Headers.Select(
                pair => new KeyValuePair<string, IEnumerable<string>>(pair.Key, pair.Value));

            this.GraphLogger.LogHttpMessage(
                TraceLevel.Verbose,
                TransactionDirection.Incoming,
                HttpTraceType.HttpResponse,
                request.GetDisplayUrl(),
                request.Method,
                obfuscatedContent: null,
                headers: headers,
                responseCode: response.StatusCode,
                responseTime: stopwatch.ElapsedMilliseconds);
        }
        catch (Microsoft.Graph.ServiceException e)
        {
            string obfuscatedContent = null;
            if ((int)e.StatusCode >= 300)
            {
                response.StatusCode = (int)e.StatusCode;
                await response.WriteAsync(e.ToString()).ConfigureAwait(false);
                obfuscatedContent = this.GraphLogger.SerializeAndObfuscate(e, Formatting.Indented);
            }
            else if ((int)e.StatusCode >= 200)
            {
                response.StatusCode = (int)e.StatusCode;
            }
            else
            {
                response.StatusCode = (int)e.StatusCode;
                await response.WriteAsync(e.ToString()).ConfigureAwait(false);
                obfuscatedContent = this.GraphLogger.SerializeAndObfuscate(e, Formatting.Indented);
            }

            headers = response.Headers.Select(
                pair => new KeyValuePair<string, IEnumerable<string>>(pair.Key, pair.Value));

            if (e.ResponseHeaders?.Any() == true)
            {
                foreach (var pair in e.ResponseHeaders)
                {
                    response.Headers.Add(pair.Key, new StringValues(pair.Value.ToArray()));
                }

                headers = headers.Concat(e.ResponseHeaders);
            }

            this.GraphLogger.LogHttpMessage(
                TraceLevel.Error,
                TransactionDirection.Incoming,
                HttpTraceType.HttpResponse,
                request.GetDisplayUrl(),
                request.Method,
                obfuscatedContent,
                headers,
                response.StatusCode,
                responseTime: stopwatch.ElapsedMilliseconds);
        }
        catch (Exception e)
        {
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await response.WriteAsync(e.ToString()).ConfigureAwait(false);

            var obfuscatedContent = this.GraphLogger.SerializeAndObfuscate(e, Formatting.Indented);
            headers = response.Headers.Select(
                pair => new KeyValuePair<string, IEnumerable<string>>(pair.Key, pair.Value));

            this.GraphLogger.LogHttpMessage(
               TraceLevel.Error,
               TransactionDirection.Incoming,
               HttpTraceType.HttpResponse,
               request.GetDisplayUrl(),
               request.Method,
               obfuscatedContent,
               headers,
               response.StatusCode,
               responseTime: stopwatch.ElapsedMilliseconds);
        }
    }

    private void NotificationProcessor_OnNotificationReceived(NotificationEventArgs args)
    {
        this.NotificationProcessor_OnNotificationReceivedAsync(args).ForgetAndLogExceptionAsync(
            this.GraphLogger,
            $"Error processing notification {args.Notification.ResourceUrl} with scenario {args.ScenarioId}");
    }

    private async Task NotificationProcessor_OnNotificationReceivedAsync(NotificationEventArgs args)
    {
        this.GraphLogger.CorrelationId = args.ScenarioId;
        var headers = new[]
        {
                new KeyValuePair<string, IEnumerable<string>>(HttpConstants.HeaderNames.ScenarioId, new[] {args.ScenarioId.ToString() }),
                new KeyValuePair<string, IEnumerable<string>>(HttpConstants.HeaderNames.ClientRequestId, new[] {args.RequestId.ToString() }),
                new KeyValuePair<string, IEnumerable<string>>(HttpConstants.HeaderNames.Tenant, new[] {args.TenantId }),
            };

        var notifications = new CommsNotifications { Value = new[] { args.Notification } };
        var obfuscatedContent = this.GraphLogger.SerializeAndObfuscate(notifications, Formatting.Indented);
        this.GraphLogger.LogHttpMessage(
            TraceLevel.Info,
            TransactionDirection.Incoming,
            HttpTraceType.HttpRequest,
            args.CallbackUri.ToString(),
            Microsoft.AspNetCore.Http.HttpMethods.Post,
            obfuscatedContent,
            headers,
            correlationId: args.ScenarioId,
            requestId: args.RequestId);

        if (args.ResourceData is Call call)
        {
            if (call.State == CallState.Established && call.MediaState?.Audio == MediaState.Active)
            {
                await this.BotRecordsOutgoingCallAsync(call.Id, args.TenantId, args.ScenarioId).ConfigureAwait(false);
            }
            else if (args.ChangeType == ChangeType.Deleted && call.State == CallState.Terminated)
            {
                this.GraphLogger.Log(TraceLevel.Info, $"Call State:{call.State}");
            }
        }
        else if (args.ResourceData is PlayPromptOperation playPromptOperation)
        {
            if (string.IsNullOrWhiteSpace(playPromptOperation.ClientContext))
            {
                throw new ServiceException(new Error()
                {
                    Message = "No call id proided in PlayPromptOperation.ClientContext.",
                });
            }
            else if (playPromptOperation.Status == OperationStatus.Completed)
            {
                await this.BotHangupCallAsync(playPromptOperation.ClientContext, args.TenantId, args.ScenarioId).ConfigureAwait(false);
                this.GraphLogger.Log(TraceLevel.Info, $"Disconnecting the call.");
            }
        }
    }

    private async Task BotHangupCallAsync(string callId, string tenantId, Guid scenarioId)
    {
        var hangupRequest = this.RequestBuilder.Communications.Calls[callId].Request();
        await this.GraphApiClient.SendAsync(hangupRequest, RequestType.Delete, tenantId, scenarioId).ConfigureAwait(false);
    }

    private async Task BotRecordsOutgoingCallAsync(string callId, string tenantId, Guid scenarioId)
    {

        IEnumerable<string> stopTones = new List<string>() { "#" };
        var recordRequest = this.RequestBuilder.Communications.Calls[callId].RecordResponse(
            bargeInAllowed: true,
            clientContext: callId,
            //prompts: prompts,
            maxRecordDurationInSeconds: 20,
            initialSilenceTimeoutInSeconds: 2,
            maxSilenceTimeoutInSeconds: 2,
            playBeep: true,
            stopTones: stopTones).Request();

        await this.GraphApiClient.SendAsync(recordRequest, RequestType.Create, tenantId, scenarioId).ConfigureAwait(false);
    }
}
