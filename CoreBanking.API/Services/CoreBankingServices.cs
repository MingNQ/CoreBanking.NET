using CoreBanking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.API.Services;

/// <summary>
/// Store the services which used in the Core Banking API
/// </summary>
public class CoreBankingServices(CoreBankingDbContext dbContext, ILogger<CoreBankingServices> logger)
{
    public CoreBankingDbContext DbContext { get; } = dbContext;
    public ILogger<CoreBankingServices> Logger => logger;
}
