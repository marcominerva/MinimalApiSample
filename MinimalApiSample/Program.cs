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
    var query = dataContext.People.AsQueryable();

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

app.Run();

static async Task ConfigureDatabaseAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DataContext>();
    await db.Database.MigrateAsync();
}