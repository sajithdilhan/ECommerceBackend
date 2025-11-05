using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Events;
using OrderService.Services;
using Shared.Contracts;

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


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
