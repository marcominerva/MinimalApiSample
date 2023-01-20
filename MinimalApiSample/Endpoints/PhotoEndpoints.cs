using System.Net.Mime;
using Microsoft.AspNetCore.Http.HttpResults;
using MinimalApiSample.DataAccessLayer;
using MinimalApiSample.Extensions;
using MinimalApiSample.Requests;

namespace MinimalApiSample.Endpoints;

public class PhotoEndpoints : IEndpointRouteHandler
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var peopleApiGroup = endpoints.MapGroup("/api/people");

        peopleApiGroup.MapGet("{id:guid}/photo", GetAsync)
            .WithName("GetPhoto")
            .Produces(StatusCodes.Status200OK, contentType: MediaTypeNames.Image.Jpeg);

        peopleApiGroup.MapPut("{id:guid}/photo", SaveAsync)
            .WithName("UpdatePhoto");

        peopleApiGroup.MapDelete("{id:guid}/photo", DeleteAsync)
            .WithName("DeletePhoto");
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> GetAsync(Guid id, DataContext dataContext)
    {
        var dbPerson = await dataContext.People.FindAsync(id);
        if (dbPerson?.Photo is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Bytes(dbPerson.Photo, "image/jpeg");
    }

    private static async Task<Results<NoContent, NotFound>> SaveAsync([AsParameters] SinglePersonRequest request, IFormFile file)
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
    }

    private static async Task<Results<NoContent, NotFound>> DeleteAsync([AsParameters] SinglePersonRequest request)
    {
        var dbPerson = await request.DataContext.People.FindAsync(request.Id);
        if (dbPerson is null)
        {
            return TypedResults.NotFound();
        }

        dbPerson.Photo = null;
        await request.DataContext.SaveChangesAsync();

        return TypedResults.NoContent();
    }
}
