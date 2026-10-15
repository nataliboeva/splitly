using Microsoft.EntityFrameworkCore;
using Splitly.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Splitly")));

var app = builder.Build();

app.Run();
