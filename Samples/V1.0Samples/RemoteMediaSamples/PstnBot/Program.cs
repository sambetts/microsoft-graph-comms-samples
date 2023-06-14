using Microsoft.Graph.Communications.Common.Telemetry;
using Sample.IncidentBot.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.Bind("");

var logger = LoggerFactory.Create(config =>
{
    config.AddConsole();
}).CreateLogger("asdfasdfasdf");
builder.Services.AddSingleton(logger);

// Add services to the container.

builder.Services.AddBot(options => builder.Configuration.Bind("Bot", options));
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var graphLogger = new GraphLogger(typeof(Program).Assembly.GetName().Name);
builder.Services.AddSingleton<IGraphLogger>(graphLogger);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{ 
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();
