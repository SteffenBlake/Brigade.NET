using BenchmarkDotNet.Running;
using Brigade.Net.Benchmarks.DatabaseQuerying.Compilation;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

if (args is ["--print-sql"])
{
    using var context = new CompilationDbContext();
    var mise = ComplexQueryFactory.CreateMiseQuery().Compile().Text;
    var efCore = ComplexQueryFactory.CreateEfQuery(context).ToQueryString();
    Console.WriteLine("MISE SQL:");
    Console.WriteLine(mise);
    Console.WriteLine("EF CORE SQL:");
    Console.WriteLine(efCore);
    return;
}

BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).Run(args);
