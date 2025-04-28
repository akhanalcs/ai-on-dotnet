var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.AIChat>("my-ai-chat");

builder.Build().Run();
