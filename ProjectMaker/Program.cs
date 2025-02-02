using ProjectMaker.Base;
using ProjectMaker.Featueres.BaseCreator.Contracts;
using ProjectMaker.Featueres.BaseCreator.Services;
using ProjectMaker.Featueres.DataCreator.Contracts;
using ProjectMaker.Featueres.DataCreator.Services;
using ProjectMaker.Featueres.DBHandler.Contracts;
using ProjectMaker.Featueres.DBHandler.Services;
using ProjectMaker.Featueres.DtoCreator.Contracts;
using ProjectMaker.Featueres.DtoCreator.Services;
using ProjectMaker.Featueres.ProjectCreator.Contracts;
using ProjectMaker.Featueres.ProjectCreator.Services;
using ProjectMaker.Featueres.RelationShipCreator.Contracts;
using ProjectMaker.Featueres.RelationShipCreator.Services;
using ProjectMaker.Featueres.ServiceCreator.Contracts;
using ProjectMaker.Featueres.ServiceCreator.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ResponseHandler>();
builder.Services.AddScoped<IConfigurationDB, ConfigurationDB>();
builder.Services.AddScoped<IDataCreator, DataCreator>();
builder.Services.AddScoped<IRelationShipService, RelationShipService>();
builder.Services.AddScoped<IRelationShipConfiguration, RelationShipConfiguration>();
builder.Services.AddScoped<IRelationShipForiegnKey, RelationShipForiegnKey>();
builder.Services.AddScoped<IRelationShipValidator, RelationShipValidator>();
builder.Services.AddScoped<IDtoCreatorService, DtoCreatorService>();
builder.Services.AddScoped<IBaseGenerator, BaseGenerator>();
builder.Services.AddScoped<IPropertyHandler, PropertyHandler>();
builder.Services.AddScoped<IServiceCreatorService, ServiceCreatorService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // Specify the Angular app URL
              .AllowAnyHeader()                     // Allow all headers
              .AllowAnyMethod()                     // Allow all HTTP methods (GET, POST, etc.)
              .AllowCredentials();                  // Allow cookies/authentication if needed
    });
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseCors("AllowAngularApp");
app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI();
app.Run();
