using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace PmsAI.Api.Db;

public static class SqlSugarSetup
{
    public static IServiceCollection AddSqlSugar(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SqlServer");
        services.AddScoped<ISqlSugarClient>(s =>
        {
            var db = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = connectionString,
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            db.Aop.OnLogExecuting = (sql, pars) =>
            {
                var logger = s.GetRequiredService<ILogger<SqlSugarClient>>();
                logger.LogDebug("SQL: {Sql}", sql);
            };
            return db;
        });
        return services;
    }
}
