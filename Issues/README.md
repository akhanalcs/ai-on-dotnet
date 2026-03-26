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

