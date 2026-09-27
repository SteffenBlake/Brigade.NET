# Complex query SQL rendering

This project compares a prebuilt Mise SQL Server query with the equivalent prebuilt EF Core LINQ query. Each timed call returns a SQL string. It does not open a database connection. The query joins accounts, purchases, and shipments; checks for a qualifying purchase line with `EXISTS`; filters rows; groups by region and category; applies `HAVING`; sorts; and pages the result.

Three cases run:

| Case | Timed action |
|---|---|
| MiseCompile | `QueryBuilder.Compile().Text` on the same query builder |
| EfCoreCached | `ToQueryString()` on the same LINQ query with EF Core's normal cache |
| EfCoreUncached | `ToQueryString()` on the same LINQ query with the compiled-query cache bypassed |

EF Core's SQL text includes debug formatting and parameter declarations. `ToQueryString()` is its public SQL-text API, but the text is meant for debugging and might not be directly executable. The uncached case replaces EF Core's internal `ICompiledQueryCache`; that API is not stable and must be checked when EF Core is upgraded. Both EF cases use the same query expression and produce identical SQL. Mise and EF Core express the same logical result, though each renders SQL in its own form.

```sh
dotnet run -c Release --project Benchmarks/DatabaseQuerying/Compilation -- --print-sql
dotnet run -c Release --project Benchmarks/DatabaseQuerying/Compilation -- \
  --filter '*QueryCompilationBenchmarks*' --exporters json \
  --artifacts Benchmarks/DatabaseQuerying/Compilation/Results
python3 Benchmarks/DatabaseQuerying/Compilation/chart.py \
  Benchmarks/DatabaseQuerying/Compilation/Results/results/Brigade.Net.Benchmarks.DatabaseQuerying.Compilation.QueryCompilationBenchmarks-report-full-compressed.json \
  Benchmarks/DatabaseQuerying/Compilation/Charts/query-compilation.svg
```

Run the benchmark again on a quiet host before treating one session's ratios as stable.
