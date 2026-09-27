using Aspire.Hosting.ApplicationModel;

namespace Brigade.Net.Benchmarks.Api.AppHost;

internal sealed class MariaDbResource(
    string name,
    ParameterResource password,
    string databaseName
) : ContainerResource(name), IResourceWithConnectionString
{
    internal const string EndpointName = "mariadb";

    private EndpointReference? endpoint;

    public ParameterResource Password => password;

    public EndpointReference Endpoint => endpoint ??= new EndpointReference(this, EndpointName);

    public ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create(
        $"Server={Endpoint.Property(EndpointProperty.Host)};Port={Endpoint.Property(EndpointProperty.Port)};Database={databaseName};User ID=root;Password={password}"
    );
}
