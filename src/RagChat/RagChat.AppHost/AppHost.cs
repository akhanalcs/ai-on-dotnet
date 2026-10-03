var builder = DistributedApplication.CreateBuilder(args);

// See https://learn.microsoft.com/dotnet/aspire/azure/local-provisioning#configuration
// for instructions providing configuration values
var openai = builder.AddAzureOpenAI("openai");

// The template's gpt-4o-mini (2024-07-18) Standard deployment was retired on 2026-03-31.
openai.AddDeployment(
    name: "chat",
    modelName: "gpt-5-mini",
    modelVersion: "2025-08-07")
    .WithProperties(d => d.SkuName = "GlobalStandard"); // Cheapest for learning; use DataZoneStandard/Standard when data residency matters

openai.AddDeployment(
    name: "text-embedding-3-small",
    modelName: "text-embedding-3-small",
    modelVersion: "1");

// Azure AI Search: hybrid (keyword + vector) search + semantic ranker. The web app gets index read/write roles.
var search = builder.AddAzureSearch("search");

var webApp = builder.AddProject<Projects.RagChat_Web>("aichatweb-app");
webApp
    .WithReference(openai)
    .WaitFor(openai);
webApp
    .WithReference(search)
    .WaitFor(search);

builder.Build().Run();
