using ChatApp.Data;
using ChatApp.Hubs;
using ChatApp.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var databaseFileName = builder.Configuration["Database:FileName"];
if (string.IsNullOrWhiteSpace(databaseFileName))
{
    throw new InvalidOperationException("Set Database:FileName to the SQLite database filename.");
}

var connectionString = new SqliteConnectionStringBuilder
{
    DataSource = Path.Combine(builder.Environment.ContentRootPath, databaseFileName),
    ForeignKeys = true
}.ToString();
builder.Services.AddDbContext<ChatDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<ChatService>();
builder.Services.AddSingleton<PresenceTracker>();
builder.Services.AddSignalR();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var database = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    await database.Database.EnsureCreatedAsync();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapHub<ChatHub>("/chat");

app.Run();
