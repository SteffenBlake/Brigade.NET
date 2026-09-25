using System.Text.Json;

namespace Brigade.Net.Example.IntegrationTests;

[Collection("AppHost")]
public sealed class MisePurchaseQueryTests(AppHostFixture host)
{
    [Theory]
    [InlineData("sqlserver")]
    [InlineData("postgresql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    [InlineData("mariadb")]
    public async Task ComplexPurchaseQueryReturnsSamePage(string engine)
    {
        using var response = await host.WebClient.GetAsync($"/api/v1/mise/{engine}/purchases");
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{engine}: {(int)response.StatusCode}: {body}");
        using var document = JsonDocument.Parse(body);
        var rows = document.RootElement;
        Assert.Equal(JsonValueKind.Array, rows.ValueKind);
        Assert.Equal(
            [11, 13, 14],
            rows.EnumerateArray().Select(row => row.GetProperty("id").GetInt32()).ToArray()
        );
        Assert.Equal("O'Reilly's deal", rows[2].GetProperty("label").GetString());
    }

    [Theory]
    [InlineData("sqlserver")]
    [InlineData("postgresql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    [InlineData("mariadb")]
    public async Task OuterJoinsKeepAccountWithoutPurchasesAndNullableColumns(string engine)
    {
        using var response = await host.WebClient.GetAsync($"/api/v1/mise/{engine}/accounts");
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{engine}: {(int)response.StatusCode}: {body}");
        using var document = JsonDocument.Parse(body);
        var rows = document.RootElement;
        Assert.Equal(
            [1, 2, 3, 4, 5],
            rows.EnumerateArray().Select(row => row.GetProperty("id").GetInt32()).ToArray()
        );
        Assert.Equal("O'Reilly", rows[0].GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Null, rows[0].GetProperty("note").ValueKind);
        Assert.Equal("idle", rows[4].GetProperty("group").GetString());
        Assert.Equal(JsonValueKind.Null, rows[4].GetProperty("parentId").ValueKind);
    }

    [Fact]
    public async Task AccountUpdateCommitsOnlyToItsSelectedDatabase()
    {
        await using var snapshot = await SqliteDatabaseSnapshot.CaptureAsync(
            host.SqliteConnectionString
        );
        var note = "changed-'" + Guid.NewGuid().ToString("N");
        await UpdateSqliteAsync(note);
        foreach (var engine in new[] { "sqlserver", "postgresql", "sqlite", "mysql", "mariadb" })
        {
            using var response = await host.WebClient.GetAsync($"/api/v1/mise/{engine}/accounts");
            response.EnsureSuccessStatusCode();
            using var rows = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var account = rows.RootElement
                .EnumerateArray()
                .Single(row => row.GetProperty("id").GetInt32() == 5);
            Assert.Equal(engine == "sqlite" ? note : null, account.GetProperty("note").GetString());
        }
    }

    private async Task UpdateSqliteAsync(string? note)
    {
        var url = "/api/v1/mise/sqlite/accounts/update?id=5";
        if (note is not null)
        {
            url += "&note=" + Uri.EscapeDataString(note);
        }

        using var response = await host.WebClient.PostAsync(url, content: null);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.IsSuccessStatusCode,
            $"SQLite update: {(int)response.StatusCode}: {body}"
        );
        using var result = JsonDocument.Parse(body);
        Assert.Equal(1, result.RootElement.GetInt32());
    }
}
