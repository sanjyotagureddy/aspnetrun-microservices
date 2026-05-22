using Catalog.API.Infrastructure.Persistence.Models;
using MongoDB.Driver;
using Moq;

namespace Catalog.API.Test.TestHelpers;

internal static class MongoMockHelpers
{
    public static void SetupFind<T>(Mock<IMongoCollection<T>> collection, IReadOnlyCollection<T> items)
    {
        var cursor = CreateCursor(items);

        collection.Setup(c => c.FindAsync<T>(It.IsAny<FilterDefinition<T>>(), It.IsAny<FindOptions<T, T>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursor.Object);

        collection.Setup(c => c.FindSync<T>(It.IsAny<FilterDefinition<T>>(), It.IsAny<FindOptions<T, T>>(), It.IsAny<CancellationToken>()))
            .Returns(cursor.Object);
    }

    public static Mock<IAsyncCursor<T>> CreateCursor<T>(IReadOnlyCollection<T> items)
    {
        var cursor = new Mock<IAsyncCursor<T>>();
        var hasItems = items.Count > 0;

        cursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(hasItems)
            .Returns(false);
        cursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(hasItems)
            .ReturnsAsync(false);
        cursor.SetupGet(c => c.Current).Returns(items);

        return cursor;
    }

    public static Mock<IFindFluent<ProductDocument, ProductDocument>> CreateFindFluent(IReadOnlyCollection<ProductDocument> items)
    {
        var cursor = CreateCursor(items);
        var findFluent = new Mock<IFindFluent<ProductDocument, ProductDocument>>();

        findFluent.Setup(f => f.ToCursor(It.IsAny<CancellationToken>())).Returns(cursor.Object);
        findFluent.Setup(f => f.ToCursorAsync(It.IsAny<CancellationToken>())).ReturnsAsync(cursor.Object);

        return findFluent;
    }
}