using FluentValidation;
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

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddSqlServer<DataContext>(builder.Configuration.GetConnectionString("SqlConnection"));

var app = builder.Build();
await ConfigureDatabaseAsync(app.Services);

app.UseHttpsRedirection();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/api/people", async (string firstName, string lastName, string city, DataContext dataContext) =>
{
    var query = dataContext.People.AsNoTracking().AsQueryable();

    if (!string.IsNullOrWhiteSpace(firstName))
    {
        query = query.Where(p => p.FirstName.Contains(firstName));
    }

    if (!string.IsNullOrWhiteSpace(lastName))
    {
        query = query.Where(p => p.LastName.Contains(lastName));
    }

    if (!string.IsNullOrWhiteSpace(city))
    {
        query = query.Where(p => p.City.Contains(city));
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

app.MapPost("/api/people", async (Person person, DataContext dataContext, IValidator<Person> validator) =>
{
    //if (!MiniValidator.TryValidate(person, out var errors))
    //{
    //    return Results.ValidationProblem(errors);
    //}

    var validationResult = await validator.ValidateAsync(person);
    if (!validationResult.IsValid)
    {
        return Results.ValidationProblem(validationResult.ToDictionary());
    }

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

app.MapPut("/api/people/{id:guid}", async (Guid id, Person person, DataContext dataContext, IValidator<Person> validator) =>
{
    //if (!MiniValidator.TryValidate(person, out var errors))
    //{
    //    return Results.ValidationProblem(errors);
    //}

    var validationResult = await validator.ValidateAsync(person);
    if (!validationResult.IsValid)
    {
        return Results.ValidationProblem(validationResult.ToDictionary());
    }

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

app.MapPut("/api/people/{id:guid}/photo", async (Guid id, IFormFile file, DataContext dataContext) =>
{
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

app.Run();

static async Task ConfigureDatabaseAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DataContext>();
    await db.Database.MigrateAsync();
}