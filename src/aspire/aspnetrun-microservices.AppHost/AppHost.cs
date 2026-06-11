using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);


// ReSharper disable once EmptyRegion
#region Persistence

IResourceBuilder<ParameterResource> postgresPassword = builder.AddParameter("postgres-password", secret: true);
IResourceBuilder<ParameterResource> openSearchInitialAdminPassword = builder.AddParameter("opensearch-initial-admin-password", secret: true);

IResourceBuilder<PostgresServerResource> postgresDb = builder.AddPostgres("productsdb", password: postgresPassword)
        .WithDataVolume("postgres-data")
        .WithPgAdmin(containerName: "pgAdmin")
        .WithHostPort(5432);

IResourceBuilder<PostgresDatabaseResource> productDb = postgresDb.AddDatabase("products", "products");
IResourceBuilder<PostgresDatabaseResource> inventory = postgresDb.AddDatabase("inventory", "inventory");
IResourceBuilder<KafkaServerResource> messaging = builder.AddKafka("message-broker")
    .WithDataVolume("kafka-data")
    .WithKafkaUI();

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



IResourceBuilder<ProjectResource> inventoryApi = builder.AddProject<Inventory_Api>("inventory-api")
    .WithReference(inventory)
    .WithEnvironment("ConnectionStrings__inventory", inventory.Resource.ConnectionStringExpression)
    .WithEnvironment("ConnectionStrings__inventorydb", inventory.Resource.ConnectionStringExpression)
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__Enabled", "true")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__Endpoint", "http://localhost:9200")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__ApiIndexPrefix", "api-logs")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__InfraIndexPrefix", "infra-logs")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__MessagingIndexPrefix", "messaging-log")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__UseDailyIndexes", "true")
    .WaitFor(inventory)
    .WaitFor(openSearch)
    .WithUrl("/swagger", "Swagger");

builder.AddProject<Products_Api>("products-api")
    .WithReference(productDb)
    .WithReference(messaging)
    .WithReference(inventoryApi)
    .WithEnvironment("ConnectionStrings__productsdb", productDb.Resource.ConnectionStringExpression)
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__Enabled", "true")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__Endpoint", "http://localhost:9200")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__ApiIndexPrefix", "api-logs")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__InfraIndexPrefix", "infra-logs")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__MessagingIndexPrefix", "messaging-log")
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
    .WithUrl("/openapi/v1.json", "OpenAPI");

openSearchDashboards.WithUrl("/", "OpenSearch Dashboards");

#endregion



builder.Build().Run();
