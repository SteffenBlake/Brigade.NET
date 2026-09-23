using System.Data.Common;
using System.Reflection;
using Brigade.Net.Mise;

namespace Brigade.Net.Mise.Tests;

public sealed class ContractTests
{
    [Fact]
    public void MetadataAttributesExposeConstructorValuesAndExpectedUsage()
    {
        Assert.True(typeof(TableAttributeBase).IsAbstract);
        Assert.Equal("person_id", new MiseColumnAttribute("person_id").Name);
        Assert.Equal(2, new MisePrimaryKeyAttribute(2).Position);
        Assert.Equal("p", new MiseAliasAttribute("p").Name);

        var relationship = new MiseRelationshipAttribute("Owner", typeof(string), "owner_id", "id");
        Assert.Equal("Owner", relationship.Name);
        Assert.Equal(typeof(string), relationship.TargetType);
        Assert.Equal("owner_id", relationship.SourceColumn);
        Assert.Equal("id", relationship.TargetColumn);

        AssertUsage<TableAttributeBase>(AttributeTargets.Class | AttributeTargets.Struct);
        AssertUsage<MiseColumnAttribute>(AttributeTargets.Property, inherited: true);
        AssertUsage<MiseAliasAttribute>(AttributeTargets.Class | AttributeTargets.Struct, allowMultiple: true);
        AssertUsage<MiseRelationshipAttribute>(AttributeTargets.Class | AttributeTargets.Struct, allowMultiple: true);
    }

    [Fact]
    public void SinglePurposePropertyMetadataTargetsProperties()
    {
        AssertUsage<MisePrimaryKeyAttribute>(AttributeTargets.Property, inherited: true);
        AssertUsage<MiseDatabaseGeneratedAttribute>(AttributeTargets.Property, inherited: true);
        AssertUsage<MiseComputedAttribute>(AttributeTargets.Property, inherited: true);
        AssertUsage<MiseExcludeFromInsertAttribute>(AttributeTargets.Property, inherited: true);
        AssertUsage<MiseExcludeFromUpdateAttribute>(AttributeTargets.Property, inherited: true);
    }

    [Fact]
    public void RowContractUsesStaticAbstractReaderOperations()
    {
        Assert.True(typeof(RowAttributeBase).IsAbstract);
        AssertUsage<RowAttributeBase>(AttributeTargets.Class | AttributeTargets.Struct);

        var methods = typeof(IMiseRow<>).GetMethods(BindingFlags.Public | BindingFlags.Static);
        Assert.Collection(
            methods.OrderBy(method => method.Name),
            method => Assert.Equal("BindOrdinals", method.Name),
            method => Assert.Equal("Materialize", method.Name)
        );
        Assert.All(methods, method => Assert.True(method.IsAbstract));
    }

    [Fact]
    public void ConfigExposesOnlyConnectionStringAndProviderFactory()
    {
        var properties = typeof(IMiseConfig).GetProperties();

        Assert.Collection(
            properties.OrderBy(property => property.Name),
            property =>
            {
                Assert.Equal(nameof(IMiseConfig.ConnectionString), property.Name);
                Assert.Equal(typeof(string), property.PropertyType);
            },
            property =>
            {
                Assert.Equal(nameof(IMiseConfig.ProviderFactory), property.Name);
                Assert.Equal(typeof(DbProviderFactory), property.PropertyType);
            }
        );
    }

    [Fact]
    public void NullMappingFaultHasSafeStructuralContext()
    {
        var exception = new MiseMappingException(typeof(ContractTests), "Name", "person_name", 3);

        Assert.Equal(typeof(ContractTests), exception.ResultType);
        Assert.Equal("Name", exception.MemberName);
        Assert.Equal("person_name", exception.ColumnName);
        Assert.Equal(3, exception.Ordinal);
        Assert.Contains(typeof(ContractTests).FullName!, exception.Message);
        Assert.Contains("Name", exception.Message);
        Assert.Contains("person_name", exception.Message);
        Assert.Contains("3", exception.Message);
    }

