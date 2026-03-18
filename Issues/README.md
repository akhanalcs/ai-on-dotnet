**Title:**  
Dashboard Date/Time Incorrect

**Description:**  
The dashboard is displaying incorrect dates for job executions. My server is in EST, my local machine is in EST, and I have set EST in the config, but the dashboard shows job dates as 17.03.2026 when the actual date today is 3/18/2026. This is a day late and does not match the expected time zone.

```cs
builder.Services.AddTickerQ(options =>
{
    options.ConfigureScheduler(schedulerOptions =>
    {
        schedulerOptions.SchedulerTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
    //... and so on
```

Additionally, the date format shown is DD.MM.YYYY, which is not standard for US users. I would prefer the American date format MM/DD/YYYY.

**Screenshot:**  
![Dashboard Date Issue](https://user-images.githubusercontent.com/your-screenshot-link.png)  
*(See attached screenshot: chart shows 17.03.2026, but system date is 3/18/2026)*

**Environment:**  
- Server Timezone: EST  
- Local Machine Timezone: EST  
- Config Timezone: EST  
- TickerQ Dashboard v9.1.1  
- .NET 9


--===========
**Title:**  
Dashboard Enhancement: Filter and View Jobs by Status

**Description:**  
Currently, the dashboard displays counts for job statuses like "Done", "DueDone", "Failed", etc., but there is no way to view which specific jobs failed or succeeded. To find failed jobs, I have to click into each cron job individually, which is cumbersome when there are many jobs.

It would be much more useful if the dashboard provided a way to filter and view jobs based on their status. For example, a user should be able to quickly see a list of all failed jobs, all completed jobs, etc., from the dashboard based on their status.

**Request:**  
- Add a feature to filter and display jobs by status (e.g., Failed, Done, DueDone) in the dashboard.
- Allow users to easily identify which jobs are in each status without having to click into each cron job.

**Value:**  
This enhancement would make it significantly easier to monitor, troubleshoot, and manage jobs, especially in environments with many cron jobs.
