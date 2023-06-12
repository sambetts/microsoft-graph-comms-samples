using System.Text.Json.Serialization;

namespace PstnBot;

// Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
public class MeetingCapability
{
    [JsonPropertyName("@odata.type")]
    public string? odatatype { get; set; }
}

public class ResourceData
{
    [JsonPropertyName("@odata.type")]
    public string? odatatype { get; set; }

    [JsonPropertyName("state")]
    public string? state { get; set; }

    [JsonPropertyName("resultInfo")]
    public ResultInfo resultInfo { get; set; }

    [JsonPropertyName("meetingCapability")]
    public MeetingCapability meetingCapability { get; set; }

    [JsonPropertyName("coOrganizers")]
    public List<object> CoOrganizers { get; set; }

    [JsonPropertyName("callChainId")]
    public string? callChainId { get; set; }
}

public class ResultInfo
{
    [JsonPropertyName("@odata.type")]
    public string? odatatype { get; set; }

    [JsonPropertyName("code")]
    public int code { get; set; }

    [JsonPropertyName("subcode")]
    public int subcode { get; set; }

    [JsonPropertyName("message")]
    public string? message { get; set; }
}

public class CommsNotifications
{
    [JsonPropertyName("@odata.type")]
    public string? odatatype { get; set; }

    [JsonPropertyName("value")]
    public List<CommsNotification> value { get; set; }
}

public class CommsNotification
{
    [JsonPropertyName("@odata.type")]
    public string? odatatype { get; set; }

    [JsonPropertyName("changeType")]
    public string? changeType { get; set; }

    [JsonPropertyName("resource")]
    public string? resource { get; set; }

    [JsonPropertyName("resourceUrl")]
    public string? resourceUrl { get; set; }

    [JsonPropertyName("resourceData")]
    public ResourceData? resourceData { get; set; }
}


public class IdentitySet
{
    [JsonPropertyName("phone")]
    public Identity? Phone { get; set; }
}
public class Identity
{
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;
}

public class ParticipantInfo
{
    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("identity")]
    public IdentitySet Identity { get; set; }

    [JsonPropertyName("endpointType")]
    public string? EndpointType { get; set; }

    [JsonPropertyName("languageId")]
    public string? LanguageId { get; set; }

    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [JsonPropertyName("participantId")]
    public string? ParticipantId { get; set; }
}

// Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
public class CallOptions
{
    [JsonPropertyName("@odata.type")]
    public string? OdataType { get; set; }
}

public class ChatInfo
{
    [JsonPropertyName("@odata.type")]
    public string? OdataType { get; set; }
}

public class ContentSharingSession
{
    [JsonPropertyName("@odata.type")]
    public string? OdataType { get; set; }
}

public class MediaConfig
{
    [JsonPropertyName("@odata.type")]
    public string? OdataType { get; set; }

    [JsonPropertyName("PreFetchMedia")]
    public List<Microsoft.Graph.MediaInfo> PreFetchMedia { get; set; } = new();
}

public class MediaState
{
    [JsonPropertyName("@odata.type")]
    public string? OdataType { get; set; }
}

public class MeetingInfo
{
    [JsonPropertyName("@odata.type")]
    public string? OdataType { get; set; }
}

public class Call
{
    [JsonPropertyName("callbackUri")]
    public string? CallbackUri { get; set; }

    [JsonPropertyName("callChainId")]
    public string? CallChainId { get; set; }

    [JsonPropertyName("callOptions")]
    public CallOptions CallOptions { get; set; }

    [JsonPropertyName("chatInfo")]
    public ChatInfo ChatInfo { get; set; }

    [JsonPropertyName("contentSharingSessions")]
    public List<ContentSharingSession> ContentSharingSessions { get; set; }

    [JsonPropertyName("direction")]
    public string? Direction { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("mediaConfig")]
    public MediaConfig MediaConfig { get; set; }


    [JsonPropertyName("myParticipantId")]
    public string? MyParticipantId { get; set; }

    [JsonPropertyName("replacesContext")]
    public string? ReplacesContext { get; set; }

    [JsonPropertyName("requestedModalities")]
    public List<string?>? RequestedModalities { get; set; }

    [JsonPropertyName("resultInfo")]
    public ResultInfo? ResultInfo { get; set; }

    [JsonPropertyName("source")]
    public Source? Source { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = null!;

    [JsonPropertyName("targets")]
    public List<ParticipantInfo>? Targets { get; set; } = new();

    [JsonPropertyName("toneInfo")]
    public ToneInfo? ToneInfo { get; set; }
}

public class Source
{
    [JsonPropertyName("@odata.type")]
    public string? OdataType { get; set; }
}

public class ToneInfo
{
    [JsonPropertyName("@odata.type")]
    public string? OdataType { get; set; }
}
