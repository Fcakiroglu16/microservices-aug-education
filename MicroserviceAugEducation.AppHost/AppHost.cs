var builder = DistributedApplication.CreateBuilder(args);

// Keycloak'un kendi verisini tuttuğu PostgreSQL sunucusu
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin();

var keycloakDb = postgres.AddDatabase("keycloakdb");


var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WaitFor(keycloakDb)
    .WithEnvironment(context =>
    {
        var endpoint = postgres.Resource.PrimaryEndpoint;

        context.EnvironmentVariables["KC_DB"] = "postgres";
        context.EnvironmentVariables["KC_DB_URL"] = ReferenceExpression.Create(
            $"jdbc:postgresql://{endpoint.Property(EndpointProperty.Host)}:{endpoint.Property(EndpointProperty.Port)}/{keycloakDb.Resource.DatabaseName}");
        context.EnvironmentVariables["KC_DB_USERNAME"] = (object?)postgres.Resource.UserNameParameter ?? "postgres";
        context.EnvironmentVariables["KC_DB_PASSWORD"] = postgres.Resource.PasswordParameter;
    });

builder.Build().Run();
