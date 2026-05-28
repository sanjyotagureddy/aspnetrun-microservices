using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// ReSharper disable once EmptyRegion
#region Persistence

IResourceBuilder<PostgresServerResource> postgresDb = builder.AddPostgres("productsdb")
        .WithDataVolume("postgres-data")
        .WithPgAdmin(containerName: "pgAdmin")
        .WithHostPort(5432);

IResourceBuilder<PostgresDatabaseResource> productDb = postgresDb.AddDatabase("products", "products");

#endregion
#region Services



builder.AddProject<Products_Api>("products-api")
    .WithReference(productDb)
    .WaitFor(productDb);

builder.AddProject<Cart_Api>("cart-api");

builder.AddProject<Discount_Grpc>("discount-grpc");

builder.AddProject<Order_Api>("order-api");

builder.AddProject<Gateway_Yarp>("gateway-yarp");

#endregion



builder.Build().Run();
