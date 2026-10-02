var builder = DistributedApplication.CreateBuilder(args);

// See https://learn.microsoft.com/dotnet/aspire/azure/local-provisioning#configuration
// for instructions providing configuration values
var openai = builder.AddAzureOpenAI("openai");

// The template's gpt-4o-mini (2024-07-18) Standard deployment was retired on 2026-03-31.
openai.AddDeployment(
    name: "chat",
    modelName: "gpt-5.4-mini",
    modelVersion: "2026-03-17");

openai.AddDeployment(
    name: "text-embedding-3-small",
    modelName: "text-embedding-3-small",
    modelVersion: "1");

var vectorDB = builder.AddQdrant("vectordb")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var markitdown = builder.AddContainer("markitdown", "mcp/markitdown")
    .WithArgs("--http", "--host", "0.0.0.0", "--port", "3001")
    .WithHttpEndpoint(targetPort: 3001, name: "http");

var webApp = builder.AddProject<Projects.RagChat_Web>("aichatweb-app");
webApp
    .WithReference(openai)
    .WaitFor(openai);
webApp
    .WithReference(vectorDB)
    .WaitFor(vectorDB);
webApp
    .WithEnvironment("MARKITDOWN_MCP_URL", markitdown.GetEndpoint("http"));

builder.Build().Run();
