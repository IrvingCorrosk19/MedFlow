using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthChecks.NpgSql;

namespace MedFlow.Infrastructure.Health;

public static class MedFlowHealthChecks
{
    public static IHealthChecksBuilder AddMedFlowHealthChecks(this IHealthChecksBuilder builder, IConfiguration configuration)
    {
        var connStr = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connStr))
            builder.AddNpgSql(connStr, name: "database", failureStatus: HealthStatus.Unhealthy);

        return builder;
    }
}
