namespace CoreBanking.API.Services;

/// <summary>
/// Store the services which used in the Core Banking API
/// </summary>
public class CoreBankingServices(ILogger<CoreBankingServices> logger)
{
    public ILogger<CoreBankingServices> Logger => logger;
}
