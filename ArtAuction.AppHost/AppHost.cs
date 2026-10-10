using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var rabbitmq = builder.AddRabbitMQ("rabbit-mq").WithManagementPlugin();
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(azurite =>
    {
        azurite.WithBlobPort(27000)
               .WithTablePort(27001);
    });

var tables = storage.AddTables("tables");

var api = builder.AddProject<Projects.ArtAuction_Api>("api")
    .WithReference(rabbitmq)
    .PublishAsDockerFile();

var consumer = builder.AddProject<Projects.ArtAuction_Consumer>("consumer")
    .WithReference(rabbitmq)
    .WithReference(tables)
    .PublishAsDockerFile();

builder.Build().Run();
