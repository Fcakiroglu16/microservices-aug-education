var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisInsight();

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
