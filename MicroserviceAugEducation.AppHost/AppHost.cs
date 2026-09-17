using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithLifetime(ContainerLifetime.Persistent);

var observabilityTwo = builder.AddProject<ObservabilityTwo_API>("observabilitytwo")
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);

builder.AddProject<ObservabilityOne_API>("observabilityone")
    .WithReference(observabilityTwo)
    .WaitFor(observabilityTwo)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);

builder.Build().Run();
