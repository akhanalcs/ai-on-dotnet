## Update & Solution

I was able to achieve the desired behavior by combining two key approaches:

1. **Always call** `options.IgnoreSeedDefinedCronTickers();`  
   This is crucial! If you don’t include this line, TickerQ will automatically seed (and overwrite) cron tickers in the database even if you’ve already configured them via the dashboard. For example, when I commented out `[TickerFunction]` attribute on a function, it wiped out my custom cron configuration that I had setup from the dashboard. 

2. **Register the TickerQ operational store only in non-local environments**  
   In local development, I skip calling `options.AddOperationalStore(...)`, so TickerQ doesn’t touch the shared dev database. This lets me manually queue jobs for testing/debugging, while still using my app’s real connection string (via Dapper) for business logic. In other environments, the operational store is registered as usual.

**This approach gives me:**
- Full control over when/what jobs are queued in local development (no accidental pollution or overwrites in the shared DB).
- The ability to use the real database for my app logic, but keep TickerQ’s job state isolated.
- No more confusion or lost configuration in the cron ticker tables.

**This is also helpful for teams using EF Core for all DB operations:**  
You can keep TickerQ’s context isolated to the shared DB, but use your own AppDbContext for everything else, even in local development.

---

### Sample Code (for .NET 9, Oracle, TickerQ 9.1):

```csharp
var environment = builder.Environment.EnvironmentName;
var conStr = builder.Configuration.ConnectionString("MY_CONN_STRING");

builder.Services.AddTickerQ(options =>
{
    // Register global exception handler for all job execution exceptions
    options.SetExceptionHandler<JobsExceptionHandler>();

    options.ConfigureScheduler(schedulerOptions =>
    {
        schedulerOptions.MaxConcurrency = 1;  // Maximum number of concurrent worker threads.  Match to server's logical processor count. I got it by running this in command: "wmic cpu get NumberOfCores,NumberOfLogicalProcessors"
        schedulerOptions.SchedulerTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        schedulerOptions.IdleWorkerTimeOut = TimeSpan.FromMinutes(1); // Time before idle workers are shut down. Shutdown quickly.
        schedulerOptions.FallbackIntervalChecker = TimeSpan.FromMinutes(2); // Check less frequently
    });

    // CRUCIAL: Prevents TickerQ from seeding/overwriting cron tickers in DB
    options.IgnoreSeedDefinedCronTickers();

    // Only register the operational store in non-local environments
    if (!environment.Equals("Local", StringComparison.OrdinalIgnoreCase))
    {
        options.AddOperationalStore(efOptions =>
        {
            efOptions.UseTickerQDbContext<CustomTickerQDbContext>(optionsBuilder =>
            {
                optionsBuilder.UseOracle(conStr, oracleOptions =>
                {
                    oracleOptions.MigrationsHistoryTable("MY_SCHEDULER_EFMIGRATIONSHISTORY", "MY_SCHEMA");
                });
            });
            efOptions.SetDbContextPoolSize(6);  // Match Max Pool Size in connection string
        });
    }

    options.AddDashboard(dashboardOptions =>
    {
        dashboardOptions.SetBasePath("/tickerq/dashboard");
        dashboardOptions.WithNoAuth();
    });
});
```

Thanks for the pointers—this workflow should help others in similar situations!

---

The tickerq 9.2.5 migration is not working.

```sql
DECLARE
    v_Count INTEGER;
BEGIN
SELECT COUNT(*) INTO v_Count FROM "MY_SCHEMA"."MY_SCHEDULER_EFMIGRATIONSHISTORY" WHERE "MigrationId" = N'20260330163420_TQNet925Upgrade';
IF v_Count = 0 THEN

    EXECUTE IMMEDIATE 
    'ALTER TABLE "MY_SCHEMA"."MY_SCHEDULER_CRONTICKERS" ADD "IsEnabled" BOOLEAN DEFAULT False NOT NULL'
    ;
 END IF;
END;

/

DECLARE
    v_Count INTEGER;
BEGIN
SELECT COUNT(*) INTO v_Count FROM "MY_SCHEMA"."MY_SCHEDULER_EFMIGRATIONSHISTORY" WHERE "MigrationId" = N'20260330163420_TQNet925Upgrade';
IF v_Count = 0 THEN

    EXECUTE IMMEDIATE 
    'INSERT INTO "MY_SCHEMA"."MY_SCHEDULER_EFMIGRATIONSHISTORY" ("MigrationId", "ProductVersion")
    VALUES (N''20260330163420_TQNet925Upgrade'', N''9.0.13'')'
    ;
 END IF;
END;

/
```
> dotnet ef migrations add TQNet925Upgrade -c TickerQDbContext -o Data\Migrations

> dotnet ef migrations script --idempotent 20260313170226_TQNet9Upgrade 20260330163420_TQNet925Upgrade -c TickerQDbContext -o Data\MigrationScripts\TQNet925Upgrade.sql



----

**Title:**  
Source generator fails to resolve input types with namespace for new jobs – urgent production blocker

**Body:**

Hi TickerQ team,

We are facing a critical issue with the TickerQ source generator that is blocking our production deployment.

**Background:**  
We have several jobs in our scheduler project (let’s call it `My.Scheduler`). Until recently, everything was working fine. For example, we have a job like this:

```csharp
namespace My.Scheduler.Jobs.DataCopy.TempForecastsSync;

public class TempForecastsSyncJobInput
{
    public int LookbackDaysFromMaxDate { get; set; }
}

public class TempForecastsSyncJob
{
    [TickerFunction(functionName: nameof(SyncFetchedTempForecasts))]
    public async Task SyncFetchedTempForecasts(TickerFunctionContext<TempForecastsSyncJobInput> context, CancellationToken cancellationToken)
    {
        // ...
    }
}
```

**Problem:**  
After adding new jobs and new input types (like `TempForecastsSyncJobInput`), the build started failing with errors in the generated `TickerQInstanceFactory.g.cs` file. The errors look like this:

```
error CS0246: The type or namespace name 'TempForecastsSyncJobInput' could not be found (are you missing a using directive or an assembly reference?)
error CS1503: Argument 1: cannot convert from 'TickerQ.Utilities.Base.TickerFunctionContext<TempForecastsSyncJobInput>' to 'TickerQ.Utilities.Base.TickerFunctionContext<My.Scheduler.Jobs.DataCopy.TempForecastsSync.TempForecastsSyncJobInput>'
```

The generated code is referencing `TempForecastsSyncJobInput` without the namespace, even though the class is defined in a namespace. This only started happening after adding new jobs and input types. Previously, all jobs (with similar structure) worked fine.

**What I’ve tried:**
- Cleaned and rebuilt the solution
- Deleted all `bin` and `obj` folders
- Verified there are no duplicate class names

**Impact:**  
This is a major blocker for us. The only workaround is to remove the namespace from the input object, which is not acceptable for code organization and maintainability. This issue is very frustrating, as it was working fine for all other jobs until we added new ones.

**Questions:**
- Why did this work previously, but now fails for new jobs?
- Is there a workaround for this namespace resolution issue in the source generator?
- Is there a way to force the generator to use fully qualified type names for input types?

**Urgency:**  
We are going to production next week and this issue is wasting a lot of time. Any urgent help or workaround would be greatly appreciated!

Thank you!



