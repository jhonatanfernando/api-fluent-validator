using ApiFluentValidator.Data;
using ApiFluentValidator.Helpers;
using ApiFluentValidator.Models;
using ApiFluentValidator.Security;
using ApiFluentValidator.Services;
using ApiFluentValidator.Validators;
using Asp.Versioning;
using Asp.Versioning.Conventions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();
//builder.Services.AddDistributedMemoryCache();

builder.Services.AddDistributedSqlServerCache(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("DistCacheConnection");
    options.SchemaName = "dbo";
    options.TableName = "TestCache";
});

builder.Services.AddDbContext<TodoDb>(opt => opt.UseInMemoryDatabase("TodoList"));
builder.Services.AddScoped<IValidator<Todo>, TodoValidator>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITodoService, TodoService>();


var version1 = new ApiVersion(1);
var version2 = new ApiVersion(2);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var logger = LoggerFactory.Create(config =>
{
    config.AddConsole();
}).CreateLogger("Program");

//builder.Services.Configure<ConsoleLifetimeOptions>(opt => opt.SuppressStatusMessages = true);

builder.Services.AddApiVersioning(opt =>
{
    opt.ApiVersionReader = new UrlSegmentApiVersionReader();
    opt.DefaultApiVersion = version1;
    opt.AssumeDefaultVersionWhenUnspecified = true;
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BasicAuth", Version = "v1" });
    c.SwaggerDoc("v2", new OpenApiInfo { Title = "BasicAuth", Version = "v2" });
    c.AddSecurityDefinition("basic", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "basic",
        In = ParameterLocation.Header,
        Description = "Basic Authorization header using the Bearer scheme."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
{
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "basic"
            }
        },
        new string[] { }
    }
});
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
    c.OperationFilter<ApiVersionOperationFilter>();
});


// configure basic authentication 
builder.Services.AddAuthentication("BasicAuthentication")
    .AddScheme<CustomBasicAuthenticationSchemeOptions, CustomBasicAuthenticationHandler>("BasicAuthentication",null);
builder.Services.AddAuthorization();


var app = builder.Build();


var versionSet = app.NewApiVersionSet()
                     .HasApiVersion(version1)
                     .HasApiVersion(version2)
                     .Build();


app.MapGet("/", () =>
{
    logger.LogInformation("Logging information.");
    logger.LogCritical("Logging critical information.");
    logger.LogDebug("Logging debug information.");
    logger.LogError("Logging error information.");
    logger.LogTrace("Logging trace");
    logger.LogWarning("Logging warning.");

    return "Hello World!";
});

app.MapGet("v{version:apiVersion}/todoitems", [Authorize] async (ITodoService todoService) =>
{
    logger.LogInformation("Getting todoitems");

    return await todoService.GetAllWithDistributedCache();
})
.WithApiVersionSet(versionSet)
.MapToApiVersion(version1);

app.MapGet("/todoitems/complete", async (TodoDb db) =>
    await db.Todos.Where(t => t.IsComplete).ToListAsync());

app.MapGet("/todoitems/{id}", async (int id, TodoDb db) =>
    await db.Todos.FindAsync(id)
        is Todo todo
            ? Results.Ok(todo)
            : Results.NotFound());


app.MapPost("v{version:apiVersion}/todoitems", [Authorize] async (IValidator<Todo> validator, Todo todo, TodoDb db) =>
{
    try
    {
        logger.LogInformation("Saving todoitem");

        ValidationResult validationResult = await validator.ValidateAsync(todo);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        db.Todos.Add(todo);
        await db.SaveChangesAsync();

        logger.LogInformation("TodoItem was saved");

        return Results.Created($"/todoitems/{todo.Id}", todo);
    }
    catch(Exception ex)
    {
        logger.LogError(ex, "An unexpected exception occured");
        return Results.Problem();
    }
})
.WithApiVersionSet(versionSet)
.MapToApiVersion(version1);

app.MapPost("v{version:apiVersion}/todoitems", [Authorize] async (IValidator<Todo> validator, Todo todo, TodoDb db) =>
{
    ValidationResult validationResult = await validator.ValidateAsync(todo);

    if (!validationResult.IsValid)
    {
        return Results.ValidationProblem(validationResult.ToDictionary());
    }

    todo.CompletedTimestamp = DateTime.Now;
    db.Todos.Add(todo);
    await db.SaveChangesAsync();

    return Results.Created($"/todoitems/{todo.Id}", todo);
})
.WithName("newtodoitems")
.WithApiVersionSet(versionSet)
.MapToApiVersion(version2);



app.MapPut("/todoitems/{id}", async (IValidator<Todo> validator, int id, Todo inputTodo, TodoDb db) =>
{
    ValidationResult validationResult = await validator.ValidateAsync(inputTodo);

    if (!validationResult.IsValid)
    {
        return Results.ValidationProblem(validationResult.ToDictionary());
    }

    var todo = await db.Todos.FindAsync(id);

    if (todo is null) return Results.NotFound();

    todo.Name = inputTodo.Name;
    todo.IsComplete = inputTodo.IsComplete;

    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapDelete("/todoitems/{id}", async (int id, TodoDb db) =>
{
    if (await db.Todos.FindAsync(id) is Todo todo)
    {
        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return Results.Ok(todo);
    }

    return Results.NotFound();
});

app.MapPost("/user", async (IUserService userService, User user) =>
{
    await userService.Save(user);

    return Results.Created($"/users/{user.Id}", user);
});


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.Run();
