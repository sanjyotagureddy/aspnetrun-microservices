using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

#region Services

builder.AddProject<Products_Api>("products-api");

builder.AddProject<Cart_Api>("cart-api");

builder.AddProject<Discount_Grpc>("discount-grpc");

builder.AddProject<Order_Api>("order-api");

builder.AddProject<Gateway_Yarp>("gateway-yarp");

#endregion

// ReSharper disable once EmptyRegion
#region Persistence



#endregion

builder.Build().Run();
