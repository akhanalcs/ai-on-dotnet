# AI Assisted Feedback to Feature
This shows how feedback from users is captured, processed, and turned into actionable code changes through a series of automated steps involving 
Microsoft Forms, Power Automate, SharePoint, GitHub, and Azure OpenAI. 

The developer interacts with the system primarily through SharePoint tasks and GitHub for code changes.

Example: https://youtu.be/dL0uvPXYi28?si=wwDeJE52O3dX1hb9 

## Flow
```mermaid
sequenceDiagram
    actor User
    participant MS_Forms as Microsoft Forms
    participant PA_Flow1 as Power Automate (Flow 1: Intake)
    participant SPO_Tasks as SharePoint List (Tasks)
    participant Dev as Developer
    participant PA_Flow2 as Power Automate (Flow 2: Prompt Gen)
    participant Github_API as GitHub API
    participant Azure_OpenAI as Azure OpenAI Service
    participant AI_Codex as AI Coding Assistant (Codex via ChatGPT)
    participant Github_Repo as GitHub Repository

    User->>MS_Forms: Submits Feedback
    MS_Forms->>PA_Flow1: Triggers Flow (New Submission)
    PA_Flow1->>MS_Forms: Get Form Details
    PA_Flow1->>SPO_Tasks: Create Task (Status: New)
    Note over Dev,SPO_Tasks: Developer Monitors Tasks

    Dev->>SPO_Tasks: Reviews & Approves Task (Status: Approved)
    SPO_Tasks->>PA_Flow2: Triggers Flow (Task Approved)
    PA_Flow2->>SPO_Tasks: Get Approved Task Details
    PA_Flow2->>Github_API: Request Code Context (PAT Auth)
    Github_API-->>PA_Flow2: Return Code Context
    PA_Flow2->>Azure_OpenAI: Send (Feedback + Context + System Instructions)
    Azure_OpenAI-->>PA_Flow2: Return Engineered Prompt
    PA_Flow2->>PA_Flow2: URL Encode Prompt & Create Magic Link
    PA_Flow2->>SPO_Tasks: Update Task (Add Magic Link, Status: Prompt Generated)

    Note over Dev,SPO_Tasks: Developer Sees "Prompt Generated" Task
    Dev->>AI_Codex: Clicks Magic Link (Opens Codex with Prompt)
    Note over AI_Codex, Github_Repo: Codex is pre-connected to Repo
    AI_Codex->>Github_Repo: Writes/Modifies Code
    AI_Codex->>Github_Repo: Creates Pull Request (PR)
    Github_Repo-->>Dev: Notifies of New PR

    Dev->>Github_Repo: Reviews PR
    Dev->>Github_Repo: Approves & Merges PR
    Note over Github_Repo: Merge can trigger CI/CD Pipeline for Deployment
```
