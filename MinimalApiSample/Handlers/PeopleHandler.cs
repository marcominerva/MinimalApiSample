using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinimalApiSample.DataAccessLayer;
using MinimalApiSample.Extensions;
using MinimalApiSample.Models;
using MinimalHelpers.Routing;
using Entities = MinimalApiSample.DataAccessLayer.Entities;

namespace MinimalApiSample.Handlers;

public class PeopleHandler : IEndpointRouteHandler
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/people", GetListAsync)
            .WithName("GetPeople")
            .Produces(StatusCodes.Status200OK, typeof(IEnumerable<Person>));

        app.MapGet("/api/people/{id:guid}", GetAsync)
            .WithName("GetPerson")
            .Produces(StatusCodes.Status200OK, typeof(Person))
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/people", InsertAsync)
            .WithName("InsertPerson")
            .Produces(StatusCodes.Status201Created, typeof(Person))
            .ProducesValidationProblem();

        app.MapPut("/api/people/{id:guid}", UpdateAsync)
            .WithName("UpdatePerson")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        app.MapDelete("/api/people/{id:guid}", DeleteAsync)
            .WithName("DeletePerson")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private async Task<IResult> GetListAsync([FromQuery(Name = "q")] string searchText, DataContext dataContext)
    {
        var query = dataContext.People.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query = query.Where(p => p.FirstName.Contains(searchText) || p.LastName.Contains(searchText));
        }

        var people = await query.OrderBy(p => p.FirstName).ThenBy(p => p.LastName)
            .Select(p => p.ToDto()).ToListAsync();

        return Results.Ok(people);
    }

    private async Task<IResult> GetAsync(Guid id, DataContext dataContext)
    {
        var dbPerson = await dataContext.People.FindAsync(id);
        if (dbPerson is null)
        {
            return Results.NotFound();
        }

        var person = dbPerson.ToDto();
        return Results.Ok(person);
    }

    private async Task<IResult> InsertAsync(Person person, DataContext dataContext, IValidator<Person> validator)
    {
        //if (!MiniValidator.TryValidate(person, out var errors))
        //{
        //    return Results.ValidationProblem(errors);
        //}

        var validationResult = validator.Validate(person);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.GroupBy(e => e.PropertyName)
                .ToDictionary(k => k.Key, v => v.Select(e => e.ErrorMessage).ToArray());

            return Results.ValidationProblem(errors);
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
    }

    private async Task<IResult> UpdateAsync(Guid id, Person person, DataContext dataContext, IValidator<Person> validator)
    {
        //if (!MiniValidator.TryValidate(person, out var errors))
        //{
        //    return Results.ValidationProblem(errors);
        //}

        var validationResult = validator.Validate(person);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.GroupBy(e => e.PropertyName)
                .ToDictionary(k => k.Key, v => v.Select(e => e.ErrorMessage).ToArray());

            return Results.ValidationProblem(errors);
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
    }

    private async Task<IResult> DeleteAsync(Guid id, DataContext dataContext)
    {
        var dbPerson = await dataContext.People.FindAsync(id);
        if (dbPerson is null)
        {
            return Results.NotFound();
        }

        dataContext.People.Remove(dbPerson);
        await dataContext.SaveChangesAsync();

        return Results.NoContent();
    }
}
