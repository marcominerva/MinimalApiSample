var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddFormFile();
});

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddSqlServer<DataContext>(builder.Configuration.GetConnectionString("SqlConnection"));

var app = builder.Build();
await ConfigureDatabaseAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var peopleHandler = new PeopleHandler();
peopleHandler.MapEndpoints(app);

var peoplePhotoHandler = new PeoplePhotoHandler();
peoplePhotoHandler.MapEndpoints(app);

var productsHandler = new ProductsHandler();
productsHandler.MapEndpoints(app);

app.Run();

static async Task ConfigureDatabaseAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DataContext>();
    await db.Database.MigrateAsync();
}