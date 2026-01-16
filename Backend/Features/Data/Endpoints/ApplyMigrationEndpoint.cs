using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using PureTCOWebApp.Data;

public class ApplyMigrationEndpoint(ApplicationDbContext db) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/apply-migrations");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
    {
        // Get pending migrations before applying them
        var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
        var pendingCount = pendingMigrations.Count();

        // Apply pending migrations
        await db.Database.MigrateAsync();

        // Get all applied migrations
        var appliedMigrations = await db.Database.GetAppliedMigrationsAsync();

        // Generate HTML response
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Database Migrations</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; }}
        .container {{ max-width: 800px; margin: 0 auto; }}
        .success {{ color: #28a745; }}
        .info {{ color: #007bff; }}
        .migration-list {{ background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin: 20px 0; }}
        .migration-item {{ margin: 5px 0; padding: 8px; background-color: white; border-left: 4px solid #007bff; }}
        .newly-applied {{ border-left-color: #28a745; }}
        h1 {{ color: #333; }}
        h2 {{ color: #666; }}
        .summary {{ background-color: #e9ecef; padding: 15px; border-radius: 5px; margin-bottom: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>Database Migration Status</h1>
        <div class='summary'>
            <p class='success'><strong>✓ Migration completed successfully!</strong></p>
            <p class='info'>Applied {pendingCount} new migration(s)</p>
            <p class='info'>Total migrations in database: {appliedMigrations.Count()}</p>
            <p><strong>Database:</strong> {db.Database.GetDbConnection().Database}</p>
            <p><strong>Timestamp:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
        </div>
        
        <h2>Applied Migrations</h2>
        <div class='migration-list'>";

        if (appliedMigrations.Any())
        {
            foreach (var migration in appliedMigrations.OrderBy(m => m))
            {
                var isNewlyApplied = pendingMigrations.Contains(migration);
                var cssClass = isNewlyApplied ? "migration-item newly-applied" : "migration-item";
                var indicator = isNewlyApplied ? "🆕 " : "✓ ";
                html += $@"
            <div class='{cssClass}'>
                {indicator}{migration}
            </div>";
            }
        }
        else
        {
            html += @"
            <div class='migration-item'>
                No migrations found in database.
            </div>";
        }

        html += @"
        </div>
    </div>
</body>
</html>";

        await Send.StringAsync(html, contentType: "text/html", cancellation: ct);
    }
    catch (Exception ex)
    {
        var errorHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Migration Error</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; }}
        .container {{ max-width: 800px; margin: 0 auto; }}
        .error {{ color: #dc3545; background-color: #f8d7da; padding: 15px; border-radius: 5px; border: 1px solid #f5c6cb; }}
        h1 {{ color: #333; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>Migration Error</h1>
        <div class='error'>
            <p><strong>❌ Migration failed:</strong></p>
            <p>{ex.Message}</p>
            <p><strong>Timestamp:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
        </div>
    </div>
</body>
</html>";

        await Send.StringAsync(errorHtml, contentType: "text/html", cancellation: ct);
    }}
}