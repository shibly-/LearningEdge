using LearningEdge.Application.Common.Exceptions;
using LearningEdge.Application.Interfaces;
using LearningEdge.Infrastructure.Identity;
using System.Net;

namespace LearningEdge.Tests.Infrastructure.Identity;

public class KeycloakAdminClientTests
{
    [Fact]
    public async Task CreateLearner_ShouldFailBeforeCallingKeycloak_WhenTheAdminClientIsNotConfigured()
    {
        var handler = new StubHandler();
        var client = new KeycloakAdminClient(new HttpClient(handler), Settings(secret: ""));

        var error = await Assert.ThrowsAsync<KeycloakProvisioningException>(() =>
            client.CreateLearnerAsync(Account(), CancellationToken.None));

        Assert.Contains("AdminClientSecret", error.Message);
        Assert.Empty(handler.Calls);
    }

    [Fact]
    public async Task CreateLearner_ShouldCreateTheUserAndAssignTheLearnerRole()
    {
        var handler = new StubHandler();
        handler.Respond(_ => Json(HttpStatusCode.OK, """{"access_token":"admin-token"}"""));
        handler.Respond(_ => Created("https://keycloak.test/admin/realms/learningedge/users/kc-1"));
        handler.Respond(_ => Json(HttpStatusCode.OK, """{"id":"role-1","name":"learner"}"""));
        handler.Respond(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = new KeycloakAdminClient(new HttpClient(handler), Settings());

        var id = await client.CreateLearnerAsync(Account(), CancellationToken.None);

        Assert.Equal("kc-1", id);
        Assert.Equal(HttpMethod.Post, handler.Calls[1].Method);
        Assert.EndsWith("/users", handler.Calls[1].Url);
        Assert.Contains("Password1", handler.Calls[1].Body);
        Assert.Contains("/roles/learner", handler.Calls[2].Url);
        Assert.EndsWith("/role-mappings/realm", handler.Calls[3].Url);
    }

    [Fact]
    public async Task CreateLearner_ShouldResetThePassword_WhenTheEmailAlreadyExists()
    {
        var handler = new StubHandler();
        handler.Respond(_ => Json(HttpStatusCode.OK, """{"access_token":"admin-token"}"""));
        handler.Respond(_ => new HttpResponseMessage(HttpStatusCode.Conflict));
        handler.Respond(_ => Json(HttpStatusCode.OK, """[{"id":"kc-existing"}]"""));
        handler.Respond(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        handler.Respond(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        handler.Respond(_ => Json(HttpStatusCode.OK, """{"id":"role-1","name":"learner"}"""));
        handler.Respond(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = new KeycloakAdminClient(new HttpClient(handler), Settings());

        var id = await client.CreateLearnerAsync(Account(), CancellationToken.None);

        Assert.Equal("kc-existing", id);
        Assert.Contains("/users/kc-existing/reset-password", handler.Calls[3].Url);
        Assert.Contains("Password1", handler.Calls[3].Body);
    }

    private static KeycloakLearnerAccount Account() =>
        new("ada@example.com", "Ada", "Lovelace", "Password1");

    private static KeycloakAdminSettings Settings(string secret = "secret") => new()
    {
        TokenUrl = "https://keycloak.test/token",
        AdminRoot = "https://keycloak.test/admin/realms/learningedge",
        ClientId = "le-realm-admin",
        ClientSecret = secret,
        LearnerRole = "learner",
    };

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

    private static HttpResponseMessage Created(string location)
    {
        var response = new HttpResponseMessage(HttpStatusCode.Created);
        response.Headers.Location = new Uri(location);
        return response;
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses = new();
        public List<(HttpMethod Method, string Url, string? Body)> Calls { get; } = [];

        public void Respond(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responses.Enqueue(responder);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            Calls.Add((request.Method, request.RequestUri!.ToString(), body));
            return _responses.Dequeue()(request);
        }
    }
}
