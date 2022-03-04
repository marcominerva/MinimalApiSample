using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinimalApiSample.DataAccessLayer;
using MinimalApiSample.Extensions;
using MinimalApiSample.Models;
using MinimalApiSample.Routing;
using Entities = MinimalApiSample.DataAccessLayer.Entities;

namespace MinimalApiSample.Handlers;

public class ProductsHandler : IEndpointRouteHandler
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/products", GetListAsync)
            .WithName("GetProducts")
            .Produces(StatusCodes.Status200OK, typeof(IEnumerable<Product>));

        app.MapGet("/api/products/{id:guid}", GetAsync)
            .WithName("GetProduct")
            .Produces(StatusCodes.Status200OK, typeof(Product))
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/products", InsertAsync)
            .WithName("InsertProduct")
            .Produces(StatusCodes.Status201Created, typeof(Product))
            .ProducesValidationProblem();

        app.MapPut("/api/products/{id:guid}", UpdateAsync)
            .WithName("UpdateProduct")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        app.MapDelete("/api/products/{id:guid}", DeleteAsync)
            .WithName("DeleteProduct")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private async Task<IResult> GetListAsync([FromQuery(Name = "q")] string searchText, DataContext dataContext)
    {
        var query = dataContext.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query = query.Where(p => p.Name.Contains(searchText));
        }

        var products = await query.OrderBy(p => p.Name)
            .Select(p => p.ToDto()).ToListAsync();

        return Results.Ok(products);
    }

    private async Task<IResult> GetAsync(Guid id, DataContext dataContext)
    {
        var dbProduct = await dataContext.Products.FindAsync(id);
        if (dbProduct is null)
        {
            return Results.NotFound();
        }

        var person = dbProduct.ToDto();
        return Results.Ok(person);
    }

    private async Task<IResult> InsertAsync(Product product, DataContext dataContext, IValidator<Product> validator)
    {
        //if (!MiniValidator.TryValidate(product, out var errors))
        //{
        //    return Results.ValidationProblem(errors);
        //}

        var validationResult = validator.Validate(product);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.GroupBy(e => e.PropertyName)
                .ToDictionary(k => k.Key, v => v.Select(e => e.ErrorMessage).ToArray());

            return Results.ValidationProblem(errors);
        }

        var dbProduct = new Entities.Product
        {
            Name = product.Name,
            Price = product.Price
        };

        dataContext.Products.Add(dbProduct);
        await dataContext.SaveChangesAsync();

        return Results.CreatedAtRoute("GetProduct", new { dbProduct.Id }, dbProduct.ToDto());
    }

    private async Task<IResult> UpdateAsync(Guid id, Product product, DataContext dataContext, IValidator<Product> validator)
    {
        //if (!MiniValidator.TryValidate(product, out var errors))
        //{
        //    return Results.ValidationProblem(errors);
        //}

        var validationResult = validator.Validate(product);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.GroupBy(e => e.PropertyName)
                .ToDictionary(k => k.Key, v => v.Select(e => e.ErrorMessage).ToArray());

            return Results.ValidationProblem(errors);
        }

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
    }

    private async Task<IResult> DeleteAsync(Guid id, DataContext dataContext)
    {
        var dbProduct = await dataContext.Products.FindAsync(id);
        if (dbProduct is null)
        {
            return Results.NotFound();
        }

        dataContext.Products.Remove(dbProduct);
        await dataContext.SaveChangesAsync();

        return Results.NoContent();
    }
}
