using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace Luma.Services;

public enum UpdateState { Available, Current, Unavailable, Failed }
public sealed record UpdateResult(UpdateState State, string? Version = null, string? ReleaseUrl = null);

// Shared by Windows and Linux. Network access occurs only on explicit request.
public sealed class UpdateService
{
    private static readonly HttpClient defaultClient = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly HttpClient client;
    public UpdateService(HttpClient? client = null) => this.client = client ?? defaultClient;

    public async Task<UpdateResult> CheckAsync(Version current, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/Kennyl4d/Luma/releases/latest");
            request.Headers.UserAgent.ParseAdd("Luma/" + current.ToString(3));
            request.Headers.Accept.ParseAdd("application/vnd.github+json");
            request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
            using var response = await client.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return new(UpdateState.Unavailable);
            if (!response.IsSuccessStatusCode) return new(UpdateState.Failed);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var release = json.RootElement;
            if (release.ValueKind != JsonValueKind.Object) return new(UpdateState.Failed);
            if (!release.TryGetProperty("draft", out var draft) || draft.ValueKind != JsonValueKind.False ||
                !release.TryGetProperty("prerelease", out var prerelease) || prerelease.ValueKind != JsonValueKind.False ||
                !release.TryGetProperty("tag_name", out var tagValue) || tagValue.ValueKind != JsonValueKind.String)
                return new(UpdateState.Failed);
            string tag = tagValue.GetString()!;
            if (!TryParseVersion(tag, out var latest)) return new(UpdateState.Failed);
            return latest > Normalize(current)
                ? new(UpdateState.Available, tag, "https://github.com/Kennyl4d/Luma/releases/tag/" + Uri.EscapeDataString(tag))
                : new(UpdateState.Current);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return new(UpdateState.Failed); }
        catch (HttpRequestException) { return new(UpdateState.Failed); }
        catch (JsonException) { return new(UpdateState.Failed); }
    }

    internal static bool TryParseVersion(string tag, out Version version)
    {
        string value = tag.Trim().TrimStart('v', 'V').Split('+')[0];
        if (Version.TryParse(value, out var parsed)) { version = Normalize(parsed); return true; }
        version = new Version(0, 0, 0, 0); return false;
    }
    private static Version Normalize(Version value) => new(value.Major, value.Minor, Math.Max(0, value.Build), Math.Max(0, value.Revision));
}
