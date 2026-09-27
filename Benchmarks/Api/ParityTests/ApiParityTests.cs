using Brigade.Net.Benchmarks.Api.Common;
using Brigade.Net.Benchmarks.Api.Fixture;
using Dapper;
using System.Net;
using System.Net.Http.Json;

namespace Brigade.Net.Benchmarks.Api.ParityTests;

[Collection("API parity")]
public sealed class ApiParityTests(ApiFixture fixture)
{
    [Theory]
    [InlineData("sqlserver")]
    [InlineData("postgresql")]
    [InlineData("mysql")]
    [InlineData("mariadb")]
    [InlineData("sqlite")]
    public async Task Both_apis_agree_on_validation_creation_and_search(string database)
    {
        var connectionString = await fixture.GetConnectionStringAsync(database);
        var baseline = new Dictionary<string, List<ItemResult>>();
        string? invalidBody = null;
        string? domainBody = null;
        foreach (var stack in new[] { "brigade", "fluent-ef-mediatr" })
        {
            await ApiData.ResetAsync(database, connectionString);
            using var client = fixture.CreateClient(stack);
            var path = $"/api/{database}/items";

            using var invalid = await client.PostAsJsonAsync(path, new
            {
                title = "x",
                categoryId = 0,
                score = -1
            });
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            var invalidJson = await invalid.Content.ReadAsStringAsync();
            if (stack == "brigade")
            {
                invalidBody = invalidJson;
            }
            else
            {
                Assert.Equal(invalidBody, invalidJson);
            }

            Assert.Equal(1000, await CountAsync(database, connectionString));

            using var missingCategory = await client.PostAsJsonAsync(path, new
            {
                title = "Good item",
                categoryId = 10,
                score = 70
            });
            Assert.Equal((HttpStatusCode)422, missingCategory.StatusCode);
            var domainJson = await missingCategory.Content.ReadAsStringAsync();
            if (stack == "brigade")
            {
                domainBody = domainJson;
            }
            else
            {
                Assert.Equal(domainBody, domainJson);
            }

            Assert.Equal(1000, await CountAsync(database, connectionString));

            using var created = await client.PostAsJsonAsync(path, new
            {
                title = "Good item",
                categoryId = 1,
                score = 70
            });
            Assert.Equal(HttpStatusCode.OK, created.StatusCode);
            var result = await created.Content.ReadFromJsonAsync<CreateResult>();
            Assert.Equal(1001, result!.Id);
            Assert.Equal(1001, await CountAsync(database, connectionString));

            foreach (var query in new[] { "categoryId=1&minScore=0", "categoryId=4&minScore=75" })
            {
                var rows = await client.GetFromJsonAsync<List<ItemResult>>($"{path}?{query}");
                Assert.NotNull(rows);
                Assert.NotEmpty(rows);
                if (stack == "brigade")
                {
                    baseline.Add(query, rows);
                }
                else
                {
                    Assert.Equal(baseline[query], rows);
                }
            }
        }
    }

    private static async Task<int> CountAsync(string database, string connectionString)
    {
        await using var connection = ApiDatabasePlatform.CreateConnection(database, connectionString);
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM benchmark_items");
    }
}
