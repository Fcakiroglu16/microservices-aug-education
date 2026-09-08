var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");



var sqlServer = builder.AddContainer("sqlserver", "mcr.microsoft.com/mssql/server")
    .WithEnvironment("ACCEPT_EULA", "Y")
    .WithEnvironment("MSSQL_SA_PASSWORD", "YourStrong!Passw0rd")
    .WithEnvironment("MSSQL_PID", "Developer")
    .WithEndpoint(port: 1433, targetPort: 1433, name: "sql");

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin();

var webApplication2 = builder.AddProject<Projects.WebApplication2>("webapplication2")
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);


builder.AddProject<Projects.WebApplication1>("webapplication1")
    .WithReference(webApplication2)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(webApplication2)
    .WaitFor(redis)
    .WaitFor(rabbitmq);

builder.Build().Run();
