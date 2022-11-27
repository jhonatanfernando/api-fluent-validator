using ApiFluentValidator.Data;
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

builder.Services.AddDbContext<TodoDb>(opt => opt.UseInMemoryDatabase("TodoList"));
builder.Services.AddScoped<IValidator<Todo>, TodoValidator>();
builder.Services.AddScoped<IUserService, UserService>();

var version1 = new ApiVersion(1);
var version2 = new ApiVersion(2);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApiVersioning(opt =>
{
    opt.ApiVersionReader = new HeaderApiVersionReader("Api-Version");
    opt.DefaultApiVersion = version1;
    opt.AssumeDefaultVersionWhenUnspecified = true;
});

//builder.Services.AddSwaggerGen(c =>
//{
//    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TodoServiceApi", Version = "v1" });

//    c.AddSecurityDefinition(Constants.ApiKeyHeaderName, new OpenApiSecurityScheme
//    {
//        Description = "Api key needed to access the endpoints. ApiKey: ApiKey",
//        In = ParameterLocation.Header,
//        Name = Constants.ApiKeyHeaderName,
//        Type = SecuritySchemeType.ApiKey
//    });

//    c.AddSecurityRequirement(new OpenApiSecurityRequirement
//    {
//        {
//            new OpenApiSecurityScheme
//            {
//                Name = Constants.ApiKeyHeaderName,
//                Type = SecuritySchemeType.ApiKey,
//                In = ParameterLocation.Header,
//                Reference = new OpenApiReference
//                {
//                    Type = ReferenceType.SecurityScheme,
//                    Id = Constants.ApiKeyHeaderName,
//                },
//                },
//                new string[] {}
//            }
//    });
//});



builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BasicAuth", Version = "v1" });
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
                            new string[] {}
                    }
                });
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



app.MapGet("/", () => "Hello World!");

//app.MapGet("v{version:apiVersion}/todoitems", [Authorize] async (TodoDb db) =>
//{
//    return await db.Todos.ToListAsync();
//})
//.WithApiVersionSet(versionSet)
//.HasApiVersions(new[] { version1, version2 });

app.MapGet("/todoitems", [Authorize] async (TodoDb db) =>
{
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


app.MapPost("/todoitems", [Authorize] async (IValidator<Todo> validator, Todo todo, TodoDb db) =>
{
    ValidationResult validationResult = await validator.ValidateAsync(todo);

    if (!validationResult.IsValid)
    {
        return Results.ValidationProblem(validationResult.ToDictionary());
    }

    db.Todos.Add(todo);
    await db.SaveChangesAsync();

    return Results.Created($"/todoitems/{todo.Id}", todo);
})
.WithApiVersionSet(versionSet)
.HasApiVersion(version1);



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

// app.UseMiddleware<CustomApiKeyMiddleware>(app.Configuration.GetValue<string>("TodoApiKey"));

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.Run();
