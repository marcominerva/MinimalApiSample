using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MinimalApiSample.DataAccessLayer.Entities;

namespace MinimalApiSample.DataAccessLayer.Configurations;

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("People");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.FirstName).HasMaxLength(30).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(30).IsRequired();
        builder.Property(x => x.City).HasMaxLength(50);
    }
}