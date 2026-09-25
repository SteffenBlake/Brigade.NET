using System.Runtime.CompilerServices;
using Brigade.Net.Mise;
using Brigade.Net.Mise.MariaDb;
using Brigade.Net.Mise.MySQL;
using Brigade.Net.Mise.PostgreSQL;
using Brigade.Net.Mise.SQLite;
using Brigade.Net.Mise.SqlServer;

namespace Brigade.Net.Mise.Tests;

public sealed class QueryApiEdgeTests
{
    [Fact]
    public void DistinctGroupingHavingAndSetKindsRenderInSqlOrder()
    {
        var child = new QueryBuilder().Select($"{7}");
        var query = new QueryBuilder().Select($"category").Distinct().From($"items")
            .GroupBy($"category").Having($"COUNT(*) > {2}")
            .UnionAll(child).Intersect(child).Except(child).OrderBy($"category");

        var compiled = query.Compile();

        const string expected =
            "SELECT DISTINCT category FROM items GROUP BY category HAVING COUNT(*) > @p0 "
            + "UNION ALL SELECT @p1 INTERSECT SELECT @p2 EXCEPT SELECT @p3 ORDER BY category";
        Assert.Equal(expected, compiled.Text);
        Assert.Equal([2, 7, 7, 7], compiled.Parameters.Select(parameter => parameter.Value));
    }

    [Fact]
    public void CrossJoinChildAndInSubqueryShareParameterScope()
    {
        var child = new QueryBuilder().Select($"{1} AS id");
        var predicate = new QueryBuilder().Select($"{2}");
        var query = new QueryBuilder().Select($"d.id").From($"items")
            .CrossJoin(child, "d").WhereIn($"d.id", predicate);

        var compiled = query.Compile();

        Assert.Equal(
            "SELECT d.id FROM items CROSS JOIN (SELECT @p0 AS id) AS \"d\" "
            + "WHERE d.id IN (SELECT @p1)",
            compiled.Text
        );
        Assert.Equal([1, 2], compiled.Parameters.Select(parameter => parameter.Value));
    }

    [Fact]
    public void TwoCtesRenderInCallOrder()
    {
        var child = new QueryBuilder().Select($"{1}");
        var query = new QueryBuilder().With("a", child).With("b", child).Select($"1");

        Assert.Equal(
            "WITH \"a\" AS (SELECT @p0), \"b\" AS (SELECT @p1) SELECT 1",
            query.Compile().Text
        );
        Assert.Throws<InvalidOperationException>(() => query.With("a", child));
    }

    [Fact]
    public void InvalidQueryShapesFailBeforeReturningSql()
    {
        Assert.Throws<InvalidOperationException>(() => new QueryBuilder().Compile());
        Assert.Throws<InvalidOperationException>(
            () => new QueryBuilder().Sql($"SELECT 1").Sql($"SELECT 2")
        );
        Assert.Throws<InvalidOperationException>(
            () => new QueryBuilder().Sql($"SELECT 1").Distinct().Compile()
        );
        Assert.Throws<InvalidOperationException>(
            () => new QueryBuilder().Select($"1").From($"a").From($"b")
        );
        Assert.Throws<ArgumentOutOfRangeException>(() => new QueryBuilder().Offset(-1));
        Assert.Throws<ArgumentNullException>(() => new QueryBuilder().Union(null!));
        Assert.Throws<ArgumentNullException>(() => new QueryBuilder(null!));
        Assert.Throws<ArgumentNullException>(() => new CommandBuilder(null!));
        Assert.Throws<ArgumentNullException>(() => new CommandBuilder().FromQuery(null!));
    }

    [Fact]
    public void CustomReadSqlRejectsEveryFluentClauseKind()
    {
        var child = new QueryBuilder().Select($"1");
        Action<QueryBuilder>[] changes =
        [
            query => query.With("x", child),
            query => query.Select($"1"),
            query => query.From($"items"),
            query => query.InnerJoin($"other ON 1 = 1"),
            query => query.Where($"id = 1"),
            query => query.GroupBy($"id"),
            query => query.Having($"COUNT(*) > 1"),
            query => query.OrderBy($"id"),
            query => query.Union(child),
            query => query.Limit(1),
            query => query.Offset(1),
            query => query.Distinct()
        ];

        foreach (var change in changes)
        {
            var query = new QueryBuilder().Sql($"SELECT 1");
            change(query);
            Assert.Throws<InvalidOperationException>(() => query.Compile());
        }
    }

