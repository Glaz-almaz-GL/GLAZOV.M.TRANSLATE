namespace GLAZOV.M.TRANSLATE.Providers.GoogleCloud.Tests;

/// <summary>
/// An <see cref="HttpMessageHandler"/> that returns a canned response built
/// by the supplied delegate, without performing any network I/O.
/// </summary>
internal sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(responder(request));
    }
}
