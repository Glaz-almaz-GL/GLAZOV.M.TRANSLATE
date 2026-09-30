using GLTranslate.Abstractions.Providers;

namespace GLTranslate.Providers.Common.Tests;

/// <summary>
/// Verifies the part every engine shares: the credentials it keeps, the client
/// it owns or borrows, and the failures it reports.
/// </summary>
public sealed class CredentialedEngineTests
{
    private sealed record Account(string Name);

    private sealed class FakeEngine : CredentialedEngine<Account>
    {
        public FakeEngine(Account credentials)
            : base("Fake", credentials)
        {
        }

        public FakeEngine(Account credentials, HttpClient httpClient)
            : base("Fake", credentials, httpClient)
        {
        }

        public Account Keeps => Credentials;

        public HttpClient Client => HttpClient;

        public bool Disposed => IsDisposed;

        public ProviderException RefusedByStatus(int status, string? explanation)
        {
            return Refused(status, explanation);
        }

        public ProviderException RefusedByCode(int code, string? explanation)
        {
            return RefusedWithCode(code, explanation);
        }
    }

    [Fact]
    public void Constructor_KeepsTheCredentials()
    {
        Account account = new("me");
        using FakeEngine engine = new(account);

        Assert.Same(account, engine.Keeps);
    }

    [Fact]
    public void Constructor_NullCredentials_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new FakeEngine(null!));
        Assert.Throws<ArgumentNullException>(() => new FakeEngine(null!, new HttpClient()));
    }

    [Fact]
    public void Constructor_NullClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new FakeEngine(new Account("me"), null!));
    }

    [Fact]
    public void Dispose_ClientOfItsOwn_IsDisposedWithIt()
    {
        FakeEngine engine = new(new Account("me"));
        HttpClient client = engine.Client;

        engine.Dispose();

        Assert.True(engine.Disposed);
        Assert.Throws<ObjectDisposedException>(() => client.BaseAddress = new Uri("https://example.com"));
    }

    [Fact]
    public void Dispose_ClientItWasGiven_IsLeftAlone()
    {
        using HttpClient client = new();
        FakeEngine engine = new(new Account("me"), client);

        engine.Dispose();

        client.BaseAddress = new Uri("https://example.com");

        Assert.Equal(new Uri("https://example.com"), client.BaseAddress);
    }

    [Theory]
    [InlineData(403, "API has not been used", "Fake refused the request with status 403: API has not been used")]
    [InlineData(502, null, "Fake refused the request with status 502.")]
    [InlineData(502, "  ", "Fake refused the request with status 502.")]
    public void Refused_ReportsTheStatusAndWhatWasSaid(int status, string? explanation, string expected)
    {
        using FakeEngine engine = new(new Account("me"));

        ProviderException exception = engine.RefusedByStatus(status, explanation);

        Assert.Equal("Fake", exception.ProviderName);
        Assert.Equal(expected, exception.Message);
    }

    [Theory]
    [InlineData(54001, "Invalid Sign", "Fake refused the request with code 54001: Invalid Sign")]
    [InlineData(54003, null, "Fake refused the request with code 54003.")]
    public void RefusedWithCode_ReportsTheCodeAndWhatWasSaid(int code, string? explanation, string expected)
    {
        using FakeEngine engine = new(new Account("me"));

        Assert.Equal(expected, engine.RefusedByCode(code, explanation).Message);
    }
}
