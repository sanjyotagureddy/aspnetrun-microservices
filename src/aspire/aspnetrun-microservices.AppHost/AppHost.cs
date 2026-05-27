using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// ReSharper disable once EmptyRegion
#region Persistence

IResourceBuilder<PostgresServerResource> postgresDb = builder.AddPostgres("productsdb")
        .WithDataVolume("postgres-data")
        .WithPgAdmin(containerName: "pgAdmin")
        .WithHostPort(5432);

IResourceBuilder<PostgresDatabaseResource> productDb = postgresDb.AddDatabase("products", "products");
IResourceBuilder<KafkaServerResource> messaging = builder.AddKafka("message-broker")
    .WithDataVolume("kafka-data")
    .WithKafkaUI();

#endregion
#region Services



builder.AddProject<Products_Api>("products-api")
    .WithReference(productDb)
    .WithReference(messaging)
    .WaitFor(productDb);

builder.AddProject<Cart_Api>("cart-api");

builder.AddProject<Discount_Grpc>("discount-grpc");

builder.AddProject<Order_Api>("order-api");

builder.AddProject<Gateway_Yarp>("gateway-yarp");

#endregion



builder.Build().Run();
