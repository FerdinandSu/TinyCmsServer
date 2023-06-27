using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Steeltoe.Extensions.Configuration.Placeholder;
using static Microsoft.AspNetCore.Http.Results;
using File = System.IO.File;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddPlaceholderResolver();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDirectoryBrowser();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI();

var contentTypeProvider = new FileExtensionContentTypeProvider();

var contentDir = Path.GetFullPath(app.Configuration["ResourceDir"] ?? "/contents");
bool CheckPath(string path) => path.StartsWith(contentDir + Path.DirectorySeparatorChar) && !Directory.Exists(path);
string ContentPath(string subPath) => Path.Combine(contentDir!, subPath.Replace("%2F", "/"));
void EnsureDirectory(string path) => Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
app.UseFileServer(new FileServerOptions
{
    EnableDirectoryBrowsing = true,
    FileProvider = new PhysicalFileProvider(contentDir),
    RequestPath = "/Files",
    StaticFileOptions =
    {
        ContentTypeProvider = contentTypeProvider,
        ServeUnknownFileTypes = true
    }
});



app.MapGet("/Exists/{**path}",
    (string path) =>
    {
        path = ContentPath(path);
        return !CheckPath(path) ? BadRequest() : Ok(File.Exists(path));
    });
if (app.Configuration.GetValue("AllowDelete", false))
    app.MapDelete("/Delete/{**path}",
    (string path) =>
    {
        path = ContentPath(path);
        if (!CheckPath(path)) return BadRequest();
        File.Delete(path);
        return NoContent();
    });
if (app.Configuration.GetValue("AllowDeleteDir", false))
    app.MapDelete("/DeleteDir/{**path}",
    (string path) =>
    {
        path = ContentPath(path);
        if (!path.StartsWith(contentDir + Path.DirectorySeparatorChar)) return BadRequest();
        Directory.Delete(path);
        return NoContent();
    });
if (app.Configuration.GetValue("AllowStream", false))
    app.MapGet("/Stream/{**path}",
        (string path) =>
        {
            path = ContentPath(path);
            if (!CheckPath(path)) return BadRequest();
            if (!File.Exists(path)) return NotFound();
            contentTypeProvider.TryGetContentType(path, out var contentType);
            return File(path, contentType, Path.GetFileName(path), enableRangeProcessing: true);
        });
//if (app.Configuration.GetValue("AllowDownloadDirZip", false))
//    app.MapGet("/Stream/{**path}",
//        (string path) =>
//        {
//            path = ContentPath(path);
//            if (!CheckPath(path)) return BadRequest();
//            if (!Directory.Exists(path)) return NotFound();
//            ZipFile.CreateFromDirectory(path, $"{path}.zip");
//            path = $"{path}.zip";
//            contentTypeProvider.TryGetContentType(path, out var contentType);
//            return File(path, contentType, Path.GetFileName(path), enableRangeProcessing: true);
//        });
if (app.Configuration.GetValue("AllowWrite", false))
{
    app.MapPut("/Upload/{**path}",
        async ([FromRoute] string path, [FromForm] IFormFile file) =>
        {
            path = ContentPath(path);
            if (!CheckPath(path)) return BadRequest();
            EnsureDirectory(path);
            if (File.Exists(path)) return BadRequest();
            await using var rs = file.OpenReadStream();
            await using var fs = File.OpenWrite(path);
            await rs.CopyToAsync(fs);
            fs.Close();
            return NoContent();
        });
    app.MapPut("/UploadDirZip/{**path}",
        async ([FromRoute] string path, [FromForm] IFormFile file) =>
        {
            path = ContentPath(path);
            if (!CheckPath(path)) return BadRequest();
            EnsureDirectory(path);
            if (File.Exists(path)) return BadRequest();
            await using var rs = file.OpenReadStream();
            await using var fs = File.OpenWrite($"{path}.zip");
            await rs.CopyToAsync(fs);
            fs.Close();
            ZipFile.ExtractToDirectory($"{path}.zip", path);
            File.Delete($"{path}.zip");
            return NoContent();
        });
}
if (app.Configuration.GetValue("AllowOverWrite", false))
{
    app.MapPut("/UploadOrUpdate/{**path}",
        async ([FromRoute] string path, [FromForm] IFormFile file) =>
        {
            path = ContentPath(path);
            if (!CheckPath(path)) return BadRequest();
            EnsureDirectory(path);
            await using var rs = file.OpenReadStream();
            await using var fs = File.OpenWrite(path);
            await rs.CopyToAsync(fs);
            fs.Close();
            return NoContent();
        });
    app.MapPut("/UploadOrUpdateDirZip/{**path}",
        async ([FromRoute] string path, [FromForm] IFormFile file) =>
        {
            path = ContentPath(path);
            if (!CheckPath(path)) return BadRequest();
            EnsureDirectory(path);
            await using var rs = file.OpenReadStream();
            await using var fs = File.OpenWrite($"{path}.zip");
            await rs.CopyToAsync(fs);
            fs.Close();
            ZipFile.ExtractToDirectory($"{path}.zip", path,true);
            File.Delete($"{path}.zip");
            return NoContent();
        });
}


app.Run();

