using System.Data;
using System.Runtime.CompilerServices;
using Brigade.Net.Mise;
using Brigade.Net.Mise.MariaDb;
using Brigade.Net.Mise.MySQL;
using Brigade.Net.Mise.PostgreSQL;
using Brigade.Net.Mise.SQLite;
using Brigade.Net.Mise.SqlServer;

namespace Brigade.Net.Mise.Tests;

public sealed class QueryApiDialectTests
{
    [Theory]
    [InlineData("SqlServer", "WITH", " OFFSET 2 ROWS FETCH NEXT 3 ROWS ONLY", "[a]]b]")]
    [InlineData("PostgreSQL", "WITH RECURSIVE", " LIMIT 3 OFFSET 2", "\"a\"\"b\"")]
    [InlineData("SQLite", "WITH RECURSIVE", " LIMIT 3 OFFSET 2", "\"a\"\"b\"")]
    [InlineData("MySQL", "WITH RECURSIVE", " LIMIT 3 OFFSET 2", "`a``b`")]
    [InlineData("MariaDB", "WITH RECURSIVE", " LIMIT 3 OFFSET 2", "`a``b`")]
    public void DialectRendersRecursiveCtePagingAndQuotes(
        string engine,
        string withKeyword,
        string paging,
        string quoted
    )
    {
        var dialect = GetDialect(engine);
        var anchor = new QueryBuilder(dialect).Select($"{1}");
        var recursive = new QueryBuilder(dialect).Select($"{2}");
        var query = new QueryBuilder(dialect)
            .WithRecursive("tree", anchor, recursive)
            .Select($"{3}")
            .From($"tree")
            .OrderBy($"id")
            .Limit(3)
            .Offset(2);

        var compiled = query.Compile();

        Assert.StartsWith(withKeyword + " ", compiled.Text);
        Assert.EndsWith(paging, compiled.Text);
        Assert.Equal([1, 2, 3], compiled.Parameters.Select(parameter => parameter.Value));
        Assert.Contains("@p0", compiled.Text);
        Assert.Contains("@p1", compiled.Text);
        Assert.Contains("@p2", compiled.Text);
        Assert.Equal(quoted, dialect.QuoteIdentifier(engine == "SqlServer" ? "a]b" : engine is "MySQL" or "MariaDB" ? "a`b" : "a\"b"));
    }

    [Theory]
    [InlineData("PostgreSQL", " OFFSET 4")]
    [InlineData("SQLite", " LIMIT -1 OFFSET 4")]
    [InlineData("MySQL", " LIMIT 18446744073709551615 OFFSET 4")]
    [InlineData("MariaDB", " LIMIT 18446744073709551615 OFFSET 4")]
    [InlineData("SqlServer", " OFFSET 4 ROWS")]
    public void OffsetWithoutLimitUsesDialectSyntax(string engine, string suffix)
    {
        var query = new QueryBuilder(GetDialect(engine)).Select($"1").OrderBy($"1").Offset(4);

        Assert.EndsWith(suffix, query.Compile().Text);
    }

