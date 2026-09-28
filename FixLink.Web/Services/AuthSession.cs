
using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace FixLink.Web.Services;

public class AuthSession
{
    private const string StorageKey = "fixlink_auth_token";
    private readonly IJSRuntime _jsRuntime;

    public AuthSession(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public string? Token { get; private set; }

    public bool IsAuthenticated =>
        !string.IsNullOrWhiteSpace(Token);

    public bool IsAdmin
    {
        get
        {
            if (!IsAuthenticated)
                return false;

            try
            {
                var parts = Token!.Split('.');

                if (parts.Length != 3)
                    return false;

                var payload = parts[1]
                    .Replace('-', '+')
                    .Replace('_', '/');

                if (payload.Length % 4 == 2)
                    payload += "==";
                else if (payload.Length % 4 == 3)
                    payload += "=";

                var bytes = Convert.FromBase64String(payload);
                var json = Encoding.UTF8.GetString(bytes);

                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                string[] roleClaimNames =
                {
                    "role",
                    "roles",
                    "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
                };

                foreach (var claimName in roleClaimNames)
                {
                    if (!root.TryGetProperty(claimName, out var claim))
                        continue;

                    if (claim.ValueKind == JsonValueKind.String &&
                        IsAdminRole(claim.GetString()))
                    {
                        return true;
                    }

                    if (claim.ValueKind == JsonValueKind.Array &&
                        claim.EnumerateArray().Any(role =>
                            role.ValueKind == JsonValueKind.String &&
                            IsAdminRole(role.GetString())))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }

    public async Task SetTokenAsync(string token)
    {
        Token = token;

        await _jsRuntime.InvokeVoidAsync(
            "fixLinkAuthStorage.set",
            StorageKey,
            token);
    }

    public async Task RestoreTokenAsync()
    {
        var savedToken = await _jsRuntime.InvokeAsync<string?>(
            "fixLinkAuthStorage.get",
            StorageKey);

        Token = savedToken;
    }

    public async Task ClearTokenAsync()
    {
        Token = null;

        await _jsRuntime.InvokeVoidAsync(
            "fixLinkAuthStorage.remove",
            StorageKey);
    }

    private static bool IsAdminRole(string? role) =>
        string.Equals(
            role,
            "Admin",
            StringComparison.OrdinalIgnoreCase);
}