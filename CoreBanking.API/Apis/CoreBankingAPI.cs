

namespace CoreBanking.API.Apis;

/// <summary>
/// API for Core Banking
/// </summary>
public static class CoreBankingAPI
{
    public static IEndpointRouteBuilder MapCoreBankingApi(this IEndpointRouteBuilder builder)
    {
        var vApi = builder.NewVersionedApi("CoreBanking");
        var v1 = vApi.MapGroup("/api/v{version:apiVersion}/corebanking").HasApiVersion(1, 0);

        v1.MapGet("/customers", GetCustomers);
        v1.MapGet("/customers/{id:guid}", GetCustomerById);
        v1.MapPost("/customers", CreateCustomer);

        v1.MapGet("/accounts", GetAccounts);
        v1.MapGet("/accounts/{id:guid}", GetAccountById);
        v1.MapPost("/accounts", CreateAccount);

        v1.MapPut("/accounts/{id:guid}/deposit", Deposit);
        v1.MapPut("/accounts/{id:guid}/withdraw", Withdraw);
        v1.MapPut("/accounts/{id:guid}/transfer", Transfer);

        return builder;
    }



    #region Transaction APIs

    private static async Task<Results<Ok, BadRequest>> Transfer(
        [AsParameters] CoreBankingServices services,
        Guid id,
        TransferRequest transferRequest)
    {
        if (id == Guid.Empty)
        {
            services.Logger.LogWarning("Account ID is required for transfer");
            return TypedResults.BadRequest();
        }

        if (string.IsNullOrEmpty(transferRequest.ToAccountNumber) || transferRequest.Amount <= 0)
        {
            services.Logger.LogWarning("Invalid transfer request");
            return TypedResults.BadRequest();
        }

        var fromAccount = await services.DbContext.Accounts.FindAsync(id);
        if (fromAccount == null)
        {
            services.Logger.LogWarning("From account not found");
            return TypedResults.BadRequest();
        }

        if (fromAccount.Balance < transferRequest.Amount)
        {
            services.Logger.LogWarning("Insufficient funds for transfer");
            return TypedResults.BadRequest();
        }

        var toAccount = await services.DbContext.Accounts.FirstOrDefaultAsync(a => a.Number == transferRequest.ToAccountNumber);
        if (toAccount == null)
        {
            services.Logger.LogWarning("To account not found");
            return TypedResults.BadRequest();
        }

        fromAccount.Balance -= transferRequest.Amount;
        toAccount.Balance += transferRequest.Amount;

        try
        {
            var now = DateTime.UtcNow;

            services.DbContext.Transactions.Add(new Transaction
            {
                Id = Guid.CreateVersion7(),
                AccountId = fromAccount.Id,
                Amount = transferRequest.Amount,
                Type = TransactionTypes.Withdraw,
                DateUtc = now
            });

            services.DbContext.Transactions.Add(new Transaction
            {
                Id = Guid.CreateVersion7(),
                AccountId = toAccount.Id,
                Amount = transferRequest.Amount,
                Type = TransactionTypes.Deposit,
                DateUtc = now
            });

            await services.DbContext.SaveChangesAsync();

            services.Logger.LogInformation("Transfer successful from account {FromAccountId} to account {ToAccountNumber}", id, transferRequest.ToAccountNumber);
            return TypedResults.Ok();
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error processing transfer from account {FromAccountId} to account {ToAccountNumber}", id, transferRequest.ToAccountNumber);
            return TypedResults.BadRequest();
        }
    }

    private static async Task<Results<Ok<Account>, BadRequest>> Withdraw(
        [AsParameters] CoreBankingServices services,
        Guid id, WithdrawalRequest withdrawalRequest)
    {
        if (id == Guid.Empty)
        {
            services.Logger.LogWarning("Account ID is required for withdrawal");
            return TypedResults.BadRequest();
        }

        var account = await services.DbContext.Accounts.FindAsync(id);
        if (account == null)
        {
            services.Logger.LogWarning("Account not found");
            return TypedResults.BadRequest();
        }

        if (account.Balance < withdrawalRequest.Amount)
        {
            services.Logger.LogWarning("Insufficient funds for withdrawal");
            return TypedResults.BadRequest();
        }
        account.Balance -= withdrawalRequest.Amount;

        try
        {
            services.DbContext.Transactions.Add(new Transaction
            {
                Id = Guid.CreateVersion7(),
                AccountId = account.Id,
                Amount = withdrawalRequest.Amount,
                Type = TransactionTypes.Withdraw,
                DateUtc = DateTime.UtcNow
            });

            services.DbContext.Accounts.Update(account);
            await services.DbContext.SaveChangesAsync();

            services.Logger.LogInformation("Withdrawal successful");
            return TypedResults.Ok(account);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error processing withdrawal for account {AccountId}", id);
            return TypedResults.BadRequest();
        }
    }

