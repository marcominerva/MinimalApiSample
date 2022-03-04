using Microsoft.AspNetCore.Mvc;
using MinimalApiSample.Binding;
using MinimalApiSample.DataAccessLayer;
using MinimalApiSample.Routing;

namespace MinimalApiSample.Handlers;

public class PeoplePhotoHandler : IEndpointRouteHandler
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/people/{id:guid}/photo", GetPhotoAsync)
            .WithName("GetPhoto")
            .Produces(StatusCodes.Status200OK, contentType: "image/jpeg")
            .Produces(StatusCodes.Status400BadRequest, typeof(ProblemDetails))
            .Produces(StatusCodes.Status404NotFound, typeof(ProblemDetails));

        app.MapPut("/api/people/{id:guid}/photo", UpdatePhotoAsync)
            .WithName("UpdatePhoto")
            .Accepts<FormFileContent>("multipart/form-data")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        app.MapDelete("/api/people/{id:guid}/photo", DeletePhotoAsync)
            .WithName("DeletePhoto")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private async Task<IResult> GetPhotoAsync(Guid id, DataContext dataContext)
    {
        var dbPerson = await dataContext.People.FindAsync(id);
        if (dbPerson?.Photo is null)
        {
            return Results.NotFound();
        }

        return Results.Bytes(dbPerson.Photo, "image/jpeg");
    }

    private async Task<IResult> UpdatePhotoAsync(Guid id, FormFileContent fileContent, DataContext dataContext)
    {
        var dbPerson = await dataContext.People.FindAsync(id);
        if (dbPerson is null)
        {
            return Results.NotFound();
        }

        using var stream = fileContent.Content.OpenReadStream();
        using var photoStream = new MemoryStream();
        await stream.CopyToAsync(photoStream);

        dbPerson.Photo = photoStream.ToArray();
        await dataContext.SaveChangesAsync();

        return Results.NoContent();
    }

    private async Task<IResult> DeletePhotoAsync(Guid id, DataContext dataContext)
    {
        var dbPerson = await dataContext.People.FindAsync(id);
        if (dbPerson is null)
        {
            return Results.NotFound();
        }

        dbPerson.Photo = null;
        await dataContext.SaveChangesAsync();

        return Results.NoContent();
    }
}
