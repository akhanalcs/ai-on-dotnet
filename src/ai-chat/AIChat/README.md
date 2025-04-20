# AI Chat with Custom Data
This project is an AI chat application that demonstrates how to chat with custom data using an AI language model.  

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

### Terminology
1. AI service provider (`--provider`)
    - The service that provides the AI model. For example, OpenAI, GitHub Models, Azure OpenAI, etc.
2. Vector store (`--vector-store`)
    - Place to store information that can be retrieved by the AI system using Semantic search.

## Create .NET AI app
```bash
$ cd src/ai-chat
$ pwd
/Users/ashishkhanal/RiderProjects/ai-on-dotnet/src/ai-chat
$ dotnet new aichatweb --Framework net9.0 --provider githubmodels --vector-store local --name AIChat

# https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-sln#examples
# My previous project: https://github.com/akhanalcs/cs-coding-interview/blob/main/README.md#setup-project
$ dotnet new sln
$ dotnet sln add AIChat/AIChat.csproj --solution-folder src
Project `AIChat/AIChat.csproj` added to the solution.
```

Open the solution by right-clicking `ai-chat.sln` > Open Solution.

## Configure AI model provider (I had chosen `githubmodels`)
https://docs.github.com/en/github-models/prototyping-with-ai-models#experimenting-with-ai-models-using-the-api

### Taking a look around
Go to models marketplace and select a model.

<img width="1000" alt="image" src="../screenshots/github-models-select.png">

After you select the model, it opens the AI model playground which is a free resource that allows you to adjust model parameters and submit prompts to see how a model responds.
It allows you to experiment with different models and parameters to find the best fit for your use case.

To adjust parameters for the model, in the playground, select the Parameters tab in the sidebar.
1. Frequency Penalty (think of it like penalizing because of frequent same text in the response)
    - This decreases the likelihood of repeating the exact same text in a response.
2. Presence Penalty (think of it like penalizing because of presence of same text in the response)
    - This increases the likelihood of introducing new topics in a response.

<p>
  <img alt="image" src="../screenshots/github-model-params1.png" width="350">
&nbsp;
  <img alt="image" src="../screenshots/github-model-params2.png" width="350">
</p>

To see code that corresponds to the parameters that you selected, switch from the Chat tab to the Code tab.

<img width="1000" alt="image" src="../screenshots/github-model-code.png">

### Using the model
Click > **Use this model** in the top right corner of the model page.

To use models hosted by GitHub Models, you will need to create a GitHub personal access token.

Steps. [Reference](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens#creating-a-fine-grained-personal-access-token)
- Go to https://github.com/settings/personal-access-tokens
- Token name: `GITHUB_AI_MODEL_TOKEN`, Description: `Token to authenticate with the GitHub AI model.`
- Expiration: 30 days.
- Repository access: Only select repositories > `ai-on-dotnet` (this repository).
- Permissions: Account permissions > Models > Access: Read-only.
- Click **Generate token**.

  <img width="350" alt="image" src="../screenshots/generate-github-pat.png">
- Copy the token. You won't be able to see it again.
- From the command line, set token for this project using .NET User Secrets by running the following commands:
  ```sh
  $ cd AIChat
  $ dotnet user-secrets set GitHubModels:Token <YOUR-TOKEN>
  ```
- Right click project > Tools > .NET User Secrets. It opens up `secrets.json` file. Verify that the token is set.
  ```json
  {
    "GitHubModels:Token": "github_pat_..."
  }
  ```

## Run the app
Click the green play button in Rider to run the app.

<img width="250" alt="image" src="../screenshots/rider-play.png">

The console will show the following output:
```bash
/Users/ashishkhanal/RiderProjects/ai-on-dotnet/src/ai-chat/AIChat/bin/Debug/net9.0/AIChat
info: AIChat.Services.Ingestion.DataIngestor[0]
      Ingestion is up-to-date
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7080
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5068
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
info: Microsoft.Hosting.Lifetime[0]
      Content root path: /Users/ashishkhanal/RiderProjects/ai-on-dotnet/src/ai-chat/AIChat
```

Some files are generated in the `AIChat` folder:

<img width="250" alt="image" src="../screenshots/generated-files.png">



