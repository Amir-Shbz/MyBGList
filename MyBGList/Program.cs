using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBGList.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

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

app.MapGet("/error", [EnableCors("AnyOrigin")] 
                     [ResponseCache(NoStore = true)]
                     () => Results.Problem());

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


app.Run();
