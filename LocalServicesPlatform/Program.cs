using LocalServicesPlatform.Components;
using LocalServicesPlatform.Models;
using LocalServicesPlatform.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
// This connects the UI to the Database Logic
builder.Services.AddScoped<LocalServicesPlatform.Services.PlatformService>();
// Inside Program.cs - verify it says AddScoped!
builder.Services.AddScoped<UserSession>();
// This allows the app to remember the logged-in user
builder.Services.AddScoped<LocalServicesPlatform.Services.UserSession>();
builder.Services.AddDbContext<LocalServicesDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<LocalServicesPlatform.Services.PlatformService>();
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
// Temporary Debug Code
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LocalServicesPlatform.Models.LocalServicesDbContext>();
    Console.WriteLine("=================================================");
    Console.WriteLine($"REAL DATABASE SERVER: {dbContext.Database.GetDbConnection().DataSource}");
    Console.WriteLine($"REAL DATABASE NAME: {dbContext.Database.GetDbConnection().Database}");
    Console.WriteLine("=================================================");
}
app.Run();