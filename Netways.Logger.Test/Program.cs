using Microsoft.AspNetCore.Mvc;
using Netways.Logger.Core;
using Netways.Logger.Test.Controllers;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Netways Logger with simplified configuration
builder.Services.AddNetwaysLogger(builder.Configuration);

// Use Serilog for logging
builder.Host.UseSerilog();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use Netways Logger middleware
app.UseNetwaysLogger(builder.Configuration);

app.UseAuthorization();
app.MapControllers();

app.Run();
