using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using ECommerce.Application;
using ECommerce.Infrastructure;
using ECommerce.Persistence;


var builder = WebApplication.CreateBuilder(args);

//katmanlar
builder.Services.AddApplicationServices();
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddInfrastructureServices();



builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//PostgreSQL connect
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));



var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ECommerce.API.Middlewares.GlobalExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();


app.Run();