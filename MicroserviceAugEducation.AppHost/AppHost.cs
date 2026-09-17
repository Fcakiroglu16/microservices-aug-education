using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var observabilityTwo = builder.AddProject<ObservabilityTwo_API>("observabilitytwo");

builder.AddProject<ObservabilityOne_API>("observabilityone")
    .WithReference(observabilityTwo)
    .WaitFor(observabilityTwo);

builder.Build().Run();
