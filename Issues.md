Disable Automatic CRON Job Queuing during Local Development

When developing locally, my application connects to the shared dev database, which is also used by the dev app server running TickerQ.

Currently, when I launch my local app, it automatically queues cron jobs in the database from my local machine, causing duplicate or unwanted job executions (jobs locked by my local machine + dev server machine). This creates **friction during development**, including confusion, wasted resources, and duplicate processing.

### **Desired Behavior:**
- In local development (or specified environments), prevent automatic registration and queuing of jobs from the local instance.
- Allow jobs to be queued/run only manually via the dashboard or explicit triggers.
- Avoid idle/queued jobs from local instances unless explicitly triggered.

### **Current Workarounds:**
- None available without separate DB.

### **Use Case:**
This would allow developers to test and debug jobs locally without interfering with the shared dev environment or causing duplicate job executions.

### **Environment:**
- TickerQ v9.1.1
- .NET 9
- Local development with shared dev database

### **Request:**
Please add a configuration option/flag to disable automatic job registration/queuing from specified environments (e.g., local development), allowing only manual job execution.