using Microsoft.Extensions.FileProviders;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Загрузка необходимых DLL
var nativePath = Path.Combine(
    AppDomain.CurrentDomain.BaseDirectory,
    "runtimes",
    "win-x64",
    "native");


var modelDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models");

string[] dllFiles = Directory.GetFiles(modelDirectory, "*.dll");
foreach (var dllFile in dllFiles)
{
    Assembly.LoadFrom(dllFile); // Загружаем DLL
}

builder.Services.AddRazorPages();

builder.Services.AddControllers();

var app = builder.Build();

var visDirectory = Path.Combine(builder.Environment.ContentRootPath, "Visualized");

if (!Directory.Exists(visDirectory))
{
    Directory.CreateDirectory(visDirectory);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(visDirectory),
    RequestPath = "/Visualized"
});


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.MapControllers();

app.Run();
