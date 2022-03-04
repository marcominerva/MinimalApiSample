using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinimalApiSample.DataAccessLayer;
using MinimalApiSample.Extensions;
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

app.MapGet("/api/people", async ([FromQueryAttribute(Name = "q")] string searchText, DataContext dataContext) =>
{
    var query = dataContext.People.AsNoTracking().AsQueryable();

    if (!string.IsNullOrWhiteSpace(searchText))
    {
        query = query.Where(p => p.FirstName.Contains(searchText) || p.LastName.Contains(searchText));
    }

    var people = await query.OrderBy(p => p.FirstName).ThenBy(p => p.LastName)
        .Select(p => p.ToDto()).ToListAsync();

    return Results.Ok(people);
})
.WithName("GetPeople")
.Produces(StatusCodes.Status200OK, typeof(IEnumerable<Person>));

app.MapGet("/api/people/{id:guid}", async (Guid id, DataContext dataContext) =>
{
    var dbPerson = await dataContext.People.FindAsync(id);
    if (dbPerson is null)
    {
        return Results.NotFound();
    }

    var person = dbPerson.ToDto();
    return Results.Ok(person);
})
.WithName("GetPerson")
.Produces(StatusCodes.Status200OK, typeof(Person))
.Produces(StatusCodes.Status404NotFound);

app.MapPost("/api/people", async (Person person, DataContext dataContext) =>
{
    var dbPerson = new Entities.Person
    {
        FirstName = person.FirstName,
        LastName = person.LastName,
        City = person.City,
    };

    dataContext.People.Add(dbPerson);
    await dataContext.SaveChangesAsync();

    return Results.CreatedAtRoute("GetPerson", new { dbPerson.Id }, dbPerson.ToDto());
})
.WithName("InsertPerson")
.Produces(StatusCodes.Status201Created, typeof(Person))
.ProducesValidationProblem();

app.MapPut("/api/people/{id:guid}", async (Guid id, Person person, DataContext dataContext) =>
{
    if (id != person.Id)
    {
        return Results.BadRequest();
    }

    var dbPerson = await dataContext.People.FindAsync(id);
    if (dbPerson is null)
    {
        return Results.NotFound();
    }

    dbPerson.FirstName = person.FirstName;
    dbPerson.LastName = person.LastName;
    dbPerson.City = person.City;

    await dataContext.SaveChangesAsync();

    return Results.NoContent();
})
.WithName("UpdatePerson")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound)
.ProducesValidationProblem();

app.MapDelete("/api/people/{id:guid}", async (Guid id, DataContext dataContext) =>
{
    var dbPerson = await dataContext.People.FindAsync(id);
    if (dbPerson is null)
    {
        return Results.NotFound();
    }

    dataContext.People.Remove(dbPerson);
    await dataContext.SaveChangesAsync();

    return Results.NoContent();
})
.WithName("DeletePerson")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

app.MapGet("/api/people/{id:guid}/photo", async (Guid id, DataContext dataContext) =>
{
    var dbPerson = await dataContext.People.FindAsync(id);
    if (dbPerson?.Photo is null)
    {
        return Results.NotFound();
    }

    return Results.Bytes(dbPerson.Photo, "image/jpeg");
})
.WithName("GetPhoto")
.Produces(StatusCodes.Status200OK, contentType: "image/jpeg")
.Produces(StatusCodes.Status400BadRequest, typeof(ProblemDetails))
.Produces(StatusCodes.Status404NotFound, typeof(ProblemDetails));

app.MapPut("/api/people/{id:guid}/photo", async (Guid id, HttpRequest request, DataContext dataContext) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest();
    }

    var form = await request.ReadFormAsync();
    var file = form.Files.FirstOrDefault();

    if (file is null)
    {
        return Results.BadRequest();
    }

    var dbPerson = await dataContext.People.FindAsync(id);
    if (dbPerson is null)
    {
        return Results.NotFound();
    }

    using var stream = file.OpenReadStream();
    using var photoStream = new MemoryStream();
    await stream.CopyToAsync(photoStream);

    dbPerson.Photo = photoStream.ToArray();
    await dataContext.SaveChangesAsync();

    return Results.NoContent();
})
.WithName("UpdatePhoto")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status404NotFound);

app.MapDelete("/api/people/{id:guid}/photo", async (Guid id, DataContext dataContext) =>
{
    var dbPerson = await dataContext.People.FindAsync(id);
    if (dbPerson is null)
    {
        return Results.NotFound();
    }

    dbPerson.Photo = null;
    await dataContext.SaveChangesAsync();

    return Results.NoContent();
})
.WithName("DeletePhoto")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

app.MapGet("/api/products", async ([FromQueryAttribute(Name = "q")] string searchText, DataContext dataContext) =>
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