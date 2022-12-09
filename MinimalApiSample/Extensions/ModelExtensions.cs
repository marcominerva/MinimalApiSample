using MinimalApiSample.Models;
using Entities = MinimalApiSample.DataAccessLayer.Entities;

namespace MinimalApiSample.Extensions;

public static class ModelExtensions
{
    public static Person ToDto(this Entities.Person person)
        => new()
        {
            Id = person.Id,
            FirstName = person.FirstName,
            LastName = person.LastName,
            City = person.City,
        };
}
