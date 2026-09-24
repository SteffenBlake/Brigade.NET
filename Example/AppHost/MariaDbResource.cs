using Aspire.Hosting.ApplicationModel;

namespace Brigade.Net.Example.AppHost;

internal sealed class MariaDbResource(
    string name,
    ParameterResource password,
    string databaseName
) : ContainerResource(name), IResourceWithConnectionString
{
    internal const string EndpointName = "mariadb";

    private EndpointReference? _endpoint;

    public ParameterResource Password => password;

    public string DatabaseName => databaseName;

    public EndpointReference Endpoint => _endpoint ??= new EndpointReference(this, EndpointName);

    public ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create(
        $"Server={Endpoint.Property(EndpointProperty.Host)};Port={Endpoint.Property(EndpointProperty.Port)};Database={databaseName};User ID=root;Password={password}"
    );
}
