using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OrderApi.Data;
using OrderApi.Events;
using OrderApi.Services;
using Shared.Authentication;
using Shared.Contracts;
using Shared.Middlewares;
using static Shared.Common.Constants;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "Enter your API Key",
        Type = SecuritySchemeType.ApiKey,
        Name = ApiKeyHeaderName,   // Same header you validate
        In = ParameterLocation.Header,
        Scheme = "ApiKeyScheme"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            new string[] {}
        }
    });
});

var databaseName = builder.Configuration.GetConnectionString("OrderDatabase") ?? "OrderDatabase";
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseInMemoryDatabase(databaseName));

builder.Services.AddSingleton<IKafkaProducerWrapper, KafkaProducerWrapper>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrdersService, OrdersService>();
builder.Services.AddHostedService<UserConsumerService>();
builder.Services.AddHealthChecks();
builder.Services.AddOptions<AuthenticationOptions>().Bind(builder.Configuration.GetSection(AuthenticationSectionName));
builder.Services.AddTransient<IApiKeyValidation, ApiKeyValidation>();
builder.Services.AddHttpContextAccessor();


var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapHealthChecks("/health");

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ApiKeyMiddleware>();

app.MapControllers();

app.Run();
