using Autofac;
using Autofac.Extensions.DependencyInjection;
using ChatSupport.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Tell .NET to use Autofac instead of the default DI
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

//builder.Services.AddHostedService<ChatMonitoringService>();

builder.Services.AddHostedService<ChatMonitoringService>();
builder.Host.ConfigureContainer<ContainerBuilder>((context, containerBuilder) =>
{

    containerBuilder.RegisterModule<ChatSupportModule>();

});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();


public partial class Program { }