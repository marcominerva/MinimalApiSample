﻿namespace MinimalApiSample.Models;

public class Person
{
    public Guid Id { get; set; }

    //[Required]
    //[MaxLength(30)]
    public string FirstName { get; set; }

    //[Required]
    //[MaxLength(30)]
    public string LastName { get; set; }

    //[MaxLength(50)]
    public string City { get; set; }
}
