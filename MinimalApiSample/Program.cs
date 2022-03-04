using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinimalApiSample.DataAccessLayer;
using MinimalApiSample.Extensions;
using MinimalApiSample.Handlers;
using MinimalApiSample.Models;
using Entities = MinimalApiSample.DataAccessLayer.Entities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSqlServer<DataContext>(builder.Configuration.GetConnectionString("SqlConnection"));

var app = builder.Build();
await ConfigureDatabaseAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var peopleHandler = new PeopleHandler();
peopleHandler.MapEndpoints(app);

app.MapGet("/api/products", async ([FromQuery(Name = "q")] string searchText, DataContext dataContext) =>
{
    var query = dataContext.Products.AsNoTracking().AsQueryable();

    if (!string.IsNullOrWhiteSpace(searchText))
    {
        query = query.Where(p => p.Name.Contains(searchText));
    }

    var people = await query.OrderBy(p => p.Name)
        .Select(p => p.ToDto()).ToListAsync();

    return Results.Ok(people);
})
.WithName("GetProducts")
.Produces(StatusCodes.Status200OK, typeof(IEnumerable<Product>));

app.MapGet("/api/products/{id:guid}", async (Guid id, DataContext dataContext) =>
{
    var dbProduct = await dataContext.Products.FindAsync(id);
    if (dbProduct is null)
    {
        return Results.NotFound();
    }

    var person = dbProduct.ToDto();
    return Results.Ok(person);
})
.WithName("GetProduct")
.Produces(StatusCodes.Status200OK, typeof(Product))
.Produces(StatusCodes.Status404NotFound);

app.MapPost("/api/products", async (Product product, DataContext dataContext) =>
{
    var dbProduct = new Entities.Product
    {
        Name = product.Name,
        Price = product.Price
    };

    dataContext.Products.Add(dbProduct);
    await dataContext.SaveChangesAsync();

    return Results.CreatedAtRoute("GetProduct", new { dbProduct.Id }, dbProduct.ToDto());
})
.WithName("InsertProduct")
.Produces(StatusCodes.Status201Created, typeof(Product))
.ProducesValidationProblem();

app.MapPut("/api/products/{id:guid}", async (Guid id, Product product, DataContext dataContext) =>
{
    if (id != product.Id)
    {
        return Results.BadRequest();
    }

    var dbProduct = await dataContext.Products.FindAsync(id);
    if (dbProduct is null)
    {
        return Results.NotFound();
    }

    dbProduct.Name = product.Name;
    dbProduct.Price = product.Price;

    await dataContext.SaveChangesAsync();

    return Results.NoContent();
})
.WithName("UpdateProducts")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound)
.ProducesValidationProblem();

app.MapDelete("/api/products/{id:guid}", async (Guid id, DataContext dataContext) =>
{
    var dbProduct = await dataContext.Products.FindAsync(id);
    if (dbProduct is null)
    {
        return Results.NotFound();
    }

    dataContext.Products.Remove(dbProduct);
    await dataContext.SaveChangesAsync();

    return Results.NoContent();
})
.WithName("DeleteProduct")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

app.Run();

static async Task ConfigureDatabaseAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DataContext>();
    await db.Database.MigrateAsync();
}