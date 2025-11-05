using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Shared.Exceptions;
using UserApi.Data;
using UserApi.Events;
using UserApi.Services;

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
