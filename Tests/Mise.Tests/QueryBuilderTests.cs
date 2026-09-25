using System.Runtime.CompilerServices;
using Brigade.Net.Mise;

namespace Brigade.Net.Mise.Tests;

public sealed class QueryBuilderTests
{
    [Fact]
    public void ClauseOrderDeterminesParameterOrder()
    {
        const string table = "users";
        var query = new QueryBuilder()
            .Where($"id = {9}")
            .Select($"{2} AS rank")
            .From($"{table:raw}")
            .Select($"name")
            .OrderBy($"{4}");

        var compiled = query.Compile();

        Assert.Equal(
            "SELECT @p0 AS rank, name FROM users WHERE id = @p1 ORDER BY @p2",
            compiled.Text
        );
        Assert.Equal(
            ["@p0", "@p1", "@p2"],
            compiled.Parameters.Select(parameter => parameter.Name)
        );
        Assert.Equal([2, 9, 4], compiled.Parameters.Select(parameter => parameter.Value));
    }

    [Fact]
    public void RepeatedHoleGetsDistinctParametersAndCompileIsFresh()
    {
        var value = "'; DROP TABLE users; --";
        var fragment = FormattableStringFactory.Create("{0} = {0}", value);
        var query = new QueryBuilder().Select(fragment);

        var first = query.Compile();
        var second = query.Compile();

        Assert.Equal("SELECT @p0 = @p1", first.Text);
        Assert.Equal([value, value], first.Parameters.Select(parameter => parameter.Value));
        Assert.NotSame(first.Parameters, second.Parameters);
        Assert.DoesNotContain(value, first.Text);
    }

    [Fact]
    public void InsertQuerySharesParameterNumbering()
    {
        const string source = "source";
        const string target = "target";
        var child = new QueryBuilder().Select($"{7}").From($"{source:raw}").Where($"id = {8}");
        var command = new CommandBuilder().InsertInto($"{target:raw}").FromQuery(child);

        var compiled = command.Compile();

        Assert.Equal("INSERT INTO target SELECT @p0 FROM source WHERE id = @p1", compiled.Text);
        Assert.Equal([7, 8], compiled.Parameters.Select(parameter => parameter.Value));
    }

    [Fact]
    public void DuplicateSourceIsRejected()
    {
        var query = new QueryBuilder().From($"one");

        Assert.Throws<InvalidOperationException>(() => query.From($"two"));
    }
}
