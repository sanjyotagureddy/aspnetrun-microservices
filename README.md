# Introduction  [![Build Status](https://dev.azure.com/sanjyotagureddy/aspnetrun-microservices/_apis/build/status/aspnetrun-microservices%20-%20master%20branch?branchName=master)](https://dev.azure.com/sanjyotagureddy/aspnetrun-microservices/_build/latest?definitionId=15&branchName=master)
Building Microservices on .Net Ecosystem 

Used Asp.Net Web API, Docker, RabbitMQ, MassTransit, Grpc, Ocelot API Gateway, MongoDB, Redis, PostgreSQL, SqlServer, Dapper, Entity Framework Core, CQRS and Clean Architecture implementation. Also includes Cross-Cutting concerns like Implementing Centralized Distributed Logging with Elasticsearch, Kibana and SeriLog, use the HealthChecks with Watchdog, Implement Retry and Circuit Breaker patterns with Polly and so on..

**Catalog Microservice**
- ASP.NET Web API application
- REST API principles, CRUD operations
- MongoDB database connection and containerization
- Repository Pattern Implementation
- Swagger Open API implementation

**Basket Microservice**
- ASP.NET Web API application
- REST API principles, CRUD operations
- Redis database connection and containerization
- Consume Discount Grpc Service for inter-service sync communication to calculate product final price
- Publish BasketCheckout Queue with using MassTransit and RabbitMQ

**Discount Microservice**
- ASP.NET Grpc Server application
- Build a Highly Performant inter-service gRPC Communication with Basket Microservice
- Exposing Grpc Services with creating Protobuf messages
- Using Dapper for micro-orm implementation to simplify data access and ensure high performance
- PostgreSQL database connection and containerization

**Microservices Communication**
- Sync inter-service gRPC Communication
- Async Microservices Communication with RabbitMQ Message-Broker Service
- Using RabbitMQ Publish/Subscribe Topic Exchange Model
- Using MassTransit for abstraction over RabbitMQ Message-Broker system
- Publishing BasketCheckout event queue from Basket microservices and Subscribing this event from Ordering microservices
- Create RabbitMQ EventBus.Messages library and add references Microservices

**Ordering Microservice**
- Implementing DDD, CQRS, and Clean Architecture with using Best Practices
- Developing CQRS with using MediatR, FluentValidation and AutoMapper packages
- Consuming RabbitMQ BasketCheckout event queue with using MassTransit-RabbitMQ Configuration
- SqlServer database connection and containerization
- Using Entity Framework Core ORM and auto migrate to SqlServer when application startup

**API Gateway Ocelot Microservice**
- Implement API Gateways with Ocelot
- Sample microservices/containers to reroute through the API Gateways
- Run multiple different API Gateway/BFF container types
- The Gateway aggregation pattern in Shopping.Aggregator

**WebUI ShoppingApp Microservice**
- ASP.NET Core Web Application with Bootstrap 4 and Razor template
- Call Ocelot APIs with HttpClientFactory and Polly

**Microservices Cross-Cutting Implementations**
- Implementing Centralized Distributed Logging with Elastic Stack (ELK); Elasticsearch, Logstash, Kibana and SeriLog for Microservices
- Use the HealthChecks feature in back-end ASP.NET microservices
- Using Watchdog in separate service that can watch health and load across services, and report health about the microservices by querying with the HealthChecks

**Microservices Resilience Implementations**
- Making Microservices more resilient Use IHttpClientFactory to implement resilient HTTP requests
- Implement Retry and Circuit Breaker patterns with exponential backoff with IHttpClientFactory and Polly policies

**Ancillary Containers**
- Use Portainer for Container lightweight management UI which allows you to easily manage your different Docker environments
- pgAdmin PostgreSQL Tools feature rich Open Source administration and development platform for PostgreSQL

**Docker Compose establishment with all microservices on docker**
- Containerization of microservices
- Containerization of databases
- Override Environment variables


# Getting Started
TODO: Guide users through getting your code up and running on their own system. In this section you can talk about:
1.	Installation process
    ### Installing
Follow these steps to get your development environment set up: (Before Run Start the Docker Desktop)
1. Clone the repository
2. Once Docker for Windows is installed, go to the **Settings > Advanced option**, from the Docker icon in the system tray, to configure the minimum amount of memory and CPU like so:
* **Memory: 4 GB**
* CPU: 2
3. At the root directory which include **docker-compose.yml** files, run below command:
```csharp
docker-compose -f docker-compose.yml -f docker-compose.override.yml up -d
```
3. Wait for docker compose all microservices. That’s it! (some microservices need extra time to work so please wait if not worked in first shut)

4. You can **launch microservices** as below urls:

* **Catalog API -> http://host.docker.internal:8000/swagger/index.html**
* **Basket API -> http://host.docker.internal:8001/swagger/index.html**
* **Discount API -> http://host.docker.internal:8002/swagger/index.html**
* **Ordering API -> http://host.docker.internal:8004/swagger/index.html**
* **Shopping.Aggregator -> http://host.docker.internal:8005/swagger/index.html**
* **API Gateway -> http://host.docker.internal:8010/Catalog**
* **Rabbit Management Dashboard -> http://host.docker.internal:15672**   -- guest/guest
* **Portainer -> http://host.docker.internal:9000**   -- admin/admin1234
* **pgAdmin PostgreSQL -> http://host.docker.internal:5050**   -- admin@aspnetrun.com/admin1234
* **Elasticsearch -> http://host.docker.internal:9200** -- To Be Develop
* **Kibana -> http://host.docker.internal:5601** -- To Be Develop

* **Web Status -> http://host.docker.internal:8007** -- To Be Develop
* **Web UI -> http://host.docker.internal:8006**

5. Launch http://host.docker.internal:8007 in your browser to view the Web Status. Make sure that every microservices are healthy.
6. Launch http://host.docker.internal:8006 in your browser to view the Web UI. You can use Web project in order to **call microservices over API Gateway**. When you **checkout the basket** you can follow **queue record on RabbitMQ dashboard**.

>Note: If you are running this application in macOS then use `docker.for.mac.localhost` as DNS name in `.env` file and the above URLs instead of `host.docker.internal`.
2.	Software dependencies
    * [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/)
    * [.Net Core 5 or later](https://dotnet.microsoft.com/download/dotnet-core/5)
    * [Docker Desktop](https://www.docker.com/products/docker-desktop)
3.	Latest releases
4.	API references

# Build and Test
TODO: Describe and show how to build your code and run the tests. 

