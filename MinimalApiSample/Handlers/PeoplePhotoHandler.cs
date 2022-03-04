using Microsoft.AspNetCore.Mvc;
using MinimalApiSample.DataAccessLayer;

namespace MinimalApiSample.Handlers;

public class PeoplePhotoHandler
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

    private async Task<IResult> UpdatePhotoAsync(Guid id, HttpRequest request, DataContext dataContext)
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
