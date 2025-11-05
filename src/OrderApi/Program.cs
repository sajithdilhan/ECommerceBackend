using Microsoft.EntityFrameworkCore;
using OrderApi.Events;
using OrderApi.Data;
using OrderApi.Services;
using Shared.Contracts;
using Shared.Exceptions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var databaseName = builder.Configuration.GetConnectionString("OrderDatabase") ?? "OrderDatabase";
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseInMemoryDatabase(databaseName));

builder.Services.AddSingleton<IKafkaProducerWrapper, KafkaProducerWrapper>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrdersService, OrdersService>();
builder.Services.AddHostedService<UserConsumerService>();
builder.Services.AddHealthChecks();


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

//app.UseHttpsRedirection();

app.UseMiddleware<ExceptionMiddleware>();
app.MapHealthChecks("/health");

app.UseAuthorization();

app.MapControllers();

app.Run();
