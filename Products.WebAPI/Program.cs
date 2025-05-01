using Bogus;
using Microsoft.EntityFrameworkCore;
using Products.WebAPI.Context;
using Products.WebAPI.Dtos;
using Products.WebAPI.Models;
using TS.Result;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
});

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/seedData", async (ApplicationDbContext context) =>
{
    for (int i = 0; i <= 100; i++)
    {
        Faker faker = new();
        Product product = new()
        {
            Name = faker.Commerce.ProductName(),
            Price = Convert.ToDecimal(faker.Commerce.Price()),
            Stock = faker.Random.Int(1, 100)
        };
        context.Products.Add(product);
    }
    context.SaveChanges();
    return Results.Ok(Result<string>.Succeed("Seed data başarıyla çalıştırıldı ve Ürünler bsşarıyla oluşturuldu"));
});

app.MapGet("/getall", async (ApplicationDbContext context, CancellationToken cancellationToken) =>
{
    var products = await context.Products.OrderBy(p => p.Name).ToListAsync(cancellationToken);
    Result<List<Product>> response = products;
    return response;
});

app.MapPost("/create", async (CreateProductDto request, ApplicationDbContext context,
    CancellationToken cancellationToken) =>
{
    bool isNameExists = await context.Products.AnyAsync(p => p.Name == request.Name, cancellationToken);
    if (isNameExists)
    {
        var response = Result<string>.Failure("Ürün adı zaten mevcut");
        return Results.BadRequest(response);
    }
    Product product = new()
    {
        Name = request.Name,
        Price = request.Price,
        Stock = request.Stock
    };
    await context.Products.AddAsync(product, cancellationToken);
    await context.SaveChangesAsync(cancellationToken);

    return Results.Ok(Result<string>.Succeed("Ürün başarıyla oluşturuldu"));
});

using (var scope = app.Services.CreateScope())
{
    var srv = scope.ServiceProvider;
    var context = srv.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}

app.Run();