    [Fact]
    public void InvalidGeneratedMappingFaultNamesResultType()
    {
        var exception = new MiseInvalidMappingException(typeof(ContractTests), "column was not projected");

        Assert.Equal(typeof(ContractTests), exception.ResultType);
        Assert.Contains(typeof(ContractTests).FullName!, exception.Message);
        Assert.Contains("column was not projected", exception.Message);
    }

    [Fact]
    public void RuntimeCoreContainsNoReflectionMapperOrDialectType()
    {
        var sourceRoot = Path.Combine(RepositoryRoot(), "Source", "Mise");
        var source = string.Join(
            "\n",
            Directory.GetFiles(sourceRoot, "*.cs").Select(File.ReadAllText)
        );

        Assert.DoesNotContain("System.Reflection", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Activator.CreateInstance", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetProperties(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SqlServer", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PostgreSQL", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SQLite", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MySQL", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MariaDb", source, StringComparison.Ordinal);
    }

    [Fact]
    public void PublicDatabaseTerminalsUseResultsAndAvoidLinqNames()
    {
        var methods = typeof(DbReader).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Concat(typeof(DbWriter).GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .Where(method => method.DeclaringType is not null
                && method.DeclaringType.Namespace == typeof(DbReader).Namespace)
            .DistinctBy(method => method.Name)
            .ToArray();

        Assert.DoesNotContain(methods, method => method.Name is "First" or "Single"
            || method.Name.Contains("OrDefault", StringComparison.Ordinal));
        foreach (var method in methods.Where(method => method.Name.EndsWith("Async", StringComparison.Ordinal)
            && method.Name != nameof(IAsyncDisposable.DisposeAsync)))
        {
            Assert.True(method.ReturnType.IsGenericType);
            Assert.Equal(typeof(Task<>), method.ReturnType.GetGenericTypeDefinition());
            Assert.Equal(
                typeof(Brigade.Net.Core.Results.Result<>),
                method.ReturnType.GetGenericArguments()[0].GetGenericTypeDefinition()
            );
        }
    }

    [Fact]
    public void ReadAndWriteBuilderContractsAreSeparate()
    {
        Assert.Equal(typeof(CompiledSql), typeof(IQueryBuilder).GetMethod("Compile")!.ReturnType);
        Assert.Equal(typeof(CompiledSql), typeof(ICommandBuilder).GetMethod("Compile")!.ReturnType);
        Assert.DoesNotContain(typeof(ICommandBuilder), typeof(QueryBuilder).GetInterfaces());
        Assert.DoesNotContain(typeof(IQueryBuilder), typeof(CommandBuilder).GetInterfaces());

        var execute = typeof(DbWriter).GetMethod(nameof(DbWriter.ExecuteAsync))!;
        Assert.Equal(typeof(ICommandBuilder), execute.GetParameters()[0].ParameterType);
        Assert.All(typeof(DbReader).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => method.Name is nameof(DbReader.ListAsync) or nameof(DbReader.FirstOrNotFoundAsync)
                or nameof(DbReader.ScalarAsync) or nameof(DbReader.ExistsAsync)),
            method => Assert.Equal(typeof(IQueryBuilder), method.GetParameters()[0].ParameterType));
    }

    private static void AssertUsage<TAttribute>(
        AttributeTargets targets,
        bool allowMultiple = false,
        bool inherited = false
    )
        where TAttribute : Attribute
    {
        var usage = typeof(TAttribute).GetCustomAttribute<AttributeUsageAttribute>();

        Assert.NotNull(usage);
        Assert.Equal(targets, usage.ValidOn);
        Assert.Equal(allowMultiple, usage.AllowMultiple);
        Assert.Equal(inherited, usage.Inherited);
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Brigade.NET.slnx")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new DirectoryNotFoundException();
    }
}