    public static async Task<Results<Ok<Account>, BadRequest>> Deposit(
        [AsParameters] CoreBankingServices services, 
        Guid id, DepositionRequest depositionRequest)
    {
        if (id == Guid.Empty)
        {
            services.Logger.LogWarning("Account ID is required for deposit");
            return TypedResults.BadRequest();
        }

        if (depositionRequest.Amount <= 0)
        {
            services.Logger.LogWarning("Deposit amount must be greater than zero");
            return TypedResults.BadRequest();
        }

        var account = await services.DbContext.Accounts.FindAsync(id);

        if (account == null)
        {
            services.Logger.LogWarning("Account not found");
            return TypedResults.BadRequest();
        }
        account.Balance += depositionRequest.Amount;

        try
        {
            services.DbContext.Transactions.Add(new Transaction
            {
                Id = Guid.CreateVersion7(),
                AccountId = account.Id,
                Amount = depositionRequest.Amount,
                Type = TransactionTypes.Deposit,
                DateUtc = DateTime.UtcNow
            });
            services.DbContext.Accounts.Update(account);
            await services.DbContext.SaveChangesAsync();

            services.Logger.LogInformation("Deposited successfully");
            return TypedResults.Ok(account);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error processing deposit for account {AccountId}", id);
            return TypedResults.BadRequest();
        }
    }
    #endregion Transaction APIs

    #region Account APIs

    public static async Task<Results<Ok<Account>, BadRequest>> CreateAccount(
        [AsParameters] CoreBankingServices services,
        Account account)
    {
        if (account.CustomerId == Guid.Empty)
        {
            services.Logger.LogWarning("Customer ID is required for account creation");
            return TypedResults.BadRequest();
        }

        account.Id = account.Id == Guid.Empty ? Guid.CreateVersion7() : account.Id;
        account.Number = GenerateAccountNumber();
        
        services.DbContext.Accounts.Add(account);
        await services.DbContext.SaveChangesAsync();

        return TypedResults.Ok(account);
    }

    private static string GenerateAccountNumber()
    {
        return DateTime.UtcNow.Ticks.ToString();
    }

    private static async Task<Ok<PaginationResponse<Account>>> GetAccounts(
        [AsParameters] CoreBankingServices services,
        [AsParameters] PaginationRequest pagination,
        Guid? customerId = null)
    {
        IQueryable<Account> accounts = services.DbContext.Accounts;

        if (customerId.HasValue)
        {
            accounts = accounts.Where(a => a.CustomerId == customerId.Value);
        }

        return TypedResults.Ok(new PaginationResponse<Account>(
            pagination.PageIndex,
            pagination.PageSize,
            await accounts.CountAsync(),
            await accounts
                .OrderBy(c => c.Id)
                .Skip(pagination.PageIndex * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync()
        ));
    }

    private static async Task<Results<Ok<Account>, BadRequest>> GetAccountById(
        [AsParameters] CoreBankingServices services,
        Guid id)
    {
        var account = await services.DbContext.Accounts.FindAsync(id);
        if (account == null)
        {
            services.Logger.LogWarning("Customer with ID {Id} not found", id);
            return TypedResults.BadRequest(); // Return an empty customer if not found
        }

        return TypedResults.Ok(account);
    }

    #endregion Account APIs

    #region Customer APIs
    public static async Task<Results<Ok<Customer>, BadRequest>> CreateCustomer(
        [AsParameters] CoreBankingServices services,
        Customer customer)
    {
        if (string.IsNullOrEmpty(customer.Name))
        {
            services.Logger.LogWarning("Customer name is required");

            return TypedResults.BadRequest();
        }
        customer.Address ??= "";

        if (customer.Id == Guid.Empty)
        {
            customer.Id = Guid.CreateVersion7();
        }

        services.DbContext.Customers.Add(customer);
        await services.DbContext.SaveChangesAsync();

        services.Logger.LogInformation("Customer created");

        return TypedResults.Ok(customer);
    }

    private static async Task<Ok<PaginationResponse<Customer>>> GetCustomers(
        [AsParameters] CoreBankingServices services, 
        [AsParameters] PaginationRequest pagination)
    {
        return TypedResults.Ok(new PaginationResponse<Customer>(
            pagination.PageIndex, 
            pagination.PageSize, 
            await services.DbContext.Customers.CountAsync(),
            await services.DbContext.Customers
                .OrderBy(c => c.Name)
                .Skip(pagination.PageIndex * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync()
        ));
    }

    private static async Task<Results<Ok<Customer>, BadRequest>> GetCustomerById(
        [AsParameters] CoreBankingServices services,
        Guid Id)
    {
        var customer = await services.DbContext.Customers.FindAsync(Id);
        if (customer == null)
        {
            services.Logger.LogWarning("Customer with ID {Id} not found", Id);
            return TypedResults.BadRequest(); // Return an empty customer if not found
        }

        return TypedResults.Ok(customer);
    }

    #endregion Customer APIs
}

#region Classes for Requests

public class DepositionRequest
{
    public decimal Amount { get; set; }
}

public class  WithdrawalRequest
{
    public decimal Amount { get; set; }
}

public class TransferRequest
{
    public string ToAccountNumber { get; set; } = default!;
    public decimal Amount { get; set; }
}

#endregion