using Microsoft.AspNetCore.Http;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseStaticFiles();

app.MapGet("/", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync("wwwroot/index.html");
});

app.MapPost("/upload", async (HttpContext context) =>
{
    var form = await context.Request.ReadFormAsync();
    var title = form["title"].ToString();
    var file = form.Files["file"];

    if (string.IsNullOrEmpty(title) || file == null || 
        !(file.ContentType == "image/jpeg" || file.ContentType == "image/png" || file.ContentType == "image/gif"))
    {
        return Results.BadRequest("Invalid input.");
    }

    var id = Guid.NewGuid().ToString();
    var filePath = Path.Combine("wwwroot", "uploads", $"{id}_{file.FileName}");

    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }

    var imageInfo = new { Id = id, Title = title, FilePath = filePath };
    var json = JsonSerializer.Serialize(imageInfo);
    await File.WriteAllTextAsync(Path.Combine("wwwroot", "uploads", $"{id}.json"), json);

    return Results.Redirect($"/picture/{id}");
});

app.MapGet("/picture/{id}", async (HttpContext context) =>
{
    var id = context.Request.RouteValues["id"].ToString();
    var jsonFilePath = Path.Combine("wwwroot", "uploads", $"{id}.json");

    if (!File.Exists(jsonFilePath))
    {
        return Results.NotFound("Image not found.");
    }

    var json = await File.ReadAllTextAsync(jsonFilePath);
    var imageInfo = JsonSerializer.Deserialize<ImageInfo>(json);

    var html = $@"
        <html>
        <head>
            <style>
                body {{
                    font-family: Arial, sans-serif;
                    background-color: #f4f4f4;
                    margin: 0;
                    padding: 20px;
                    display: flex;
                    flex-direction: column;
                    align-items: center;
                }}
                h1 {{
                    color: #333;
                }}
                img {{
                    max-width: 100%;
                    height: auto;
                    border: 1px solid #ccc;
                    box-shadow: 0 4px 8px rgba(0,0,0,0.1);
                    margin-top: 20px;
                }}
                .container {{
                    background-color: #fff;
                    padding: 20px;
                    border-radius: 8px;
                    box-shadow: 0 4px 8px rgba(0,0,0,0.1);
                    text-align: center;
                }}
            </style>
        </head>
        <body>
            <div class='container'>
                <h1>{imageInfo.Title}</h1>
                <img src='/uploads/{Path.GetFileName(imageInfo.FilePath)}' alt='{imageInfo.Title}' />
            </div>
        </body>
        </html>";

    return Results.Content(html, "text/html");
     });

app.Run();
