using Catalog.API.Domain.Entities;
using Catalog.API.Infrastructure.Persistence.Models;
using MongoDB.Driver;

namespace Catalog.API.Infrastructure.Persistence;

internal static class CatalogContextSeed
{
    public static void SeedData(IMongoCollection<ProductDocument> productCollection)
    {
        var existProduct = productCollection.CountDocuments(Builders<ProductDocument>.Filter.Empty) > 0;
        if (!existProduct)
        {
            productCollection.InsertMany(GetPreconfiguredProducts().Select(ProductDocument.FromDomain));
        }
    }

    private static IEnumerable<Product> GetPreconfiguredProducts()
    {
        return new List<Product>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Apple iPhone 15 Pro",
                Summary = "Premium smartphone with titanium design, advanced camera system, and all-day battery life.",
                Description =
                    "Apple iPhone 15 Pro features a durable titanium frame, A17 Pro performance, Pro camera capabilities, and USB-C charging for professionals who want top-tier mobile productivity.",
                ImageFile = "product-1.png",
                Price = 999.00M,
                Category = "Smartphones"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Samsung Galaxy S24",
                Summary = "Flagship Android phone with Galaxy AI, vivid display, and excellent battery efficiency.",
                Description =
                    "Samsung Galaxy S24 combines a bright AMOLED display, powerful performance, and AI-assisted productivity features for everyday users and power users alike.",
                ImageFile = "product-2.png",
                Price = 899.00M,
                Category = "Smartphones"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Google Pixel 8 Pro",
                Summary = "Clean Android experience with an advanced camera and smart AI-powered tools.",
                Description =
                    "Google Pixel 8 Pro is built for users who value photography, on-device intelligence, and fast access to the latest Android features and security updates.",
                ImageFile = "product-3.png",
                Price = 799.00M,
                Category = "Smartphones"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Dell XPS 13 Laptop",
                Summary = "Ultraportable laptop designed for productivity, travel, and premium build quality.",
                Description =
                    "Dell XPS 13 delivers a compact aluminum chassis, sharp display, fast SSD storage, and reliable performance for work, study, and remote collaboration.",
                ImageFile = "product-4.png",
                Price = 1199.00M,
                Category = "Laptops"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Apple MacBook Air M3",
                Summary = "Lightweight laptop with silent performance, long battery life, and great everyday speed.",
                Description =
                    "MacBook Air M3 is ideal for students and professionals who need a thin, fanless laptop with strong battery life and smooth performance for daily tasks.",
                ImageFile = "product-5.png",
                Price = 1099.00M,
                Category = "Laptops"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Sony WH-1000XM5 Headphones",
                Summary = "Industry-leading noise-canceling headphones with rich sound and comfortable fit.",
                Description =
                    "Sony WH-1000XM5 offers premium active noise cancellation, clear calls, and long listening sessions for commuting, office work, and travel.",
                ImageFile = "product-6.png",
                Price = 399.00M,
                Category = "Audio"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Bose QuietComfort Earbuds II",
                Summary = "Compact wireless earbuds with strong noise cancellation and balanced audio.",
                Description =
                    "Bose QuietComfort Earbuds II provide comfortable all-day wear, customizable fit, and dependable sound quality for music and calls.",
                ImageFile = "product-7.png",
                Price = 299.00M,
                Category = "Audio"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Nintendo Switch OLED",
                Summary = "Versatile gaming console with vibrant display and flexible handheld or docked play.",
                Description =
                    "Nintendo Switch OLED features a bright 7-inch display, improved audio, and the flexibility to switch between handheld, tabletop, and TV modes.",
                ImageFile = "product-8.png",
                Price = 349.00M,
                Category = "Gaming"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Dyson V15 Detect Vacuum",
                Summary = "Cordless vacuum with laser dust detection and strong suction for deep cleaning.",
                Description =
                    "Dyson V15 Detect is designed for homes that need powerful cordless cleaning with intelligent dust detection and multiple attachment options.",
                ImageFile = "product-9.png",
                Price = 749.00M,
                Category = "Home Appliances"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Nespresso Vertuo Plus",
                Summary = "Single-serve coffee machine for rich espresso and coffee drinks at home.",
                Description =
                    "Nespresso Vertuo Plus makes barista-style coffee with easy capsule brewing, fast heat-up time, and a compact design for kitchens and offices.",
                ImageFile = "product-10.png",
                Price = 199.00M,
                Category = "Kitchen"
            }
        };
    }
}