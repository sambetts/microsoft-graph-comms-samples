using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.Graph.Communications.Client.Authentication;
using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace ServiceHostedMediaBot.Authentication
{
    public class AuthenticationProvider : ObjectRoot, IRequestAuthenticationProvider
    {

        private readonly string appName;

        private readonly string appId;

        private readonly string appSecret;

        private readonly TimeSpan openIdConfigRefreshInterval = TimeSpan.FromHours(2);

        private DateTime prevOpenIdConfigUpdateTimestamp = DateTime.MinValue;

        private OpenIdConnectConfiguration openIdConnectConfiguration;

        public AuthenticationProvider(string appName, string appId, string appSecret, IGraphLogger logger)
            : base(logger.NotNull(nameof(logger)).CreateShim(nameof(AuthenticationProvider)))
        {
            this.appName = appName.NotNullOrWhitespace(nameof(appName));
            this.appId = appId.NotNullOrWhitespace(nameof(appId));
            this.appSecret = appSecret.NotNullOrWhitespace(nameof(appSecret));
        }

        public async Task AuthenticateOutboundRequestAsync(HttpRequestMessage request, string tenant)
        {
            const string schema = "Bearer";
            const string resource = "https://graph.microsoft.com/.default";

            tenant = string.IsNullOrWhiteSpace(tenant) ? "common" : tenant;

            this.GraphLogger.Info("AuthenticationProvider: Generating OAuth token.");

            var app = ConfidentialClientApplicationBuilder.Create(this.appId)
                                  .WithClientSecret(this.appSecret)
                                  .WithAuthority($"https://login.microsoftonline.com/{tenant}")
                                  .Build();

            var scopes = new string[] { resource };
            var result = app.AcquireTokenForClient(scopes);

            var auth = await result.ExecuteAsync();

            this.GraphLogger.Info($"Authentication Provider: Generated OAuth token. Expires in {auth.ExpiresOn.Subtract(DateTimeOffset.UtcNow).TotalMinutes} minutes.");

            request.Headers.Authorization = new AuthenticationHeaderValue(schema, auth.AccessToken);
        }

        [Obsolete]
        public async Task<RequestValidationResult> ValidateInboundRequestAsync(HttpRequestMessage request)
        {
            var token = request?.Headers?.Authorization?.Parameter;
            if (string.IsNullOrWhiteSpace(token))
            {
                return new RequestValidationResult { IsValid = false };
            }

            const string authDomain = "https://api.aps.skype.com/v1/.well-known/OpenIdConfiguration";
            if(this.openIdConnectConfiguration == null || DateTime.Now > this.prevOpenIdConfigUpdateTimestamp.Add(this.openIdConfigRefreshInterval))
            {
                this.GraphLogger.Info("Updating OpenID configuration");

                IConfigurationManager<OpenIdConnectConfiguration> configurationManager =
                    new ConfigurationManager<OpenIdConnectConfiguration>(

                        authDomain,
                        new OpenIdConnectConfigurationRetriever());
                this.openIdConnectConfiguration = await configurationManager.GetConfigurationAsync(CancellationToken.None).ConfigureAwait(false);

                this.prevOpenIdConfigUpdateTimestamp = DateTime.Now;
            }

            var authIssuers = new[]
            {
                "https://graph.microsoft.com",
                "https://api.botframework.com",
            };

            TokenValidationParameters validationParameters = new TokenValidationParameters
            {
                ValidIssuers = authIssuers,
                ValidAudience = this.appId,
                IssuerSigningKeys = this.openIdConnectConfiguration.SigningKeys,
            };

            ClaimsPrincipal claimsPrincipal;
            try
            {
                JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
                claimsPrincipal = handler.ValidateToken(token, validationParameters, out _);
            }
            catch(Exception ex)
            {
                this.GraphLogger.Error(ex, $"Failed to validate token for client: {this.appId}.");
                return new RequestValidationResult() { IsValid = false };
            }

            const string ClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";
            var tenantClaim = claimsPrincipal.FindFirst(claim => claim.Type.Equals(ClaimType, StringComparison.Ordinal));
            
            if (string.IsNullOrEmpty(tenantClaim?.Value))
            {
                return new RequestValidationResult { IsValid = false };
            }

            request.Properties.Add(HttpConstants.HeaderNames.Tenant, tenantClaim.Value);
            return new RequestValidationResult { IsValid = true, TenantId = tenantClaim.Value };
        }

    }
}
