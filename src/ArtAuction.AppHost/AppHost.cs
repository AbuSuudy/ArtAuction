var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(azurite =>
    {
        azurite.WithBlobPort(27000)
               .WithTablePort(27001);
    });

var tables = storage.AddTables("tables");

var rabbitmq = builder.AddRabbitMQ("rabbit-mq").WithManagementPlugin();

var api = builder.AddProject<Projects.ArtAuction_API>("api")
    .WithReference(rabbitmq)
    .PublishAsDockerFile();

var consumer = builder.AddProject<Projects.ArtAuction_Consumer>("consumer")
    .WithReference(rabbitmq)
    .WithReference(tables)
    .PublishAsDockerFile();

builder.Build().Run();