    [Fact]
    public void CustomDialectCanRejectRightJoin()
    {
        var query = new QueryBuilder(new NoRightDialect()).Select($"1").From($"one")
            .RightJoin($"two ON two.id = one.id");

        Assert.Throws<NotSupportedException>(() => query.Compile());
    }

    [Fact]
    public void ForeignBuilderChildrenFailBeforeReturningSql()
    {
        var foreign = new TestQueryBuilder(new CompiledSql("SELECT 1"));

        Assert.Throws<ArgumentException>(() => new QueryBuilder().Select(foreign).Compile());
        Assert.Throws<ArgumentException>(
            () => new CommandBuilder().InsertInto($"target").FromQuery(foreign).Compile()
        );
    }

    [Theory]
    [InlineData("}")]
    [InlineData("{x}")]
    [InlineData("{1}")]
    [InlineData("{0,}")]
    [InlineData("{0:D}")]
    [InlineData("{0")]
    public void InvalidCompositeFormatsAreRejected(string format)
    {
        var fragment = FormattableStringFactory.Create(format, 1);

        Assert.Throws<FormatException>(() => new QueryBuilder().Select(fragment).Compile());
    }

    [Fact]
    public void RawFormatRequiresAStringEvenThroughFactory()
    {
        var fragment = FormattableStringFactory.Create("{0:raw}", 1);

        Assert.Throws<ArgumentException>(() => new QueryBuilder().Select(fragment).Compile());
    }

    [Fact]
    public void CompositeAlignmentAllowsWhitespaceAndSigns()
    {
        var aligned = FormattableStringFactory.Create("{0  , -8 } + {0,+8:raw}", "id");

        var compiled = new QueryBuilder().Select(aligned).Compile();
        Assert.Equal("SELECT @p0 + id", compiled.Text);
    }

