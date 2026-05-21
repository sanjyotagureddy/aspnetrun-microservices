using Catalog.API.Infrastructure.Persistence;
using Catalog.API.Infrastructure.Persistence.Models;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Catalog.API.Test;

public class CatalogContextSeedTests
{
    [Fact]
    public void SeedData_WhenCollectionIsEmpty_InsertsPreconfiguredProducts()
    {
        var collection = CreateCollection();
        collection.Setup(c => c.CountDocuments(It.IsAny<FilterDefinition<ProductDocument>>(), It.IsAny<CountOptions>(), It.IsAny<CancellationToken>()))
            .Returns(0);
        IReadOnlyCollection<ProductDocument>? insertedDocuments = null;
        collection.Setup(c => c.InsertMany(It.IsAny<IEnumerable<ProductDocument>>(), It.IsAny<InsertManyOptions>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ProductDocument>, InsertManyOptions?, CancellationToken>((documents, _, _) => insertedDocuments = documents.ToList())
            .Verifiable();

        CatalogContextSeed.SeedData(collection.Object);

        insertedDocuments.Should().NotBeNull();
        insertedDocuments.Should().HaveCount(10);
        collection.Verify();
    }

    [Fact]
    public void SeedData_WhenCollectionAlreadyHasProducts_DoesNotInsert()
    {
        var collection = CreateCollection();
        collection.Setup(c => c.CountDocuments(It.IsAny<FilterDefinition<ProductDocument>>(), It.IsAny<CountOptions>(), It.IsAny<CancellationToken>()))
            .Returns(1);

        CatalogContextSeed.SeedData(collection.Object);

        collection.Verify(c => c.InsertMany(It.IsAny<IEnumerable<ProductDocument>>(), It.IsAny<InsertManyOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<IMongoCollection<ProductDocument>> CreateCollection()
    {
        var collection = new Mock<IMongoCollection<ProductDocument>>();
        collection.SetupGet(c => c.CollectionNamespace).Returns(new CollectionNamespace(new DatabaseNamespace("db"), "products"));
        collection.SetupGet(c => c.DocumentSerializer).Returns(BsonSerializer.SerializerRegistry.GetSerializer<ProductDocument>());
        collection.SetupGet(c => c.Settings).Returns(new MongoCollectionSettings());
        return collection;
    }
}