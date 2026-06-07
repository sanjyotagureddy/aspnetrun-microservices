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

IResourceBuilder<ContainerResource> otelCollector = builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib", "0.108.0")
    .WithBindMount("./otel-collector-config.yaml", "/etc/otelcol-contrib/config.yaml")
    .WithArgs("--config=/etc/otelcol-contrib/config.yaml")
    .WithHttpEndpoint(port: 4318, targetPort: 4318, name: "otlp-http")
    .WithEndpoint(port: 4317, targetPort: 4317, scheme: "tcp", name: "otlp-grpc")
    .WaitFor(openSearch);

IResourceBuilder<ProjectResource> logStoreApi = builder.AddProject<LogStore_Api>("log-store-api")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://otel-collector:4318")
    .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf")
    .WithEnvironment("LogStorage__OpenSearch__Endpoint", "http://localhost:9200")
    .WithEnvironment("LogStorage__OpenSearch__ApiIndexPrefix", "api-logs")
    .WithEnvironment("LogStorage__OpenSearch__InfraIndexPrefix", "infra-logs")
    .WithEnvironment("LogStorage__OpenSearch__MessagingIndexPrefix", "messaging-log")
    .WithEnvironment("LogStorage__OpenSearch__UseDailyIndexes", "true")
    .WaitFor(openSearch)
    .WaitFor(otelCollector)
    .WithUrl("/swagger", "Swagger");

#endregion
#region Services



IResourceBuilder<ProjectResource> inventoryApi = builder.AddProject<Inventory_Api>("inventory-api")
    .WithReference(inventory)
    .WithReference(logStoreApi)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://otel-collector:4318")
    .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf")
    .WithEnvironment("ConnectionStrings__inventory", inventory.Resource.ConnectionStringExpression)
    .WithEnvironment("ConnectionStrings__inventorydb", inventory.Resource.ConnectionStringExpression)
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__Enabled", "true")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__Endpoint", "http://localhost:9200")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__ApiIndexPrefix", "api-logs")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__InfraIndexPrefix", "infra-logs")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__MessagingIndexPrefix", "messaging-log")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__UseDailyIndexes", "true")
    .WithEnvironment("Logging__CommonSharedKernel__LogStore__Enabled", "true")
    .WithEnvironment("Logging__CommonSharedKernel__LogStore__Endpoint", "http://log-store-api")
    .WithEnvironment("Logging__CommonSharedKernel__LogStore__CreateRoutePath", "/api/v1/logs")
    .WaitFor(inventory)
    .WaitFor(logStoreApi)
    .WaitFor(openSearch)
    .WaitFor(otelCollector)
    .WithUrl("/swagger", "Swagger");

builder.AddProject<Products_Api>("products-api")
    .WithReference(productDb)
    .WithReference(messaging)
    .WithReference(logStoreApi)
    .WithReference(inventoryApi)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://otel-collector:4318")
    .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf")
    .WithEnvironment("ConnectionStrings__productsdb", productDb.Resource.ConnectionStringExpression)
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__Enabled", "true")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__Endpoint", "http://localhost:9200")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__ApiIndexPrefix", "api-logs")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__InfraIndexPrefix", "infra-logs")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__MessagingIndexPrefix", "messaging-log")
    .WithEnvironment("Logging__CommonSharedKernel__OpenSearch__UseDailyIndexes", "true")
    .WithEnvironment("Logging__CommonSharedKernel__LogStore__Enabled", "true")
    .WithEnvironment("Logging__CommonSharedKernel__LogStore__Endpoint", "http://log-store-api")
    .WithEnvironment("Logging__CommonSharedKernel__LogStore__CreateRoutePath", "/api/v1/logs")
    .WaitFor(productDb)
    .WaitFor(messaging)
    .WaitFor(logStoreApi)
    .WaitFor(inventoryApi)
    .WaitFor(openSearch)
    .WaitFor(otelCollector)
    .WithUrl("/swagger", "Swagger");

builder.AddProject<Cart_Api>("cart-api")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://otel-collector:4318")
    .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf")
    .WaitFor(otelCollector)
    .WithUrl("/openapi/v1.json", "OpenAPI");

builder.AddProject<Discount_Grpc>("discount-grpc")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://otel-collector:4318")
    .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf")
    .WaitFor(otelCollector);

builder.AddProject<Order_Api>("order-api")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://otel-collector:4318")
    .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf")
    .WaitFor(otelCollector)
    .WithUrl("/openapi/v1.json", "OpenAPI");

builder.AddProject<Gateway_Yarp>("gateway-yarp")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://otel-collector:4318")
    .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf")
    .WaitFor(otelCollector)
    .WithUrl("/openapi/v1.json", "OpenAPI");

openSearchDashboards.WithUrl("/", "OpenSearch Dashboards");

#endregion



builder.Build().Run();
