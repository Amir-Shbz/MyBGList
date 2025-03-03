using System;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBGList.Models;
using MyBGList.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Logging.ClearProviders().
    AddSimpleConsole().
    AddDebug();

builder.Services.AddControllers(options =>
{
    options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(
    (x) => $"The value '{x}' is invalid.");
    options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(
    (x) => $"The field {x} must be a number.");
    options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor(
    (x, y) => $"The value '{x}' is not valid for {y}.");
    options.ModelBindingMessageProvider.SetMissingKeyOrValueAccessor(
    () => $"A value is required.");
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options => {
    options.AddDefaultPolicy(cfg => {
        cfg.WithOrigins(builder.Configuration["AllowedOrigins"]);
        cfg.AllowAnyHeader();
        cfg.AllowAnyMethod();
    });
    options.AddPolicy(name: "AnyOrigin",
    cfg => {
        cfg.AllowAnyOrigin();
        cfg.AllowAnyHeader();
        cfg.AllowAnyMethod();
    });
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
    builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSwaggerGen(options => {
    options.ParameterFilter<SortColumnFilter>();
    options.ParameterFilter<SortOrderFilter>();
});

//Code replaced by the [ManualValidationFilter] attribute
//builder.Services.Configure<ApiBehaviorOptions>(options =>
//    options.SuppressModelStateInvalidFilter = true);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();  
}


if (app.Configuration.GetValue<bool>("UseDeveloperExceptionPage"))
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
}

app.UseHttpsRedirection();

app.UseCors("AnyOrigin");

app.UseAuthorization();

app.MapGet("/error", 
    [EnableCors("AnyOrigin")]
    [ResponseCache(NoStore = true)] (HttpContext context) => 
    {
        var exceptionHandler =
        context.Features.Get<IExceptionHandlerPathFeature>(); 
        // TODO: logging, sending notifications, and more 
        var details = new ProblemDetails();
        details.Detail = exceptionHandler?.Error.Message; 
        details.Extensions["traceId"] =
            System.Diagnostics.Activity.Current?.Id
            ?? context.TraceIdentifier;
        details.Type =
        "https://tools.ietf.org/html/rfc7231#section-6.6.1";
        details.Status = StatusCodes.Status500InternalServerError;
        return Results.Problem(details);
    });

app.MapGet("/error/Test", [EnableCors("AnyOrigin")]
                          [ResponseCache(NoStore = true)]
                          () => { throw new Exception("TEST"); }).RequireCors("AnyOrigin");

app.MapGet("/COD/test", [EnableCors("AnyOrigin")]
                        [ResponseCache(NoStore = true)] 
                        () => Results.Text("<script>" +
                            "window.alert('Your client supports JavaScript!" +
                            "\\r\\n\\r\\n" +
                            $"Server time (UTC): {DateTime.UtcNow.ToString("o")}" +
                            "\\r\\n" +
                            "Client time (UTC): ' + new Date().toISOString());" +
                            "</script>" +
                            "<noscript>Your client does not support JavaScript</noscript>",
                            "text/html")
                        );

app.MapControllers().RequireCors("AnyOrigin");

app.UseExceptionHandler(action =>
{
    action.Run(async context =>
    {
        var exceptionHandler =
        context.Features.Get<IExceptionHandlerPathFeature>();
        var details = new ProblemDetails();
        details.Detail = exceptionHandler?.Error.Message;
        details.Extensions["traceId"] =
        System.Diagnostics.Activity.Current?.Id
        ?? context.TraceIdentifier;
        details.Type =
        "https://tools.ietf.org/html/rfc7231#section-6.6.1";
        details.Status = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync(
        System.Text.Json.JsonSerializer.Serialize(details));
    });
});

app.Run();
