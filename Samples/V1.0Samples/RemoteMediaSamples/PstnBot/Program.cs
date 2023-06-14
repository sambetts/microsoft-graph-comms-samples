using Sample.IncidentBot.Bot;

var builder = WebApplication.CreateBuilder(args);


var logger = LoggerFactory.Create(config =>
{
    config.AddConsole();
}).CreateLogger("asdfasdfasdf");

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton(new CallingBot("https://sambetts.eu.ngrok.io", "d2a35726-10be-4092-8ea0-d70e7fd87cca", "w0X8Q~q1VyoaayLSMEKnVlBuTYh3HgvveAjetb14",
    "a9c42b85-c133-4aaf-a935-5b4685768b16", "cf32af95-0174-485c-a495-3cd29fd0b981", logger));

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
