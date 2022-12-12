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
using Microsoft.Extensions.Logging.Console;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.Elasticsearch;
using Serilog.Sinks.File;
using ILogger = Serilog.ILogger;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TodoDb>(opt => opt.UseInMemoryDatabase("TodoList"));
builder.Services.AddScoped<IValidator<Todo>, TodoValidator>();
builder.Services.AddScoped<IUserService, UserService>();

var version1 = new ApiVersion(1);
var version2 = new ApiVersion(2);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Logging.ClearProviders();


//builder.Host.UseSerilog((hostContext, services, configuration) => {
//    configuration
//    .WriteTo.Debug(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
//});


//builder.Logging.ClearProviders();
//builder.Host.UseSerilog((hostContext, services, configuration) => {
//    configuration
//    .WriteTo.File(
//      "diagnostics.txt",
//       rollingInterval: RollingInterval.Day,
//       fileSizeLimitBytes: 10 * 1024 * 1024,
//       retainedFileCountLimit: 2,
//       rollOnFileSizeLimit: true,
//       shared: true,
//       flushToDiskInterval: TimeSpan.FromSeconds(1));
//});

builder.Host.UseSerilog((hostContext, services, configuration) =>
{
    configuration
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200"))
    {
        FailureCallback = e => Console.WriteLine("Unable to submit event " + e.MessageTemplate),
        EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog | EmitEventFailureHandling.WriteToFailureSink | EmitEventFailureHandling.RaiseCallback,
        QueueSizeLimit = 100000,
        BatchPostingLimit = 50,
        IndexFormat = "custom-index-{0:yyyy.MM}"
    });
});

Serilog.Debugging.SelfLog.Enable(msg => Console.WriteLine(msg));


//Load settings from the appsettings.
//builder.Host.UseSerilog((context, services, configuration) => configuration
//    .ReadFrom.Configuration(context.Configuration)
//    .WriteTo.Debug());


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



app.MapGet("/", (ILogger logger) =>
{
    logger.Information("Logging information.");
    logger.Error("Logging critical information.");
    logger.Debug("Logging debug information.");
    logger.Error("Logging error information.");
    logger.Warning("Logging warning.");

    return "Hello World!";
});

app.MapGet("v{version:apiVersion}/todoitems", [Authorize] async (TodoDb db) =>
{
    //logger.Information("Getting todoitems");

    return await db.Todos.ToListAsync();
})
.WithApiVersionSet(versionSet)
.HasApiVersions(new[] { version1, version2 });



app.MapGet("/todoitems/complete", async (TodoDb db) =>
    await db.Todos.Where(t => t.IsComplete).ToListAsync());

app.MapGet("/todoitems/{id}", async (int id, TodoDb db) =>
    await db.Todos.FindAsync(id)
        is Todo todo
            ? Results.Ok(todo)
            : Results.NotFound());


app.MapPost("v{version:apiVersion}/todoitems", [Authorize] async (ILogger logger, IValidator<Todo> validator, Todo todo, TodoDb db) =>
{
    try
    {
        logger.Information("Saving todoitem");

        ValidationResult validationResult = await validator.ValidateAsync(todo);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        db.Todos.Add(todo);
        await db.SaveChangesAsync();

        logger.Information("TodoItem was saved");

        return Results.Created($"/todoitems/{todo.Id}", todo);
    }
    catch (Exception ex)
    {
        logger.Error(ex, "An unexpected exception occured");
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



app.MapPut("/todoitems/{id}", async (ILogger logger, IValidator < Todo> validator, int id, Todo inputTodo, TodoDb db) =>
{
    try
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
    }
    catch (Exception ex)
    {
        logger.Error(ex, "An unexpected exception occured");
        return Results.Problem();
    }
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

// app.UseMiddleware<CustomApiKeyMiddleware>(app.Configuration.GetValue<string>("TodoApiKey"));

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.Run();
