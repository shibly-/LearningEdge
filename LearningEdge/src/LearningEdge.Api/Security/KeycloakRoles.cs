using System.Security.Claims;
using System.Text.Json;

namespace LearningEdge.Api.Security;

/// <summary>
/// Reads Keycloak realm roles whether they arrived as role claims or as the
/// <c>realm_access</c> object on the access token.
/// </summary>
public static class KeycloakRoles
{
    /// <summary>
    /// Names a realm might use for the platform operator. Compared after
    /// lowercasing, so <c>SysAdmin</c> and <c>system-admin</c> both match.
    /// </summary>
    private static readonly string[] SysAdminAliases =
    [
        "sysadmin",
        "system-admin",
        "system_admin",
        "systemadmin",
        "superadmin",
        "super-admin",
        "super_admin",
    ];

    public static bool Has(ClaimsPrincipal user, string role)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var accepted = Accepted(role);
        foreach (var claim in user.Claims)
        {
            if ((claim.Type is "role" or ClaimTypes.Role or "realm_access.roles")
                && Matches(claim.Value, accepted))
            {
                return true;
            }
        }

        return RealmAccessContains(user.FindFirst("realm_access")?.Value, accepted);
    }

    /// <summary>
    /// Copies roles out of a JSON <c>realm_access</c> claim onto
    /// <see cref="ClaimTypes.Role"/>, which is what role checks read.
    /// </summary>
    public static void EnsureRoleClaims(ClaimsIdentity identity)
    {
        var raw = identity.FindFirst("realm_access")?.Value;
        if (string.IsNullOrWhiteSpace(raw) || !raw.TrimStart().StartsWith('{'))
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(raw);
            if (!document.RootElement.TryGetProperty("roles", out var roles)
                || roles.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var entry in roles.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var value = entry.GetString();
                if (string.IsNullOrWhiteSpace(value) || identity.HasClaim(ClaimTypes.Role, value))
                {
                    continue;
                }

                identity.AddClaim(new Claim(ClaimTypes.Role, value));
            }
        }
        catch (JsonException)
        {
            // A malformed realm_access claim is ignored. Authorization then fails closed.
        }
    }

    private static string[] Accepted(string role)
    {
        var wanted = role.Trim().ToLowerInvariant();
        return wanted == "sysadmin" ? SysAdminAliases : [wanted];
    }

    private static bool RealmAccessContains(string? raw, IReadOnlyList<string> accepted)
    {
        if (string.IsNullOrWhiteSpace(raw) || !raw.TrimStart().StartsWith('{'))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(raw);
            if (!document.RootElement.TryGetProperty("roles", out var roles)
                || roles.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var entry in roles.EnumerateArray())
            {
                if (entry.ValueKind == JsonValueKind.String && Matches(entry.GetString(), accepted))
                {
                    return true;
                }
            }
        }
        catch (JsonException)
        {
            return false;
        }

        return false;
    }

    private static bool Matches(string? value, IReadOnlyList<string> accepted)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().ToLowerInvariant();
        foreach (var candidate in accepted)
        {
            if (normalized == candidate)
            {
                return true;
            }
        }

        return false;
    }
}
