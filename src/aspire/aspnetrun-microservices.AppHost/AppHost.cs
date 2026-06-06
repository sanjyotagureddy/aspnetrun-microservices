using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// ReSharper disable once EmptyRegion
#region Persistence

IResourceBuilder<PostgresServerResource> postgresDb = builder.AddPostgres("productsdb")
        .WithDataVolume("postgres-data")
        .WithPgAdmin(containerName: "pgAdmin")
        .WithHostPort(5432);

IResourceBuilder<PostgresDatabaseResource> productDb = postgresDb.AddDatabase("products", "products");
IResourceBuilder<PostgresDatabaseResource> inventory = postgresDb.AddDatabase("inventory", "inventory");
IResourceBuilder<KafkaServerResource> messaging = builder.AddKafka("message-broker")
    .WithDataVolume("kafka-data")
    .WithKafkaUI();


#endregion
#region Services



IResourceBuilder<ProjectResource> inventoryApi = builder.AddProject<Inventory_Api>("inventory-api")
    .WithReference(inventory)
    .WaitFor(inventory)
    .WithUrl("/swagger", "Swagger");

builder.AddProject<Products_Api>("products-api")
    .WithReference(productDb)
    .WithReference(messaging)
    .WithReference(inventoryApi)
    .WaitFor(productDb)
    .WaitFor(messaging)
    .WaitFor(inventoryApi)
    .WithUrl("/swagger", "Swagger");

builder.AddProject<Cart_Api>("cart-api")
    .WithUrl("/openapi/v1.json", "OpenAPI");

builder.AddProject<Discount_Grpc>("discount-grpc");

builder.AddProject<Order_Api>("order-api")
    .WithUrl("/openapi/v1.json", "OpenAPI");

builder.AddProject<Gateway_Yarp>("gateway-yarp")
    .WithUrl("/openapi/v1.json", "OpenAPI");

#endregion



builder.Build().Run();
