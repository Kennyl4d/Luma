using System.Net;
using System.Text;
using System.Text.Json;
using Luma.Services;

int passed = 0;
void Check(bool value, string name) { if (!value) throw new Exception(name); Console.WriteLine("PASS " + name); passed++; }
async Task<UpdateResult> Request(string body, HttpStatusCode code = HttpStatusCode.OK, Version? current = null)
{
    using var client = new HttpClient(new FakeHandler(request => {
        Check(request.RequestUri!.AbsoluteUri == "https://api.github.com/repos/Kennyl4d/Luma/releases/latest", "Correct repository endpoint");
        Check(request.Headers.UserAgent.Count > 0, "GitHub User-Agent");
        return new HttpResponseMessage(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }));
    return await new UpdateService(client).CheckAsync(current ?? new Version(0, 3, 5, 0));
}
string Release(string tag, bool draft = false, bool prerelease = false) => JsonSerializer.Serialize(new { tag_name = tag, draft, prerelease });
Check((await Request(Release("v0.3.6"))).State == UpdateState.Available, "New stable release");
Check((await Request(Release("v0.3.5"))).State == UpdateState.Current, "Three component tag equals assembly version");
Check((await Request(Release("v0.3.4"))).State == UpdateState.Current, "Older release does not prompt downgrade");
Check((await Request(Release("v0.10.0"))).State == UpdateState.Available, "Numeric version comparison");
Check((await Request(Release("v0.3.5+build.123"))).State == UpdateState.Current, "Build metadata ignored");
Check((await Request(Release("v0.4.0", draft:true))).State == UpdateState.Failed, "Draft rejected");
Check((await Request(Release("v0.4.0", prerelease:true))).State == UpdateState.Failed, "Prerelease rejected");
Check((await Request(Release("v0.4.0-beta.1"))).State == UpdateState.Failed, "Nonstable tag rejected");
Check((await Request("null")).State == UpdateState.Failed, "Malformed payload is reported");
Check((await Request("{" )).State == UpdateState.Failed, "Invalid JSON is reported");
Check((await Request("{}" )).State == UpdateState.Failed, "Missing tag is reported");
Check((await Request("", HttpStatusCode.NotFound)).State == UpdateState.Unavailable, "404 is distinct from up to date");
Check((await Request("", HttpStatusCode.Forbidden)).State == UpdateState.Failed, "Rate limit is distinct from up to date");
Check((await Request("", HttpStatusCode.InternalServerError)).State == UpdateState.Failed, "Server error is reported");
Check((await Request(Release("v0.3.6"))).ReleaseUrl == "https://github.com/Kennyl4d/Luma/releases/tag/v0.3.6", "Canonical release link");
using (var client = new HttpClient(new FakeHandler(_ => throw new HttpRequestException("offline"))))
    Check((await new UpdateService(client).CheckAsync(new Version(0,3,5))).State == UpdateState.Failed, "Offline failure");
using (var client = new HttpClient(new FakeHandler(_ => throw new TaskCanceledException("timeout"))))
    Check((await new UpdateService(client).CheckAsync(new Version(0,3,5))).State == UpdateState.Failed, "Timeout failure");
using (var cancellation = new CancellationTokenSource())
using (var client = new HttpClient(new FakeHandler(_ => throw new OperationCanceledException())))
{
    cancellation.Cancel();
    try { await new UpdateService(client).CheckAsync(new Version(0,3,5), cancellation.Token); throw new Exception("Cancellation was swallowed"); }
    catch (OperationCanceledException) { Check(true, "Window-close cancellation propagates"); }
}
var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
using var catalog = JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"Luma/Resources/Strings.json")));
var keys = catalog.RootElement.GetProperty("keys").EnumerateArray().Select(x=>x.GetString()!).ToArray();
foreach (var language in catalog.RootElement.GetProperty("languages").EnumerateObject())
{
    var values = language.Value.EnumerateArray().Select(x=>x.GetString()!).ToArray();
    Check(values.Length == keys.Length && values.All(x=>!string.IsNullOrWhiteSpace(x)), language.Name + " complete catalog");
    Check(values[Array.IndexOf(keys,"UpdateAvailable")].Contains("{0}"), language.Name + " update placeholder");
}
Console.WriteLine($"{passed} checks passed.");
if (args.Contains("--live"))
{
    var live=await new UpdateService().CheckAsync(new Version(0,3,5));
    Check(live.State is UpdateState.Current or UpdateState.Available,"Live GitHub release API: "+live.State);
}
sealed class FakeHandler(Func<HttpRequestMessage,HttpResponseMessage> respond) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(respond(request));
}
