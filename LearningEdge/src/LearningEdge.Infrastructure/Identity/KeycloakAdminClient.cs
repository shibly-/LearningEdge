using LearningEdge.Application.Common.Exceptions;
using LearningEdge.Application.Interfaces;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LearningEdge.Infrastructure.Identity;

public sealed class KeycloakAdminSettings
{
    public required string TokenUrl { get; init; }
    public required string AdminRoot { get; init; }
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public required string LearnerRole { get; init; }
}

/// <summary>
/// Keycloak admin REST client. Uses a confidential client's service account,
/// never the signed-in user's token.
/// </summary>
public sealed class KeycloakAdminClient : IKeycloakUserDirectory
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly KeycloakAdminSettings _settings;

    public KeycloakAdminClient(HttpClient http, KeycloakAdminSettings settings)
    {
        _http = http;
        _settings = settings;
    }

    public async Task<string> CreateLearnerAsync(KeycloakLearnerAccount account, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.ClientId) || string.IsNullOrWhiteSpace(_settings.ClientSecret))
        {
            throw new KeycloakProvisioningException(
                "Keycloak admin client is not configured. Set Keycloak:AdminClientId and Keycloak:AdminClientSecret.");
        }

        try
        {
            var token = await RequestTokenAsync(cancellationToken);
            var userId = await CreateOrReuseUserAsync(token, account, cancellationToken);
            await AssignRealmRoleAsync(token, userId, cancellationToken);
            return userId;
        }
        catch (KeycloakProvisioningException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            throw new KeycloakProvisioningException("Cannot reach Keycloak to create the user.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new KeycloakProvisioningException("Keycloak did not respond in time.", ex);
        }
    }

    private async Task<string> RequestTokenAsync(CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _settings.ClientId,
            ["client_secret"] = _settings.ClientSecret,
        });

        using var response = await _http.PostAsync(_settings.TokenUrl, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw await Failure(response, "Keycloak rejected the admin client.", cancellationToken);
        }

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions, cancellationToken);
        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            throw new KeycloakProvisioningException("Keycloak did not return an admin access token.");
        }

        return token.AccessToken;
    }

    private async Task<string> CreateOrReuseUserAsync(
        string token,
        KeycloakLearnerAccount account,
        CancellationToken cancellationToken)
    {
        var created = await SendAsync(
            HttpMethod.Post,
            $"{_settings.AdminRoot}/users",
            token,
            new NewKeycloakUser
            {
                Username = account.Email,
                Email = account.Email,
                FirstName = account.FirstName,
                LastName = account.LastName,
                Enabled = true,
                EmailVerified = true,
                Credentials =
                [
                    new KeycloakCredential { Type = "password", Value = account.Password, Temporary = false },
                ],
            },
            cancellationToken);

        if (created.StatusCode == HttpStatusCode.Created)
        {
            return ReadUserId(created);
        }

        if (created.StatusCode == HttpStatusCode.Conflict)
        {
            created.Dispose();
            return await ReuseExistingUserAsync(token, account, cancellationToken);
        }

        using (created)
        {
            throw await Failure(created, "Keycloak rejected the new user.", cancellationToken);
        }
    }

    private async Task<string> ReuseExistingUserAsync(
        string token,
        KeycloakLearnerAccount account,
        CancellationToken cancellationToken)
    {
        var lookup = $"{_settings.AdminRoot}/users?email={Uri.EscapeDataString(account.Email)}&exact=true";
        using var response = await SendAsync(HttpMethod.Get, lookup, token, null, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw await Failure(response, "Keycloak could not look up the existing user.", cancellationToken);
        }

        var matches = await response.Content.ReadFromJsonAsync<List<KeycloakUserId>>(JsonOptions, cancellationToken);
        var userId = matches?.FirstOrDefault(match => !string.IsNullOrWhiteSpace(match.Id))?.Id;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new KeycloakProvisioningException(
                "Keycloak already has this email, but the existing user could not be found.");
        }

        using var passwordReset = await SendAsync(
            HttpMethod.Put,
            $"{_settings.AdminRoot}/users/{userId}/reset-password",
            token,
            new KeycloakCredential { Type = "password", Value = account.Password, Temporary = false },
            cancellationToken);
        if (!passwordReset.IsSuccessStatusCode)
        {
            throw await Failure(passwordReset, "Keycloak could not set the user's password.", cancellationToken);
        }

        using var enabled = await SendAsync(
            HttpMethod.Put,
            $"{_settings.AdminRoot}/users/{userId}",
            token,
            new EnableKeycloakUser(),
            cancellationToken);
        if (!enabled.IsSuccessStatusCode)
        {
            throw await Failure(enabled, "Keycloak could not enable the existing user.", cancellationToken);
        }

        return userId;
    }

    private async Task AssignRealmRoleAsync(string token, string userId, CancellationToken cancellationToken)
    {
        var roleUrl = $"{_settings.AdminRoot}/roles/{Uri.EscapeDataString(_settings.LearnerRole)}";
        using var roleResponse = await SendAsync(HttpMethod.Get, roleUrl, token, null, cancellationToken);
        if (roleResponse.StatusCode == HttpStatusCode.NotFound)
        {
            throw new KeycloakProvisioningException(
                $"The Keycloak realm is missing the '{_settings.LearnerRole}' role.");
        }

        if (!roleResponse.IsSuccessStatusCode)
        {
            throw await Failure(roleResponse, "Keycloak could not read the learner role.", cancellationToken);
        }

        var role = await roleResponse.Content.ReadFromJsonAsync<KeycloakRole>(JsonOptions, cancellationToken);
        if (role is null || string.IsNullOrWhiteSpace(role.Id))
        {
            throw new KeycloakProvisioningException(
                $"The Keycloak realm role '{_settings.LearnerRole}' has no id.");
        }

        using var assigned = await SendAsync(
            HttpMethod.Post,
            $"{_settings.AdminRoot}/users/{userId}/role-mappings/realm",
            token,
            new[] { role },
            cancellationToken);

        // 409 means the user already has the role, which is the state we want.
        if (!assigned.IsSuccessStatusCode && assigned.StatusCode != HttpStatusCode.Conflict)
        {
            throw await Failure(assigned, "Keycloak could not assign the learner role.", cancellationToken);
        }
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string url,
        string token,
        object? body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        return await _http.SendAsync(request, cancellationToken);
    }

    private string ReadUserId(HttpResponseMessage response)
    {
        var location = response.Headers.Location;
        if (location is null)
        {
            response.Dispose();
            throw new KeycloakProvisioningException("Keycloak created the user but did not return an id.");
        }

        var absolute = location.IsAbsoluteUri
            ? location
            : new Uri(new Uri(_settings.AdminRoot.TrimEnd('/') + "/"), location);
        var id = absolute.Segments[^1].Trim('/');
        response.Dispose();
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new KeycloakProvisioningException("Keycloak created the user but did not return an id.");
        }

        return id;
    }

    private static async Task<KeycloakProvisioningException> Failure(
        HttpResponseMessage response,
        string summary,
        CancellationToken cancellationToken)
    {
        var detail = await ReadErrorDetail(response, cancellationToken);
        var message = string.IsNullOrWhiteSpace(detail)
            ? $"{summary} (HTTP {(int)response.StatusCode})."
            : $"{summary} {detail}";
        return new KeycloakProvisioningException(message);
    }

    private static async Task<string?> ReadErrorDetail(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(body) || body.Length > 2000 || !body.TrimStart().StartsWith('{'))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("errorMessage", out var errorMessage)
                && errorMessage.ValueKind == JsonValueKind.String)
            {
                return errorMessage.GetString();
            }

            if (document.RootElement.TryGetProperty("error_description", out var description)
                && description.ValueKind == JsonValueKind.String)
            {
                return description.GetString();
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = "";
    }

    private sealed class NewKeycloakUser
    {
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public bool Enabled { get; set; }
        public bool EmailVerified { get; set; }
        public KeycloakCredential[] Credentials { get; set; } = [];
    }

    private sealed class KeycloakCredential
    {
        public string Type { get; set; } = "password";
        public string Value { get; set; } = "";
        public bool Temporary { get; set; }
    }

    private sealed class EnableKeycloakUser
    {
        public bool Enabled { get; set; } = true;
        public bool EmailVerified { get; set; } = true;
    }

    private sealed class KeycloakUserId
    {
        public string Id { get; set; } = "";
    }

    private sealed class KeycloakRole
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
    }
}
