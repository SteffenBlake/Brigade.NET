var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Brigade_Net_Example_Web>("web", launchProfileName: "http")
	.WithHttpHealthCheck("/health");

builder.Build().Run();
