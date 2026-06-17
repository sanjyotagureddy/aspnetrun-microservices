using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);


// ReSharper disable once EmptyRegion
#region Persistence

IResourceBuilder<ParameterResource> keycloakAdminPassword = builder.AddParameter("keycloak-admin-password", secret: true);
IResourceBuilder<ParameterResource> authWebClientSecret = builder.AddParameter("auth-web-client-secret", secret: true);
IResourceBuilder<ParameterResource> authDevBootstrapEnabled = builder.AddParameter("auth-dev-bootstrap-enabled");
IResourceBuilder<ParameterResource> authDevBootstrapSharedSecret = builder.AddParameter("auth-dev-bootstrap-shared-secret", secret: true);

IResourceBuilder<ParameterResource> postgresPassword = builder.AddParameter("postgres-password", secret: true);
IResourceBuilder<ParameterResource> openSearchInitialAdminPassword = builder.AddParameter("opensearch-initial-admin-password", secret: true);

IResourceBuilder<PostgresServerResource> postgresDb = builder.AddPostgres("productsdb", password: postgresPassword)
        .WithDataVolume("postgres-data")
        .WithPgAdmin(containerName: "pgAdmin")
        .WithHostPort(5432);

IResourceBuilder<PostgresDatabaseResource> productDb = postgresDb.AddDatabase("products", "products");
IResourceBuilder<PostgresDatabaseResource> inventory = postgresDb.AddDatabase("inventory", "inventory");
IResourceBuilder<PostgresDatabaseResource> authIdentityDb = postgresDb.AddDatabase("auth-identity", "auth_identity");
IResourceBuilder<KafkaServerResource> messaging = builder.AddKafka("message-broker")
    .WithDataVolume("kafka-data")
    .WithKafkaUI();

IResourceBuilder<RedisResource> authCache = builder.AddRedis("auth-cache")
    .WithDataVolume("auth-cache-data");

IResourceBuilder<ContainerResource> keycloak = builder.AddContainer("keycloak", "quay.io/keycloak/keycloak")
    .WithEnvironment("KEYCLOAK_ADMIN", "admin")
    .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", keycloakAdminPassword)
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
    //.WithVolume("keycloak-data", "/opt/keycloak/data")
    .WithArgs("start-dev");

IResourceBuilder<ContainerResource> openSearch = builder.AddContainer("opensearch", "opensearchproject/opensearch")
    .WithEnvironment("discovery.type", "single-node")
    .WithEnvironment("plugins.security.disabled", "true")
    .WithEnvironment("OPENSEARCH_INITIAL_ADMIN_PASSWORD", openSearchInitialAdminPassword)
    .WithEnvironment("OPENSEARCH_JAVA_OPTS", "-Xms512m -Xmx512m")
    .WithHttpEndpoint(port: 9200, targetPort: 9200, name: "http")
    .WithVolume("opensearch-data", "/usr/share/opensearch/data");

IResourceBuilder<ContainerResource> openSearchDashboards = builder.AddContainer("opensearch-dashboards", "opensearchproject/opensearch-dashboards")
    .WithEnvironment("OPENSEARCH_HOSTS", "[\"http://opensearch:9200\"]")
    .WithEnvironment("DISABLE_SECURITY_DASHBOARDS_PLUGIN", "true")
    .WithHttpEndpoint(port: 5601, targetPort: 5601, name: "http")
    .WithVolume("opensearch-dashboards-data", "/usr/share/opensearch-dashboards/data")
    .WaitFor(openSearch);

#endregion
#region Services



