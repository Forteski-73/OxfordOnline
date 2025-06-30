using System.ServiceModel;
using Microsoft.EntityFrameworkCore;
using OxfordOnline.Data;
using Oxfordonline.Integration;

var builder = WebApplication.CreateBuilder(args);

// Configurar Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(10, 5, 9))));

// Adicionar serviço SOAP (Dynamics AX)
builder.Services.AddSingleton<ProductServicesClient>(provider =>
{
    var binding = new BasicHttpBinding();
    var endpoint = new EndpointAddress("http://ax201203:8201/DynamicsAx/Services/WSIntegratorServices");
    return new ProductServicesClient(binding, endpoint);
});

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "OxfordOnline API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Informe o token no formato: Bearer {seu_token}"
    });

    c.AddSecurityRequirement(new()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] { }
        }
    });
});

// Ambiente de produção
if (builder.Environment.IsProduction())
{
    builder.WebHost.UseUrls("http://0.0.0.0:5000");
}

var app = builder.Build();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Força o caminho base como /API
app.UsePathBase("/API");

// Redirecionamento da raiz da API para o Swagger
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/API/swagger/index.html");
        return;
    }

    await next();
});

// Middleware de autenticação condicional
app.UseWhen(context =>
    !context.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase)
    && !context.Request.Path.StartsWithSegments("/favicon", StringComparison.OrdinalIgnoreCase),
    appBuilder =>
    {
        appBuilder.Use(async (context, next) =>
        {
            var tokenConfigurado = builder.Configuration["AuthToken"];
            var tokenEnviado = context.Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(tokenEnviado))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Token ausente.");
                return;
            }

            var token = tokenEnviado.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? tokenEnviado.Substring("Bearer ".Length).Trim()
                : tokenEnviado.Trim();

            if (token != tokenConfigurado)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Token inválido.");
                return;
            }

            await next();
        });
    });

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();


/*
using System.ServiceModel;
using Microsoft.EntityFrameworkCore;
using OxfordOnline.Data;
using Oxfordonline.Integration;

var builder = WebApplication.CreateBuilder(args);

//builder.AddServiceDefaults();

// Configurar Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(10, 5, 9))));

// Adicionar WSIntegratorServices como um serviço Singleton

builder.Services.AddSingleton<Oxfordonline.Integration.ProductServicesClient>(provider =>
{
    var binding = new BasicHttpBinding();
    var endpoint = new EndpointAddress("http://ax201203:8201/DynamicsAx/Services/WSIntegratorServices");
    return new Oxfordonline.Integration.ProductServicesClient(binding, endpoint);
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "OxfordOnline API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Informe o token no formato: Bearer {seu_token}"
    });

    c.AddSecurityRequirement(new()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var environment = builder.Environment.EnvironmentName;
if (environment == "Production")  // Ambiente de Produção
{
    builder.WebHost.UseUrls("http://0.0.0.0:5000");
}

var app = builder.Build();

//app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment()) // ambiente de desenvolvimento
//{
    //app.UseSwagger();
    //app.UseSwaggerUI();
//}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// Define o caminho base como /API
app.UsePathBase("/API");

// Redirecionamento da raiz para o Swagger
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/API/swagger/index.html");
        return;
    }

    await next();
});

// Middleware para validar token
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";

    // Ignora Swagger e arquivos estáticos
    if (path.StartsWith("/API/swagger", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/API/favicon", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".js") || path.EndsWith(".css") || path.EndsWith(".json"))
    {
        await next();
        return;
    }

    var tokenConfigurado = builder.Configuration["AuthToken"];
    var tokenEnviado = context.Request.Headers["Authorization"].FirstOrDefault();

    if (string.IsNullOrEmpty(tokenEnviado))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Token ausente.");
        return;
    }

    var token = tokenEnviado.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
        ? tokenEnviado.Substring("Bearer ".Length).Trim()
        : tokenEnviado.Trim();

    if (token != tokenConfigurado)
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Token inválido.");
        return;
    }

    await next();
});

app.UseAuthorization();

app.MapControllers();

app.Run();
*/