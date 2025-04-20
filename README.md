# ai-on-dotnet
Building AI apps with .NET.

## Resources
- Watch the video and follow along with docs below:
  - [ASP.NET Community Standup - AI-powered Blazor web apps with the new .NET AI template](https://www.youtube.com/live/9cwSOyavdSI?si=ddZfiNBftdWDEHjv)
  - [Create a .NET AI app to chat with custom data using the AI app template extensions](https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/ai-templates?tabs=dotnet-cli%2Cconfigure-visual-studio&pivots=github-models)

## Install AI app template
```bash
$ dotnet new install Microsoft.Extensions.AI.Templates
The following template packages will be installed:
   Microsoft.Extensions.AI.Templates

Success: Microsoft.Extensions.AI.Templates::9.4.0-preview.2.25216.9 installed the following templates:
Template Name    Short Name  Language  Tags                            
---------------  ----------  --------  --------------------------------
AI Chat Web App  aichatweb   [C#]      Common/AI/Web/Blazor/.NET Aspire
```

Check out the options using help flag.
```bash
$ dotnet new aichatweb -h
AI Chat Web App (C#)
Author: Microsoft
Description: A project template for creating an AI chat application, which uses retrieval-augmented generation (RAG) to chat with your own data.

Usage:
  dotnet new aichatweb [options] [template options]

Options:
  -n, --name <name>       The name for the output being created. If no name is specified, the name of the output directory is used.
  -o, --output <output>   Location to place the generated output.
  --dry-run               Displays a summary of what would happen if the given command line were run if it would result in a template creation.
  --force                 Forces content to be generated even if it would change existing files.
  --no-update-check       Disables checking for the template package updates when instantiating a template.
  --project <project>     The project that should be used for context evaluation.
  -lang, --language <C#>  Specifies the template language to instantiate.
  --type <project>        Specifies the template type to instantiate.

Template options:
  -F, --Framework <net9.0>                             The target framework for the project.
                                                       Type: choice
                                                         net9.0  Target net9.0
                                                       Default: net9.0
  --provider <azureopenai|githubmodels|ollama|openai>  Type: choice
                                                         azureopenai   Uses Azure OpenAI service
                                                         githubmodels  Uses GitHub Models
                                                         ollama        Uses Ollama with the llama3.2 and all-minilm models
                                                         openai        Uses the OpenAI Platform
                                                       Default: githubmodels
  --vector-store <azureaisearch|local|qdrant>          Type: choice
                                                         local          Uses a JSON file on disk. You can change the implementation to a real vector database before publishing.
                                                         azureaisearch  Uses Azure AI Search. This also avoids the need to define a data ingestion pipeline, since it's managed by Azure AI Search.
                                                         qdrant         Uses Qdrant in a Docker container, orchestrated using Aspire.
                                                       Default: local
  --managed-identity                                   Use managed identity to access Azure services
                                                       Enabled if: (!UseAspire && VectorStore != "qdrant" && (AiServiceProvider == "azureopenai" || 
                                                       AiServiceProvider == "azureaifoundry" || VectorStore == "azureaisearch"))
                                                       Type: bool
                                                       Default: true
  --aspire                                             Create the project as a distributed application using .NET Aspire.
                                                       Type: bool
                                                       Default: false
  -C, --ChatModel <ChatModel>                          Model/deployment for chat completions. Example: gpt-4o-mini
                                                       Type: string
  -E, --EmbeddingModel <EmbeddingModel>                Model/deployment for embeddings. Example: text-embedding-3-small
                                                       Type: string
```

## Create .NET AI app
```bash
$ cd src/ai-chat
$ pwd
/Users/ashishkhanal/RiderProjects/ai-on-dotnet/src/ai-chat
$ dotnet new aichatweb --Framework net9.0 --provider githubmodels --vector-store local --name AIChat

# https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-sln#examples
$ dotnet new sln
$ dotnet sln add AIChat/AIChat.csproj --solution-folder src
Project `AIChat/AIChat.csproj` added to the solution.
```

Open the solution by right-clicking `ai-chat.sln` > Open Solution.



Create the app