using Catalog.API.Domain.Entities;
using Catalog.API.Infrastructure.Persistence;
using Catalog.API.Infrastructure.Persistence.Models;
using Catalog.API.Infrastructure.Persistence.Repositories;
using Catalog.API.Test.TestHelpers;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Catalog.API.Test.Infrastructure;

public class ProductRepositoryTests
{
    [Fact]
    public async Task GetProducts_ReturnsAllProducts()
    {
        var products = CreateDocuments();
        var collection = CreateCollection();
        MongoMockHelpers.SetupFind(collection, products);

        var repository = CreateRepository(collection.Object);

        var result = await repository.GetProducts(CancellationToken.None);

        result.Should().HaveCount(products.Count);
    }

    [Fact]
    public async Task GetProductsByName_ReturnsMatchingProducts()
    {
        var products = CreateDocuments();
        var collection = CreateCollection();
        MongoMockHelpers.SetupFind(collection, products.Where(product => product.Name == "Name-1").ToList());

        var repository = CreateRepository(collection.Object);

        var result = await repository.GetProductsByName("Name-1", CancellationToken.None);

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task GetProductsByCategory_ReturnsMatchingProducts()
    {
        var products = CreateDocuments();
        var collection = CreateCollection();
        MongoMockHelpers.SetupFind(collection, products.Where(product => product.Category == "Category-1").ToList());

        var repository = CreateRepository(collection.Object);

        var result = await repository.GetProductsByCategory("Category-1", CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetProduct_ReturnsProductWhenFound()
    {
        var documents = CreateDocuments();
        var collection = CreateCollection();
        var target = documents[0];
        MongoMockHelpers.SetupFind(collection, [target]);

        var repository = CreateRepository(collection.Object);

        var result = await repository.GetProduct(target.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(target.ToDomain());
    }

    [Fact]
    public async Task GetProduct_ReturnsNullWhenMissing()
    {
        var collection = CreateCollection();
        MongoMockHelpers.SetupFind(collection, Array.Empty<ProductDocument>());

        var repository = CreateRepository(collection.Object);

        var result = await repository.GetProduct(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateProduct_InsertsDocument()
    {
        var collection = CreateCollection();
        ProductDocument? capturedDocument = null;
        collection.Setup(c => c.InsertOneAsync(It.IsAny<ProductDocument>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
            .Callback<ProductDocument, InsertOneOptions?, CancellationToken>((document, _, _) => capturedDocument = document)
            .Returns(Task.CompletedTask);

        var repository = CreateRepository(collection.Object);
        var product = new Product { Id = Guid.NewGuid(), Name = "Name", Category = "Category" };

        await repository.CreateProduct(product, CancellationToken.None);

        capturedDocument.Should().NotBeNull();
        capturedDocument!.Id.Should().Be(product.Id);
        capturedDocument.Name.Should().Be(product.Name);
    }

    [Fact]
    public async Task UpdateProduct_ReturnsTrueWhenUpdateSucceeds()
    {
        var collection = CreateCollection();
        var result = new Mock<ReplaceOneResult>();
        result.SetupGet(update => update.IsAcknowledged).Returns(true);
        result.SetupGet(update => update.ModifiedCount).Returns(1);
        collection.Setup(c => c.ReplaceOneAsync(It.IsAny<FilterDefinition<ProductDocument>>(), It.IsAny<ProductDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result.Object);

        var repository = CreateRepository(collection.Object);

        var success = await repository.UpdateProduct(new Product { Id = Guid.NewGuid(), Name = "Name" }, CancellationToken.None);

        success.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateProduct_ReturnsFalseWhenNoDocumentsAreModified()
    {
        var collection = CreateCollection();
        var result = new Mock<ReplaceOneResult>();
        result.SetupGet(update => update.IsAcknowledged).Returns(true);
        result.SetupGet(update => update.ModifiedCount).Returns(0);
        collection.Setup(c => c.ReplaceOneAsync(It.IsAny<FilterDefinition<ProductDocument>>(), It.IsAny<ProductDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result.Object);

        var repository = CreateRepository(collection.Object);

        var success = await repository.UpdateProduct(new Product { Id = Guid.NewGuid(), Name = "Name" }, CancellationToken.None);

        success.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteProduct_ReturnsTrueWhenDeleteSucceeds()
    {
        var collection = CreateCollection();
        var result = new Mock<DeleteResult>();
        result.SetupGet(delete => delete.IsAcknowledged).Returns(true);
        result.SetupGet(delete => delete.DeletedCount).Returns(1);
        collection.Setup(c => c.DeleteOneAsync(It.IsAny<FilterDefinition<ProductDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result.Object);

        var repository = CreateRepository(collection.Object);

        var success = await repository.DeleteProduct(Guid.NewGuid(), CancellationToken.None);

        success.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteProduct_ReturnsFalseWhenDeleteDoesNothing()
    {
        var collection = CreateCollection();
        var result = new Mock<DeleteResult>();
        result.SetupGet(delete => delete.IsAcknowledged).Returns(true);
        result.SetupGet(delete => delete.DeletedCount).Returns(0);
        collection.Setup(c => c.DeleteOneAsync(It.IsAny<FilterDefinition<ProductDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result.Object);

        var repository = CreateRepository(collection.Object);

        var success = await repository.DeleteProduct(Guid.NewGuid(), CancellationToken.None);

        success.Should().BeFalse();
    }

    private static ProductRepository CreateRepository(IMongoCollection<ProductDocument> collection)
    {
        var context = new Mock<ICatalogContext>();
        context.SetupGet(c => c.Products).Returns(collection);
        return new ProductRepository(context.Object);
    }

    private static Mock<IMongoCollection<ProductDocument>> CreateCollection()
    {
        var collection = new Mock<IMongoCollection<ProductDocument>>();
        collection.SetupGet(c => c.CollectionNamespace).Returns(new CollectionNamespace(new DatabaseNamespace("db"), "products"));
        collection.SetupGet(c => c.DocumentSerializer).Returns(BsonSerializer.SerializerRegistry.GetSerializer<ProductDocument>());
        collection.SetupGet(c => c.Settings).Returns(new MongoCollectionSettings());

        return collection;
    }

    private static List<ProductDocument> CreateDocuments()
    {
        return
        [
            new ProductDocument { Id = Guid.NewGuid(), Name = "Name-1", Category = "Category-1" },
            new ProductDocument { Id = Guid.NewGuid(), Name = "Name-2", Category = "Category-1" }
        ];
    }
}