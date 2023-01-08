using System.Diagnostics;
using System.Net.Mime;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using MinimalApiSample.DataAccessLayer;
using MinimalApiSample.Extensions;
using MinimalApiSample.Filters;
using MinimalApiSample.Models;
using MinimalApiSample.Parameters;
using Entities = MinimalApiSample.DataAccessLayer.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddSqlServer<DataContext>(builder.Configuration.GetConnectionString("SqlConnection"));

builder.Services.AddProblemDetails();

var app = builder.Build();
await ConfigureDatabaseAsync(app.Services);

app.UseHttpsRedirection();

if (!app.Environment.IsDevelopment())
{
    // Error handling
    app.UseExceptionHandler(new ExceptionHandlerOptions
    {
        AllowStatusCode404Response = true,
        ExceptionHandler = async (HttpContext context) =>
        {
            var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
            var error = exceptionHandlerFeature?.Error;

            if (context.RequestServices.GetRequiredService<IProblemDetailsService>() is { } problemDetailsService)
            {
                // Write as JSON problem details
                await problemDetailsService.WriteAsync(new()
                {
                    HttpContext = context,
                    AdditionalMetadata = exceptionHandlerFeature?.Endpoint?.Metadata,
                    ProblemDetails =
                    {
                        Status = context.Response.StatusCode,
                        Title = error?.GetType().FullName ?? "an error occurred while processing your request",
                        Detail = error?.Message
                    }
                });
            }
            else
            {
                context.Response.ContentType = MediaTypeNames.Text.Plain;
                var message = ReasonPhrases.GetReasonPhrase(context.Response.StatusCode) switch
                {
                    { Length: > 0 } reasonPhrase => reasonPhrase,
                    _ => "An error occurred"
                };
                await context.Response.WriteAsync(message + "\r\n");
                await context.Response.WriteAsync($"Request ID: {Activity.Current?.Id ?? context.TraceIdentifier}");
            }
        }
    });
}

app.UseStatusCodePages();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var peopleApiGroup = app.MapGroup("/api/people");

peopleApiGroup.MapGet("", async ([AsParameters] SearchPeopleRequest request, DataContext dataContext) =>
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
})
.WithName("GetPeople")
.AddEndpointFilter(async (context, next) =>
{
    Console.WriteLine("Executing...");

    var result = await next(context);

    Console.WriteLine("Executed.");

    return result;
})
.WithOpenApi(operation =>
{
    operation.Summary = "Retrieves a list of people";
    operation.Description = "The list can be filtered by first name, last name and city";
    return operation;
});

peopleApiGroup.MapGet("{id:guid}", async Task<Results<Ok<Person>, NotFound>> ([AsParameters] SinglePersonRequest request) =>
{
    var dbPerson = await request.DataContext.People.FindAsync(request.Id);
    if (dbPerson is null)
    {
        return TypedResults.NotFound();
    }

    var person = dbPerson.ToDto();
    return TypedResults.Ok(person);
})
.WithName("GetPerson");

peopleApiGroup.MapPost("", async Task<Results<CreatedAtRoute<Person>, BadRequest, ValidationProblem>> (Person person, DataContext dataContext) =>
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
})
.WithName("InsertPerson")
.AddEndpointFilter<ValidatorFilter<Person>>();

peopleApiGroup.MapPut("{id:guid}", async Task<Results<NoContent, NotFound, BadRequest, ValidationProblem>> (Guid id, Person person, DataContext dataContext) =>
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
})
.WithName("UpdatePerson")
.AddEndpointFilter<ValidatorFilter<Person>>();

peopleApiGroup.MapDelete("{id:guid}", async Task<Results<NoContent, NotFound>> ([AsParameters] SinglePersonRequest request) =>
{
    var dbPerson = await request.DataContext.People.FindAsync(request.Id);
    if (dbPerson is null)
    {
        return TypedResults.NotFound();
    }

    request.DataContext.People.Remove(dbPerson);
    await request.DataContext.SaveChangesAsync();

    return TypedResults.NoContent();
})
.WithName("DeletePerson");

peopleApiGroup.MapGet("{id:guid}/photo", async Task<Results<FileContentHttpResult, NotFound>> (Guid id, DataContext dataContext) =>
{
    var dbPerson = await dataContext.People.FindAsync(id);
    if (dbPerson?.Photo is null)
    {
        return TypedResults.NotFound();
    }

    return TypedResults.Bytes(dbPerson.Photo, "image/jpeg");
})
.WithName("GetPhoto")
.Produces(StatusCodes.Status200OK, contentType: MediaTypeNames.Image.Jpeg);

peopleApiGroup.MapPut("{id:guid}/photo", async Task<Results<NoContent, NotFound>> ([AsParameters] SinglePersonRequest request, IFormFile file) =>
{
    var dbPerson = await request.DataContext.People.FindAsync(request.Id);
    if (dbPerson is null)
    {
        return TypedResults.NotFound();
    }

    using var stream = file.OpenReadStream();
    using var photoStream = new MemoryStream();
    await stream.CopyToAsync(photoStream);

    dbPerson.Photo = photoStream.ToArray();
    await request.DataContext.SaveChangesAsync();

    return TypedResults.NoContent();
})
.WithName("UpdatePhoto");

peopleApiGroup.MapDelete("{id:guid}/photo", async Task<Results<NoContent, NotFound>> ([AsParameters] SinglePersonRequest request) =>
{
    var dbPerson = await request.DataContext.People.FindAsync(request.Id);
    if (dbPerson is null)
    {
        return TypedResults.NotFound();
    }

    dbPerson.Photo = null;
    await request.DataContext.SaveChangesAsync();

    return TypedResults.NoContent();
})
.WithName("DeletePhoto");

app.Run();

static async Task ConfigureDatabaseAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DataContext>();
    await db.Database.MigrateAsync();
}