using System.Diagnostics;
using dot_net_api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dot_net_api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController(QueueDbContext db, ILogger<HealthController> logger) : ControllerBase
{
    /// <summary>
    /// Tests the database connection by opening a connection and running a trivial query.
    /// GET /health/db
    /// </summary>
    [HttpGet("db")]
    public async Task<IActionResult> TestDatabaseConnection(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            // Open explicitly (rather than CanConnectAsync) so the real error surfaces on failure.
            await db.Database.OpenConnectionAsync(ct);

            var serverVersion = await db.Database
                .SqlQueryRaw<string>("SELECT version() AS \"Value\"")
                .SingleAsync(ct);

            return Ok(new
            {
                status = "healthy",
                database = db.Database.GetDbConnection().Database,
                serverVersion,
                elapsedMs = sw.ElapsedMilliseconds
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database connection test failed");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { status = "unhealthy", error = ex.Message, elapsedMs = sw.ElapsedMilliseconds });
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}