    [Fact]
    public void CommandShapeAndDuplicateSlotsAreChecked()
    {
        Assert.Throws<InvalidOperationException>(() => new CommandBuilder().Compile());
        Assert.Throws<InvalidOperationException>(
            () => new CommandBuilder().Update($"a").Update($"b")
        );
        Assert.Throws<InvalidOperationException>(
            () => new CommandBuilder().Columns($"id").Columns($"name")
        );
        Assert.Throws<InvalidOperationException>(
            () => new CommandBuilder().Sql($"DELETE FROM x").Procedure("p")
        );
        Assert.Throws<InvalidOperationException>(
            () => new CommandBuilder().Procedure("p").Sql($"DELETE FROM x")
        );
        Assert.Throws<InvalidOperationException>(
            () => new CommandBuilder().Procedure("p")
                .ProcedureParameter("@id", 1)
                .ProcedureParameter("@id", 2)
        );
        Assert.Throws<ArgumentOutOfRangeException>(() => new CommandBuilder().Timeout(-1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CommandBuilder().Timeout(1).Timeout(2)
        );
    }

    [Fact]
    public void CommandClausesDoNotDisappearWhenKindDiffers()
    {
        Assert.Throws<InvalidOperationException>(
            () => new CommandBuilder().Where($"id = 1").Procedure("p").Compile()
        );
        Assert.Throws<InvalidOperationException>(
            () => new CommandBuilder().ProcedureParameter("@id", 1).Sql($"SELECT 1").Compile()
        );
        Assert.Throws<InvalidOperationException>(
            () => new CommandBuilder().InsertInto($"t").Compile()
        );
        Assert.Throws<InvalidOperationException>(() => new CommandBuilder().Update($"t").Compile());
        Assert.Throws<InvalidOperationException>(
            () => new CommandBuilder().InsertInto($"t").Values($"{1}")
                .FromQuery(new QueryBuilder().Select($"2")).Compile()
        );
    }

    [Fact]
    public void RepeatedUpdateTermsKeepTheirOrder()
    {
        var command = new CommandBuilder().Where($"id > {3}").Set($"a = {1}")
            .Where($"id < {4}").Set($"b = {2}").Update($"items");

        var compiled = command.Compile();

        Assert.Equal(
            "UPDATE items SET a = @p0, b = @p1 WHERE id > @p2 AND id < @p3",
            compiled.Text
        );
        Assert.Equal([1, 2, 3, 4], compiled.Parameters.Select(parameter => parameter.Value));
    }

    [Fact]
    public void EngineReturnOptionsRejectUnsupportedCommandKinds()
    {
        Assert.Throws<InvalidOperationException>(
            () => new MariaDbCommandBuilder().Returning("id")
                .Update($"items").Set($"id = {1}").Compile()
        );
        Assert.Throws<InvalidOperationException>(
            () => new PostgreSqlCommandBuilder().Returning("id").Procedure("p").Compile()
        );
        Assert.Throws<InvalidOperationException>(() => new SqliteCommandBuilder().Returning("id")
            .Procedure("p").Compile());
        Assert.Throws<InvalidOperationException>(
            () => new SqlServerCommandBuilder().OutputInserted("id")
                .DeleteFrom($"items").Compile()
        );
    }

    [Fact]
    public void ReturnOptionsRejectDuplicatesAndEmptyColumns()
    {
        Assert.Throws<InvalidOperationException>(() => new PostgreSqlCommandBuilder().Returning());
        Assert.Throws<InvalidOperationException>(() => new SqliteCommandBuilder().Returning());
        Assert.Throws<InvalidOperationException>(() => new MariaDbCommandBuilder().Returning());
        Assert.Throws<InvalidOperationException>(
            () => new SqlServerCommandBuilder().OutputInserted()
        );
        Assert.Throws<InvalidOperationException>(
            () => new PostgreSqlCommandBuilder().Returning("id").Returning("name")
        );
        Assert.Throws<InvalidOperationException>(
            () => new SqlServerCommandBuilder().OutputInserted("id").OutputInserted("name")
        );
    }

    [Fact]
    public void EngineBuildersExposeTheirOwnReturnAndQuerySyntax()
    {
        Assert.Equal("SqlServer", new SqlServerDialect().Name);
        Assert.Equal("PostgreSQL", new PostgreSqlDialect().Name);
        Assert.Equal("SQLite", new SqliteDialect().Name);
        Assert.Equal("MySQL", new MySqlDialect().Name);
        Assert.Equal("MariaDB", new MariaDbDialect().Name);
        Assert.Equal("SELECT 1", new PostgreSqlQueryBuilder().Select($"1").Compile().Text);
        Assert.Equal("SELECT 1", new MariaDbQueryBuilder().Select($"1").Compile().Text);
        Assert.Equal("p", new MySqlCommandBuilder().Procedure("p").Compile().Text);

        var sqlServer = new SqlServerCommandBuilder().OutputInserted("id")
            .Update($"items").Set($"id = {1}").Compile();
        var postgreSql = new PostgreSqlCommandBuilder().Returning("id")
            .DeleteFrom($"items").Compile();
        var sqlite = new SqliteCommandBuilder().Returning("id")
            .Update($"items").Set($"id = {1}").Compile();
        var mariaDb = new MariaDbCommandBuilder().Returning("id")
            .DeleteFrom($"items").Compile();

        Assert.Equal("UPDATE items SET id = @p0 OUTPUT INSERTED.[id]", sqlServer.Text);
        Assert.Equal("DELETE FROM items RETURNING \"id\"", postgreSql.Text);
        Assert.Equal("UPDATE items SET id = @p0 RETURNING \"id\"", sqlite.Text);
        Assert.Equal("DELETE FROM items RETURNING `id`", mariaDb.Text);
    }

    [Fact]
    public void SqlServerMaxRecursionHasBoundedCardinality()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SqlServerQueryBuilder().MaxRecursion(-1)
        );
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SqlServerQueryBuilder().MaxRecursion(32768)
        );
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SqlServerQueryBuilder().MaxRecursion(1).MaxRecursion(2)
        );
    }
}
