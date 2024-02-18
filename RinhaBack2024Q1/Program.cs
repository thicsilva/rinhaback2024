using RinhaBack2024Q1;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<RouteHandlerOptions>(o => { o.ThrowOnBadRequest = true; });
var app = builder.Build();

app.UseMiddleware<BadHttpRequestExceptionMiddleware>();
app.MapUserEndpoints();

app.Run();