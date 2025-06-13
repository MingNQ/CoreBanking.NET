using CoreBanking.API.Apis;
using CoreBanking.Infrastructure.Entity;
using System.Net.Http.Json;

namespace CoreBanking.IntegrationTests.Tests;

public class IntegrationTest1
{
    // Instructions:
    // 1. Add a project reference to the target AppHost project, e.g.:
    //
    //    <ItemGroup>
    //        <ProjectReference Include="../MyAspireApp.AppHost/MyAspireApp.AppHost.csproj" />
    //    </ItemGroup>
    //
    // 2. Uncomment the following example test and update 'Projects.MyAspireApp_AppHost' to match your AppHost project:
    
    [Fact]
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CoreBanking_AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
        // To output logs to the xUnit.net ITestOutputHelper, consider adding a package from https://www.nuget.org/packages?q=xunit+logging

        await using var app = await appHost.BuildAsync();
        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        await app.StartAsync();

        // Act
        var httpClient = app.CreateHttpClient("corebanking-api");
        await resourceNotificationService.WaitForResourceAsync("corebanking-api", KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(30));

        // Start testing the API

        // Arrange
        var customer1 = new Customer()
        {
            Id = Guid.NewGuid(),
            Name = "John Doe",
            Address = "123 Main St",
            Accounts = []
        }; 
        
        var customer2 = new Customer()
        {
            Id = Guid.NewGuid(),
            Name = "Jane Smith",
            Address = "456 Elm St",
            Accounts = []
        };

        // Act
        var response = await httpClient.PostAsJsonAsync("/api/v1/corebanking/customers", customer1);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Act
        var response2 = await httpClient.PostAsJsonAsync("/api/v1/corebanking/customers", customer2);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Arrange
        var account1 = new Account()
        {
            Id = Guid.NewGuid(),
            CustomerId = customer1.Id,
            Balance = 1000m,
        };

        var account2 = new Account()
        {
            Id = Guid.NewGuid(),
            CustomerId = customer2.Id,
            Balance = 2000m,
        };

        // Act
        response = await httpClient.PostAsJsonAsync("/api/v1/corebanking/accounts", account1);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Act
        response2 = await httpClient.PostAsJsonAsync("/api/v1/corebanking/accounts", account2);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Act
        var getResponse1 = await httpClient.GetAsync($"/api/v1/corebanking/customers/{customer1.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse1.StatusCode);

        // Act
        var getCustomer1 = await getResponse1.Content.ReadFromJsonAsync<Customer>();

        // Assert
        Assert.NotNull(getCustomer1);
        Assert.Equal(customer1.Id, getCustomer1.Id);
        Assert.Equal(customer1.Name, getCustomer1.Name);
        Assert.Equal(customer1.Address, getCustomer1.Address);

        // Act
        var getResponse2 = await httpClient.GetAsync($"/api/v1/corebanking/customers/{customer2.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse2.StatusCode);

        // Act
        var getCustomer2 = await getResponse2.Content.ReadFromJsonAsync<Customer>();

        // Assert
        Assert.NotNull(getCustomer2);
        Assert.Equal(customer2.Id, getCustomer2.Id);
        Assert.Equal(customer2.Name, getCustomer2.Name);
        Assert.Equal(customer2.Address, getCustomer2.Address);

        // Act 
        response = await httpClient.PutAsJsonAsync($"/api/v1/corebanking/accounts/{account1.Id}/deposit", new DepositionRequest() 
        { 
            Amount = 50000m 
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Act
        response2 = await httpClient.PutAsJsonAsync($"/api/v1/corebanking/accounts/{account2.Id}/withdraw", new WithdrawalRequest() 
        { 
            Amount = 50000m 
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);

        // Act 
        response = await httpClient.PutAsJsonAsync($"/api/v1/corebanking/accounts/{account2.Id}/withdraw", new WithdrawalRequest() 
        { 
            Amount = 5000m 
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Act
        response2 = await httpClient.PutAsJsonAsync($"/api/v1/corebanking/accounts/{account2.Id}/withdraw", new WithdrawalRequest() 
        { 
            Amount = 1999m 
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        // Act
        getResponse1 = await httpClient.GetAsync($"/api/v1/corebanking/accounts/{account1.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse1.StatusCode);

        getResponse2 = await httpClient.GetAsync($"/api/v1/corebanking/accounts/{account2.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse2.StatusCode);
        
        // Act
        var getAccount1 = await getResponse1.Content.ReadFromJsonAsync<Account>();

        // Assert
        Assert.NotNull(getAccount1);
        Assert.Equal(account1.Id, getAccount1.Id);
        Assert.Equal(account1.CustomerId, getAccount1.CustomerId);

        // Act
        var getAccount2 = await getResponse2.Content.ReadFromJsonAsync<Account>();

        // Assert
        Assert.NotNull(getAccount2);
        Assert.Equal(account2.Id, getAccount2.Id);
        Assert.Equal(account2.CustomerId, getAccount2.CustomerId);

        // Act
        var transferResponse = await httpClient.PutAsJsonAsync($"/api/v1/corebanking/accounts/{account1.Id}/transfer", new TransferRequest()
        {
            Amount = 100000m,
            ToAccountNumber = getAccount2.Number
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, transferResponse.StatusCode);

        // Act
        transferResponse = await httpClient.PutAsJsonAsync($"/api/v1/corebanking/accounts/{account1.Id}/transfer", new TransferRequest()
        {
            Amount = 51000m,
            ToAccountNumber = getAccount2.Number
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, transferResponse.StatusCode);

        // Act
        getResponse1 = await httpClient.GetAsync($"/api/v1/corebanking/accounts/{account1.Id}");
        getAccount1 = await getResponse1.Content.ReadFromJsonAsync<Account>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse1.StatusCode);
        Assert.NotNull(getAccount1);
        Assert.Equal(0, getAccount1.Balance);

        // Act
        getResponse2 = await httpClient.GetAsync($"/api/v1/corebanking/accounts/{account2.Id}");
        getAccount2 = await getResponse2.Content.ReadFromJsonAsync<Account>();
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse2.StatusCode);
        Assert.NotNull(getAccount2);
        Assert.Equal(51001m, getAccount2.Balance);
    }
}