    [Fact]
    public void PagingRequiresEngineDialectAndSqlServerOrder()
    {
        Assert.Throws<NotSupportedException>(() => new QueryBuilder().Select($"1").Limit(2).Compile());
        Assert.Throws<InvalidOperationException>(() => new SqlServerQueryBuilder().Select($"1").Limit(2).Compile());
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqlServerQueryBuilder().Select($"1").OrderBy($"1").Limit(0).Compile());
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqliteQueryBuilder().Limit(1).Limit(2));
    }

    [Theory]
    [InlineData("SqlServer", true)]
    [InlineData("PostgreSQL", true)]
    [InlineData("SQLite", true)]
    [InlineData("MySQL", false)]
    [InlineData("MariaDB", false)]
    public void FullJoinUsesCapability(string engine, bool supported)
    {
        const string relationship = "purchases ON purchases.user_id = users.id";
        var query = new QueryBuilder(GetDialect(engine)).Select($"users.id").From($"users").FullJoin($"{relationship:raw}");

        if (supported)
        {
            Assert.Contains(" FULL JOIN purchases ON purchases.user_id = users.id", query.Compile().Text);
        }
        else
        {
            Assert.Throws<NotSupportedException>(() => query.Compile());
        }
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSQL")]
    [InlineData("SQLite")]
    [InlineData("MySQL")]
    [InlineData("MariaDB")]
    public void OneRelationshipConstantWorksForEverySupportedJoin(string engine)
    {
        const string relationship = "purchases ON purchases.user_id = users.id";
        var dialect = GetDialect(engine);
        var kinds = new[] { "INNER JOIN", "LEFT JOIN", "RIGHT JOIN", "FULL JOIN" };
        foreach (var kind in kinds)
        {
            if (kind == "FULL JOIN" && !dialect.SupportsFullJoin)
            {
                continue;
            }
            var query = new QueryBuilder(dialect).Select($"users.id").From($"users");
            switch (kind)
            {
                case "INNER JOIN": query.InnerJoin($"{relationship:raw}"); break;
                case "LEFT JOIN": query.LeftJoin($"{relationship:raw}"); break;
                case "RIGHT JOIN": query.RightJoin($"{relationship:raw}"); break;
                case "FULL JOIN": query.FullJoin($"{relationship:raw}"); break;
            }
            Assert.Equal($"SELECT users.id FROM users {kind} {relationship}", query.Compile().Text);
        }
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSQL")]
    [InlineData("SQLite")]
    [InlineData("MySQL")]
    [InlineData("MariaDB")]
    public void JoinRuntimeValueUsesParameterInSqlOrder(string engine)
    {
        var kind = "x' OR 1=1 --";
        FormattableString join = $"purchases ON purchases.user_id = users.id AND purchases.kind = {kind}";
        var query = new QueryBuilder(GetDialect(engine))
            .Where($"users.id = {9}")
            .InnerJoin(join)
            .From($"users")
            .Select($"{7} AS marker");

        var sql = query.Compile();

        Assert.Equal(
            "SELECT @p0 AS marker FROM users INNER JOIN purchases ON purchases.user_id = users.id AND purchases.kind = @p1 WHERE users.id = @p2",
            sql.Text
        );
        Assert.Equal([7, kind, 9], sql.Parameters.Select(parameter => parameter.Value).ToArray());
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSQL")]
    [InlineData("SQLite")]
    [InlineData("MySQL")]
    [InlineData("MariaDB")]
    public void CommonWritesCompileInSqlOrderForEveryEngine(string engine)
    {
        var dialect = GetDialect(engine);
        var update = new CommandBuilder(dialect).Where($"id = {9}")
            .Set($"name = {"new"}").Update($"people").Compile();
        var delete = new CommandBuilder(dialect).Where($"id = {9}")
            .DeleteFrom($"people").Compile();
        var insert = new CommandBuilder(dialect).Values($"{1}, {"one"}")
            .Columns($"id, name").InsertInto($"people").Compile();

        Assert.Equal("UPDATE people SET name = @p0 WHERE id = @p1", update.Text);
        Assert.Equal(["new", 9], update.Parameters.Select(parameter => parameter.Value));
        Assert.Equal("DELETE FROM people WHERE id = @p0", delete.Text);
        Assert.Equal("INSERT INTO people (id, name) VALUES (@p0, @p1)", insert.Text);
        Assert.Equal(CommandType.Text, insert.CommandType);
        Assert.Equal(CommandType.StoredProcedure, new CommandBuilder(dialect).Procedure("save_person").Compile().CommandType);
        var insertSelect = new CommandBuilder(dialect).InsertInto($"people")
            .FromQuery(new QueryBuilder(dialect).Select($"{5}")).Compile();
        Assert.Equal("INSERT INTO people SELECT @p0", insertSelect.Text);
        Assert.Equal(5, insertSelect.Parameters[0].Value);
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSQL")]
    [InlineData("SQLite")]
    [InlineData("MySQL")]
    [InlineData("MariaDB")]
    public void CommonReadClausesCompileExactlyForEveryEngine(string engine)
    {
        var dialect = GetDialect(engine);
        var grouped = new QueryBuilder(dialect).Having($"COUNT(*) > {2}")
            .OrderBy($"category").GroupBy($"category").Where($"enabled = {true}")
            .Distinct().From($"items").Select($"category").Compile();
        var listed = new QueryBuilder(dialect).Select($"id").From($"items")
            .WhereIn($"id", new[] { 3, 4 }).Compile();
        var custom = new QueryBuilder(dialect).Sql($"SELECT {5}").Compile();
        var write = new CommandBuilder(dialect).Sql($"DELETE FROM items WHERE id = {6}").Compile();

        Assert.Equal("SELECT DISTINCT category FROM items WHERE enabled = @p0 GROUP BY category HAVING COUNT(*) > @p1 ORDER BY category", grouped.Text);
        Assert.Equal(["@p0", "@p1"], grouped.Parameters.Select(parameter => parameter.Name));
        Assert.Equal([true, 2], grouped.Parameters.Select(parameter => parameter.Value));
        Assert.Equal("SELECT id FROM items WHERE id IN (@p0, @p1)", listed.Text);
        Assert.Equal([3, 4], listed.Parameters.Select(parameter => parameter.Value));
        Assert.Equal("SELECT @p0", custom.Text);
        Assert.Equal("DELETE FROM items WHERE id = @p0", write.Text);
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSQL")]
    [InlineData("SQLite")]
    [InlineData("MySQL")]
    [InlineData("MariaDB")]
    public void NestedReadTreeUsesOneParameterScopeForEveryEngine(string engine)
    {
        var dialect = GetDialect(engine);
        var cte = new QueryBuilder(dialect).Select($"{1} AS id");
        var derived = new QueryBuilder(dialect).Select($"{2} AS id");
        var correlated = new QueryBuilder(dialect).Select($"1").From($"seed")
            .Where($"seed.id = s.id AND seed.id > {3}");
        var set = new QueryBuilder(dialect).Select($"{4}");
        var query = new QueryBuilder(dialect).With("seed", cte).Select($"s.id")
            .From(derived, "s").WhereExists(correlated).Union(set).OrderBy($"1");

        var compiled = query.Compile();

        Assert.Contains(" UNION SELECT @p3 ORDER BY 1", compiled.Text);
        Assert.Contains("EXISTS (SELECT 1 FROM seed WHERE seed.id = s.id AND seed.id > @p2)", compiled.Text);
        Assert.Equal(["@p0", "@p1", "@p2", "@p3"], compiled.Parameters.Select(parameter => parameter.Name));
        Assert.Equal([1, 2, 3, 4], compiled.Parameters.Select(parameter => parameter.Value));
    }

    [Fact]
    public void ReusedChildRenumbersAcrossParentPositions()
    {
        var child = new QueryBuilder().Select($"{11}");
        var query = new QueryBuilder().Select(child).Select(child).WhereExists(child);

        var compiled = query.Compile();

        Assert.Equal("SELECT (SELECT @p0), (SELECT @p1) WHERE EXISTS (SELECT @p2)", compiled.Text);
        Assert.Equal([11, 11, 11], compiled.Parameters.Select(parameter => parameter.Value));
    }

    [Fact]
    public void ReusedChildInTwoParentsKeepsIndependentParameterNames()
    {
        var child = new QueryBuilder().Select($"{11}");
        var first = new QueryBuilder().Select(child).Where($"id = {12}");
        var second = new QueryBuilder().Select($"{13}").WhereExists(child);

        Assert.Equal("SELECT (SELECT @p0) WHERE id = @p1", first.Compile().Text);
        Assert.Equal("SELECT @p0 WHERE EXISTS (SELECT @p1)", second.Compile().Text);
        Assert.Equal([13, 11], second.Compile().Parameters.Select(parameter => parameter.Value));
    }

    [Fact]
    public void InListHandlesEmptyAndNullWithoutInvalidSql()
    {
        var empty = new QueryBuilder().Select($"1").WhereIn($"id", Array.Empty<int>());
        var values = new QueryBuilder().Select($"1").WhereIn($"id", new string?[] { null, "x" });

        Assert.Equal("SELECT 1 WHERE 1 = 0", empty.Compile().Text);
        Assert.Equal("SELECT 1 WHERE id IN (@p0, @p1)", values.Compile().Text);
        Assert.Equal([null, "x"], values.Compile().Parameters.Select(parameter => parameter.Value));
    }

    [Fact]
    public void InsertValuesAndProcedureHaveCorrectCommandType()
    {
        const string target = "people";
        const string columns = "id, name";
        var insert = new CommandBuilder().InsertInto($"{target:raw}").Columns($"{columns:raw}")
            .Values($"{1}, {"a"}").Values($"{2}, {"b"}").Compile();
        var procedure = new CommandBuilder().Procedure("update_people").Timeout(17).Compile();

        Assert.Equal("INSERT INTO people (id, name) VALUES (@p0, @p1), (@p2, @p3)", insert.Text);
        Assert.Equal([1, "a", 2, "b"], insert.Parameters.Select(parameter => parameter.Value));
        Assert.Equal(CommandType.StoredProcedure, procedure.CommandType);
        Assert.Equal("update_people", procedure.Text);
        Assert.Equal(17, procedure.Timeout);
    }

    [Fact]
    public void MixedEngineChildIsRejectedBeforeACommandIsProduced()
    {
        var child = new QueryBuilder(new PostgreSqlDialect()).Select($"{1}");
        var command = new CommandBuilder(new SqlServerDialect()).InsertInto($"target").FromQuery(child);

        Assert.Throws<InvalidOperationException>(() => command.Compile());
    }

    [Fact]
    public void EscapedBracesAndAlignmentKeepArgumentsAsParameters()
    {
        var fragment = FormattableStringFactory.Create("'{{' || {0,8} || '}}' || {0}", "x");
        var compiled = new QueryBuilder().Select(fragment).Compile();

        Assert.Equal("SELECT '{' || @p0 || '}' || @p1", compiled.Text);
        Assert.Equal(["x", "x"], compiled.Parameters.Select(parameter => parameter.Value));
    }

    [Fact]
    public void UnknownFormatAndCyclesFailClearly()
    {
        var bad = new QueryBuilder().Select(FormattableStringFactory.Create("{0:D}", 1));
        var cycle = new QueryBuilder();
        cycle.Select(cycle);

        Assert.Throws<FormatException>(() => bad.Compile());
        Assert.Throws<InvalidOperationException>(() => cycle.Compile());
    }

    [Fact]
    public void ParameterTypeHintsAndProcedureParametersSurviveCompilation()
    {
        var typed = new SqlParameterSpec("ignored", null, DbType.Int32);
        var query = new QueryBuilder().Select($"{typed}").Compile();
        var procedure = new CommandBuilder().Procedure("save_person")
            .ProcedureParameter("@id", null, DbType.Int32).Compile();

        Assert.Equal("SELECT @p0", query.Text);
        Assert.Equal("@p0", query.Parameters[0].Name);
        Assert.Null(query.Parameters[0].Value);
        Assert.Equal(DbType.Int32, query.Parameters[0].DbType);
        Assert.Equal(CommandType.StoredProcedure, procedure.CommandType);
        Assert.Equal(new SqlParameterSpec("@id", null, DbType.Int32), procedure.Parameters[0]);
    }

    [Fact]
    public void ReaderBehaviorRejectsOwnershipChangingFlags()
    {
        var query = new QueryBuilder().Select($"1").Behavior(CommandBehavior.SequentialAccess);

        Assert.Equal(CommandBehavior.SequentialAccess, query.Compile().Behavior);
        Assert.Throws<ArgumentOutOfRangeException>(() => new CompiledSql("SELECT 1", behavior: CommandBehavior.CloseConnection));
        Assert.Throws<InvalidOperationException>(() => query.Behavior(CommandBehavior.SingleResult));
    }

    [Fact]
    public void ConflictingCommandClausesAreRejected()
    {
        var child = new QueryBuilder().Select($"1");
        var insert = new CommandBuilder().InsertInto($"target").FromQuery(child);
        var delete = new CommandBuilder().DeleteFrom($"target").Set($"x = {1}");
        var update = new CommandBuilder().Update($"target").Set($"x = {1}").Values($"{2}");

        Assert.Throws<InvalidOperationException>(() => insert.FromQuery(child));
        Assert.Throws<InvalidOperationException>(() => delete.Compile());
        Assert.Throws<InvalidOperationException>(() => update.Compile());
        Assert.Throws<InvalidOperationException>(() => new QueryBuilder().Select($"1").InnerJoin($"x ON 1 = 1").Compile());
    }

    [Fact]
    public void PermutedFluentCallsCompileTheSameSql()
    {
        const string table = "users";
        const string relationship = "purchases ON purchases.user_id = users.id";
        var first = new QueryBuilder().From($"{table:raw}").Where($"id > {1}")
            .Select($"{2} AS rank").InnerJoin($"{relationship:raw}").Select($"name")
            .GroupBy($"name").OrderBy($"name");
        var second = new QueryBuilder().Select($"{2} AS rank").Select($"name")
            .OrderBy($"name").GroupBy($"name").InnerJoin($"{relationship:raw}")
            .Where($"id > {1}").From($"{table:raw}");

        Assert.Equal(first.Compile().Text, second.Compile().Text);
        Assert.Equal(first.Compile().Parameters, second.Compile().Parameters);
    }

    [Fact]
    public void CrossJoinAndAliasRelationshipKeepExpectedSql()
    {
        const string relationship = "purchases ON purchases.user_id = u.id";
        const string other = "teams";
        var query = new QueryBuilder().Select($"u.id").From($"users AS u")
            .LeftJoin($"{relationship:raw}").CrossJoin($"{other:raw}");

        Assert.Equal("SELECT u.id FROM users AS u LEFT JOIN purchases ON purchases.user_id = u.id CROSS JOIN teams", query.Compile().Text);
        Assert.Throws<ArgumentException>(() => new QueryBuilder().Select($"1").From($"users").CrossJoin($"{relationship:raw}").Compile());
        Assert.Throws<ArgumentException>(() => new QueryBuilder().Select($"1").From($"users").InnerJoin($"{other:raw}").Compile());
        Assert.Throws<ArgumentException>(() => new QueryBuilder().Select($"1").From($"users").InnerJoin($"   ").Compile());
    }

    [Fact]
    public async Task IndependentCompilesCanRunConcurrently()
    {
        var child = new QueryBuilder().Select($"{5}");
        var parent = new QueryBuilder().Select(child).Select(child);
        var compiled = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(parent.Compile)));

        Assert.All(compiled, sql =>
        {
            Assert.Equal("SELECT (SELECT @p0), (SELECT @p1)", sql.Text);
            Assert.Equal([5, 5], sql.Parameters.Select(parameter => parameter.Value));
        });
        Assert.Equal(32, compiled.Select(sql => sql.Parameters).Distinct(ReferenceEqualityComparer.Instance).Count());
    }

    [Fact]
    public async Task IndependentCommandCompilesCanRunConcurrently()
    {
        var child = new QueryBuilder().Select($"{5}");
        var command = new CommandBuilder().InsertInto($"target").FromQuery(child);
        var compiled = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(command.Compile)));

        Assert.All(compiled, sql =>
        {
            Assert.Equal("INSERT INTO target SELECT @p0", sql.Text);
            Assert.Equal(5, sql.Parameters[0].Value);
        });
        Assert.Equal(32, compiled.Select(sql => sql.Parameters).Distinct(ReferenceEqualityComparer.Instance).Count());
    }

    [Fact]
    public void EngineReturnSyntaxUsesItsOwnBuilder()
    {
        const string table = "people";
        var sqlServer = new SqlServerCommandBuilder().OutputInserted("id")
            .InsertInto($"{table:raw}").Values($"{1}").Compile();
        var postgreSql = new PostgreSqlCommandBuilder().Returning("id")
            .InsertInto($"{table:raw}").Values($"{1}").Compile();
        var sqlite = new SqliteCommandBuilder().Returning("id")
            .InsertInto($"{table:raw}").Values($"{1}").Compile();
        var mariaDb = new MariaDbCommandBuilder().Returning("id")
            .InsertInto($"{table:raw}").Values($"{1}").Compile();

        Assert.Equal("INSERT INTO people OUTPUT INSERTED.[id] VALUES (@p0)", sqlServer.Text);
        Assert.Equal("INSERT INTO people VALUES (@p0) RETURNING \"id\"", postgreSql.Text);
        Assert.Equal("INSERT INTO people VALUES (@p0) RETURNING \"id\"", sqlite.Text);
        Assert.Equal("INSERT INTO people VALUES (@p0) RETURNING `id`", mariaDb.Text);
        Assert.Equal("SELECT LAST_INSERT_ID()", MySqlQueryBuilder.LastInsertId().Compile().Text);
    }

    [Theory]
    [InlineData("MySQL")]
    [InlineData("MariaDB")]
    public void LimitWithoutOffsetUsesOnlyLimit(string engine)
    {
        var query = new QueryBuilder(GetDialect(engine)).Select($"1").Limit(3);
        Assert.EndsWith(" LIMIT 3", query.Compile().Text);
    }

    [Fact]
    public void EngineReturningRejectsUnsupportedWriteKinds()
    {
        Assert.Throws<InvalidOperationException>(() => new MariaDbCommandBuilder()
            .Returning("id").Update($"people").Set($"name = {"updated"}").Compile());
        Assert.Throws<InvalidOperationException>(() => new SqlServerCommandBuilder()
            .OutputInserted("id").DeleteFrom($"people").Compile());
        Assert.Throws<InvalidOperationException>(() => new PostgreSqlCommandBuilder()
            .Returning("id").Sql($"TRUNCATE TABLE people").Compile());
    }

    [Fact]
    public void EngineReturningAcceptsItsSupportedWriteKindsAndRejectsDuplicates()
    {
        var mariaDb = new MariaDbCommandBuilder().Returning("id");
        Assert.EndsWith(" RETURNING `id`", mariaDb.DeleteFrom($"people").Compile().Text);
        Assert.Throws<InvalidOperationException>(() => mariaDb.Returning("name"));
        Assert.Throws<InvalidOperationException>(() => new MariaDbCommandBuilder().Returning());

        var postgreSql = new PostgreSqlCommandBuilder().Returning("id")
            .Update($"people").Set($"name = {"updated"}").Compile();
        Assert.EndsWith(" RETURNING \"id\"", postgreSql.Text);
        Assert.EndsWith(" RETURNING \"id\"", new PostgreSqlCommandBuilder()
            .Returning("id").DeleteFrom($"people").Compile().Text);
        Assert.DoesNotContain("RETURNING", new PostgreSqlCommandBuilder()
            .DeleteFrom($"people").Compile().Text);

        var sqlServer = new SqlServerCommandBuilder().OutputInserted("id")
            .Update($"people").Set($"name = {"updated"}").Compile();
        Assert.Contains("OUTPUT INSERTED.[id]", sqlServer.Text);
    }

    [Fact]
    public void SqlServerQueryWithoutRecursionHintHasNoTrailingOption()
    {
        Assert.Equal("SELECT 1", new SqlServerQueryBuilder().Select($"1").Compile().Text);
    }

    [Fact]
    public void UpdateSubqueryUsesSharedParameterScope()
    {
        var child = new QueryBuilder().Select($"{4}").Where($"id = {5}");
        var command = new CommandBuilder().Where($"id = {6}").Set("score", child).Update($"players");

        var compiled = command.Compile();

        Assert.Equal("UPDATE players SET \"score\" = (SELECT @p0 WHERE id = @p1) WHERE id = @p2", compiled.Text);
        Assert.Equal([4, 5, 6], compiled.Parameters.Select(parameter => parameter.Value));
    }

    [Fact]
    public void TrustedCustomSqlStillParameterizesValues()
    {
        var malicious = "x'; DELETE FROM users; --";
        var read = new QueryBuilder().Sql($"SELECT {malicious}").Compile();
        var write = new CommandBuilder().Sql($"UPDATE users SET name = {malicious}").Compile();

        Assert.Equal("SELECT @p0", read.Text);
        Assert.Equal("UPDATE users SET name = @p0", write.Text);
        Assert.Equal(malicious, read.Parameters[0].Value);
        Assert.Equal(malicious, write.Parameters[0].Value);
    }

    [Fact]
    public void SqlServerMaxRecursionStaysOnEngineBuilder()
    {
        var query = new SqlServerQueryBuilder().MaxRecursion(42).Select($"1");

        Assert.Equal("SELECT 1 OPTION (MAXRECURSION 42)", query.Compile().Text);
    }

    private static SqlDialect GetDialect(string engine) => engine switch
    {
        "SqlServer" => new SqlServerDialect(),
        "PostgreSQL" => new PostgreSqlDialect(),
        "SQLite" => new SqliteDialect(),
        "MySQL" => new MySqlDialect(),
        "MariaDB" => new MariaDbDialect(),
        _ => throw new ArgumentOutOfRangeException(nameof(engine))
    };
}
