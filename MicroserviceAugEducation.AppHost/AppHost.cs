var builder = DistributedApplication.CreateBuilder(args);

var webApplication2 = builder.AddProject<Projects.WebApplication2>("webapplication2");

builder.AddProject<Projects.WebApplication1>("webapplication1")
    .WithReference(webApplication2)
    .WaitFor(webApplication2);

builder.Build().Run();
