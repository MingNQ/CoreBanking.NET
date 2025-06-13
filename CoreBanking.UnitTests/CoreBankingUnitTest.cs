using CoreBanking.API.Apis;
using CoreBanking.API.Services;
using CoreBanking.Infrastructure.Data;
using CoreBanking.Infrastructure.Entity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreBanking.UnitTests
{
    public class CoreBankingUnitTest : IDisposable
    {
        private SqliteConnection _sqliteConnection = default!;
        private DbContextOptions<CoreBankingDbContext> _dbContextOptions = default!;
        
        public CoreBankingUnitTest()
        {
            // Arrange
            _sqliteConnection = new SqliteConnection("DataSource=:memory:");
            _sqliteConnection.Open();

            _dbContextOptions = new DbContextOptionsBuilder<CoreBankingDbContext>()
                .UseSqlite(_sqliteConnection)
                .Options;

            using var context = new CoreBankingDbContext(_dbContextOptions);
            context.Database.EnsureCreated();
        }

        private CoreBankingDbContext CreateContext() => new(_dbContextOptions);

        private static CoreBankingServices CreateService(CoreBankingDbContext context)
        {
            return new(context, NullLogger<CoreBankingServices>.Instance);
        }

        private static Customer CreateSampleCustomer(string name = "John Doe", string address = "123 Main St")
        {
            return new()
            {
                Id = Guid.NewGuid(),
                Name = name,
                Address = address,
                Accounts = []
            };
        }

        private static Account CreateSampleAccount(Guid customerId, string number = "123456789")
        {
            return new()
            {
                Id = Guid.NewGuid(),
                Number = number,
                CustomerId = customerId
            };
        }

        [Fact]
        public void Create_Customer_UnitTest()
        {
            using var context = CreateContext();
            var services = CreateService(context);
            var customer = CreateSampleCustomer();

            // Act
            var result = CoreBankingAPI.CreateCustomer(services, customer);

            // Assert
            Assert.NotNull(result);

            // Verify that the customer was added to the database
            var addedCustomer = context.Customers.FirstOrDefault(c => c.Id == customer.Id);
            Assert.NotNull(addedCustomer);
            Assert.Equal(customer.Name, addedCustomer.Name);
            Assert.Equal(customer.Address, addedCustomer.Address);
            Assert.Equal(customer.Accounts.Count, addedCustomer.Accounts.Count); // Ensure no accounts are associated yet
        }

        [Theory]
        [InlineData(1000)]
        [InlineData(5000)]
        [InlineData(10000)]
        [InlineData(99999999999)]
        [InlineData(99999999999.999999999)]
        public void Create_Customer_And_Deposit_UnitTest(decimal depositAmount = 500m)
        {
            using var context = CreateContext();
            var services = CreateService(context);

            #region Customer

            // Act: Customer
            var customer = CreateSampleCustomer();

            var customerResult = CoreBankingAPI.CreateCustomer(services, customer);

            // Assert: Customer
            Assert.NotNull(customerResult);

            #endregion

            #region Account

            // Act: Account
            var account = CreateSampleAccount(customer.Id);

            var accountResult = CoreBankingAPI.CreateAccount(services, account);

            // Assert: Account
            Assert.NotNull(accountResult);
            #endregion

            #region Deposit
            // Act: Deposit
            var depositRequest = new DepositionRequest
            {
                Amount = depositAmount
            };

            var depositResult = CoreBankingAPI.Deposit(services, account.Id, depositRequest);

            // Assert: Deposit
            Assert.NotNull(depositResult);

            #endregion

            #region Verify Deposition
            // Act
            var addedAccount = context.Accounts.FirstOrDefault(a => a.Id == account.Id);

            // Assert
            Assert.NotNull(addedAccount);

            Assert.Equal(account.Id, addedAccount.Id);
            Assert.Equal(account.Number, addedAccount.Number);
            Assert.Equal(account.CustomerId, addedAccount.CustomerId);
            Assert.Equal(depositRequest.Amount, addedAccount.Balance);

            // Act
            var transaction = context.Transactions.FirstOrDefault(t => t.AccountId == account.Id && t.Amount == depositRequest.Amount && t.Type == TransactionTypes.Deposit);

            // Verify Transaction
            Assert.NotNull(transaction);
            Assert.Equal(depositRequest.Amount, transaction.Amount);
            #endregion
        }

        public void Dispose()
        {
            _sqliteConnection.Close();
        }
    }
}
