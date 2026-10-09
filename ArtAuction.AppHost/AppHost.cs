using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddAzureServiceBus("messaging")
    .RunAsEmulator(emulator =>
    {
        emulator.WithHostPort(7777);
    });

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(azurite =>
    {
        azurite.WithBlobPort(27000)
               .WithTablePort(27001);
    });


var api = builder.AddProject<Projects.ArtAuction_Api>("api")
                 .PublishAsDockerFile();

var worker = builder.AddProject<Projects.ArtAuction_Worker>("worker")
                    .PublishAsDockerFile();


builder.Build().Run();
