using Catalog.API.Domain.Entities;
using Catalog.API.Infrastructure.Persistence.Models;
using FluentAssertions;
using Xunit;

namespace Catalog.API.Test.Infrastructure;

public class ProductDocumentTests
{
    [Fact]
    public void FromDomain_And_ToDomain_RoundTripsProductValues()
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Name",
            Category = "Category",
            Summary = "Summary",
            Description = "Description",
            ImageFile = "image.png",
            Price = 42.5m
        };

        var document = ProductDocument.FromDomain(product);
        var roundTrip = document.ToDomain();

        document.Id.Should().Be(product.Id);
        document.Name.Should().Be(product.Name);
        roundTrip.Should().BeEquivalentTo(product);
    }
}