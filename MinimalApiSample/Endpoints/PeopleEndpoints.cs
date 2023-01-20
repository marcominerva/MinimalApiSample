using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MinimalApiSample.DataAccessLayer;
using MinimalApiSample.Extensions;
using MinimalApiSample.Filters;
using MinimalApiSample.Models;
using MinimalApiSample.Requests;
using Entities = MinimalApiSample.DataAccessLayer.Entities;

namespace MinimalApiSample.Endpoints;

public class PeopleEndpoints : IEndpointRouteHandler
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var peopleApiGroup = endpoints.MapGroup("/api/people");

        peopleApiGroup.MapGet("", GetListAsync)
            .WithName("GetPeople")
            .AddEndpointFilter(async (context, next) =>
            {
                Console.WriteLine("Executing...");

                var result = await next(context);

                Console.WriteLine("Executed.");

                return result;
            }).WithOpenApi(operation =>
            {
                operation.Summary = "Retrieves a list of people";
                operation.Description = "The list can be filtered by first name, last name and city";

                return operation;
            });

        peopleApiGroup.MapGet("{id:guid}", GetAsync)
            .WithName("GetPerson");

        peopleApiGroup.MapPost("", InsertAsync)
            .WithName("InsertPerson")
            .AddEndpointFilter<ValidatorFilter<Person>>();

        peopleApiGroup.MapPut("{id:guid}", UpdateAsync)
            .WithName("UpdatePerson")
            .AddEndpointFilter<ValidatorFilter<Person>>();

        peopleApiGroup.MapDelete("{id:guid}", DeleteAsync)
            .WithName("DeletePerson");
    }

    private static async Task<Ok<List<Person>>> GetListAsync([AsParameters] SearchPeopleRequest request, DataContext dataContext)
    {
        var query = dataContext.People.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.FirstName))
        {
            query = query.Where(p => p.FirstName.Contains(request.FirstName));
        }

        if (!string.IsNullOrWhiteSpace(request.LastName))
        {
            query = query.Where(p => p.LastName.Contains(request.LastName));
        }

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            query = query.Where(p => p.City.Contains(request.City));
        }

        var people = await query.OrderBy(p => p.FirstName).ThenBy(p => p.LastName)
            .Select(p => p.ToDto()).ToListAsync();

        return TypedResults.Ok(people);
    }

    private static async Task<Results<Ok<Person>, NotFound>> GetAsync([AsParameters] SinglePersonRequest request)
    {
        var dbPerson = await request.DataContext.People.FindAsync(request.Id);
        if (dbPerson is null)
        {
            return TypedResults.NotFound();
        }

        var person = dbPerson.ToDto();
        return TypedResults.Ok(person);
    }

    private static async Task<Results<CreatedAtRoute<Person>, BadRequest, ValidationProblem>> InsertAsync(Person person, DataContext dataContext)
    {
        var dbPerson = new Entities.Person
        {
            FirstName = person.FirstName,
            LastName = person.LastName,
            City = person.City,
        };

        dataContext.People.Add(dbPerson);
        await dataContext.SaveChangesAsync();

        return TypedResults.CreatedAtRoute(dbPerson.ToDto(), "GetPerson", new { dbPerson.Id });
    }

    private static async Task<Results<NoContent, NotFound, BadRequest, ValidationProblem>> UpdateAsync(Guid id, Person person, DataContext dataContext)
    {
        if (id != person.Id)
        {
            return TypedResults.BadRequest();
        }

        var dbPerson = await dataContext.People.FindAsync(id);
        if (dbPerson is null)
        {
            return TypedResults.NotFound();
        }

        dbPerson.FirstName = person.FirstName;
        dbPerson.LastName = person.LastName;
        dbPerson.City = person.City;

        await dataContext.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> DeleteAsync([AsParameters] SinglePersonRequest request)
    {
        var dbPerson = await request.DataContext.People.FindAsync(request.Id);
        if (dbPerson is null)
        {
            return TypedResults.NotFound();
        }

        request.DataContext.People.Remove(dbPerson);
        await request.DataContext.SaveChangesAsync();

        return TypedResults.NoContent();
    }
}
