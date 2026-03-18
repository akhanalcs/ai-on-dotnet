Oracle Migration Issue: BOOLEAN Column and Default Value Not Supported, Dashboard Broken

**Description:**  
When upgrading TickerQ to v9.2.2 and running EF Core migrations against Oracle, the migration generator creates a column with type `BOOLEAN` and default value `True`. This is not supported by Oracle, which only allows `NUMBER(1)` or `CHAR(1)` for boolean-like columns and expects `DEFAULT 1` or `DEFAULT 'Y'`.

**Migration Snippet:**
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<bool>(
        name: "IsEnabled",
        schema: "MY_APP_SCHEMA",
        table: "SCHEDULER_CRONTICKERS",
        type: "BOOLEAN",
        nullable: false,
        defaultValue: true);
}
```
**Generated SQL:**
```sql
ALTER TABLE "MY_APP_SCHEMA"."SCHEDULER_CRONTICKERS" ADD "IsEnabled" BOOLEAN DEFAULT True NOT NULL
```
**Oracle Error:**
```
ORA-00904: "TRUE": invalid identifier
```

**Impact:**  
- The dashboard shows 0 cron jobs when there are actually jobs present.
- Disabling a cron job throws HTTP 500 errors.
- The app is broken for Oracle users after this migration.

**Request:**  
- Ensure migrations generate valid Oracle SQL for boolean columns.
- Document the expected column type and value for Oracle users, and update the dashboard/backend to handle `NUMBER(1)` or `CHAR(1)` as boolean.

**Environment:**  
- TickerQ v9.2.2
- Oracle Database
- EF Core

**Screenshots:**  