builder.AddProject<Auth_Api>("auth-api")
    .WithHttpEndpoint(port: 5000, name: "http")
    .WithReference(authIdentityDb)
    .WithReference(authCache)
    .WithEnvironment("ConnectionStrings__authdb", authIdentityDb.Resource.ConnectionStringExpression)
    .WithEnvironment("ConnectionStrings__auth-cache", authCache.Resource.ConnectionStringExpression)
    .WithEnvironment("Auth__Authority", "http://keycloak:8080/realms/commerce")
    .WithEnvironment("Auth__Issuer", "http://keycloak:8080/realms/commerce")
    .WithEnvironment("Auth__DiscoveryUrl", "http://keycloak:8080/realms/commerce/.well-known/openid-configuration")
    .WithEnvironment("Auth__JwksUrl", "http://keycloak:8080/realms/commerce/protocol/openid-connect/certs")
    .WithEnvironment("Auth__Audience", "auth-api")
    .WithEnvironment("Auth__WebClientId", "web-app")
    .WithEnvironment("Auth__WebClientScope", "openid profile email")
    .WithEnvironment("Auth__WebClientSecret", authWebClientSecret)
    .WithEnvironment("DevBootstrap__Enabled", authDevBootstrapEnabled)
    .WithEnvironment("DevBootstrap__SharedSecret", authDevBootstrapSharedSecret)
    .WithEnvironment("Auth__PkceClients__0__ClientId", "web-app")
    .WithEnvironment("Auth__PkceClients__0__Scope", "openid profile email")
    .WithEnvironment("Auth__PkceClients__0__RedirectUris__0", "http://localhost:5173/auth/callback")
    .WithEnvironment("Auth__PkceClients__0__RedirectUris__1", "http://localhost:3000/auth/callback")
    .WaitFor(authIdentityDb)
    .WaitFor(authCache)
    .WaitFor(keycloak)
    .WithUrl("/swagger", "Swagger");

IResourceBuilder<ProjectResource> inventoryApi = builder.AddProject<Inventory_Api>("inventory-api")
    .WithReference(inventory)
    .WithEnvironment("ConnectionStrings__inventory", inventory.Resource.ConnectionStringExpression)
    .WithEnvironment("ConnectionStrings__inventorydb", inventory.Resource.ConnectionStringExpression)
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__Enabled", "true")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__Endpoint", "http://localhost:9200")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__AppIndexPrefix", "app-log")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__MessagingIndexPrefix", "messaging-log")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__AuditIndexPrefix", "audit-log")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__SecurityIndexPrefix", "security-log")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__PayloadIndexPrefix", "payload-log")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__UseDailyIndexes", "true")
    .WaitFor(inventory)
    .WaitFor(openSearch)
    .WithUrl("/swagger", "Swagger");

IResourceBuilder<ProjectResource> productsApi = builder.AddProject<Products_Api>("products-api")
    .WithReference(productDb)
    .WithReference(messaging)
    .WithReference(inventoryApi)
    .WithEnvironment("ConnectionStrings__productsdb", productDb.Resource.ConnectionStringExpression)
    .WithEnvironment("Auth__Authority", "http://keycloak:8080/realms/commerce")
    .WithEnvironment("Auth__Issuer", "http://keycloak:8080/realms/commerce")
    .WithEnvironment("Auth__Audience", "products-api")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__Enabled", "true")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__Endpoint", "http://localhost:9200")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__AppIndexPrefix", "app-log")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__MessagingIndexPrefix", "messaging-log")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__AuditIndexPrefix", "audit-log")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__SecurityIndexPrefix", "security-log")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__PayloadIndexPrefix", "payload-log")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__UseDailyIndexes", "true")
    .WaitFor(productDb)
    .WaitFor(messaging)
    .WaitFor(inventoryApi)
    .WaitFor(openSearch)
    .WithUrl("/swagger", "Swagger");

builder.AddProject<Cart_Api>("cart-api")
    .WithUrl("/openapi/v1.json", "OpenAPI");

builder.AddProject<Discount_Grpc>("discount-grpc");

builder.AddProject<Order_Api>("order-api")
    .WithUrl("/openapi/v1.json", "OpenAPI");

builder.AddProject<Gateway_Yarp>("gateway-yarp")
    .WithReference(productsApi)
    .WithEnvironment("Auth__Authority", "http://keycloak:8080/realms/commerce")
    .WithEnvironment("Auth__Issuer", "http://keycloak:8080/realms/commerce")
    .WithEnvironment("Auth__Audience", "gateway-yarp")
    .WithUrl("/openapi/v1.json", "OpenAPI");

openSearchDashboards.WithUrl("/", "OpenSearch Dashboards");

#endregion



builder.Build().Run();
