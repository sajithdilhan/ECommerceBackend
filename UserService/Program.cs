using Microsoft.EntityFrameworkCore;
using OrderService.Events;
using Shared.Contracts;
using UserService.Data;
using UserService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var databaseName = builder.Configuration.GetConnectionString("UserDatabase") ?? "UserDatabase";
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseInMemoryDatabase(databaseName));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddHostedService<OrderConsumerService>();
builder.Services.AddSingleton<IKafkaProducerWrapper, KafkaProducerWrapper>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
