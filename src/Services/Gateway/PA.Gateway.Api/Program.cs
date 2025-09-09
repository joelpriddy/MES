var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
builder.Services.AddHealthChecks();

var app = builder.Build();
app.MapGrpcReflectionService();
app.MapHealthChecks("/healthz");
app.MapGet("/", () => "Gateway running");
app.Run();
