"""Draw a log-scale chart from this benchmark's BenchmarkDotNet JSON report."""

import json
import math
import sys
from pathlib import Path


def main() -> None:
    report = json.loads(Path(sys.argv[1]).read_text())
    output = Path(sys.argv[2])
    labels = {
        "MiseCompile": "Mise: render",
        "EfCoreCached": "EF Core: cache hit",
        "EfCoreUncached": "EF Core: cache bypass",
    }
    colors = {
        "MiseCompile": "#2563eb",
        "EfCoreCached": "#16a34a",
        "EfCoreUncached": "#ea580c",
    }
    values = {
        row["Method"]: row["Statistics"]["Mean"] / 1_000
        for row in report["Benchmarks"]
        if row["Statistics"] is not None
    }
    if set(values) != set(labels):
        raise ValueError(f"Expected {set(labels)}, got {set(values)}")

    width, height = 1060, 400
    left, right = 260, 55
    min_log, max_log = 0, 4
    plot_width = width - left - right
    parts = [
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">',
        '<rect width="100%" height="100%" fill="white"/>',
        '<text x="55" y="42" font-size="25" font-family="sans-serif">Complex query to SQL text</text>',
        '<text x="55" y="68" font-size="14" fill="#555" font-family="sans-serif">SQL Server dialect · warm benchmark processes · lower is faster · log scale</text>',
    ]
    for tick in (1, 10, 100, 1000, 10000):
        x = left + (math.log10(tick) - min_log) / (max_log - min_log) * plot_width
        parts.append(f'<line x1="{x:.1f}" y1="100" x2="{x:.1f}" y2="335" stroke="#ddd"/>')
        parts.append(f'<text x="{x:.1f}" y="360" text-anchor="middle" font-size="13" font-family="sans-serif">{tick:,} µs</text>')

    for index, method in enumerate(labels):
        y = 120 + index * 75
        value = values[method]
        end = left + (math.log10(value) - min_log) / (max_log - min_log) * plot_width
        parts.append(f'<text x="{left-18}" y="{y+24}" text-anchor="end" font-size="17" font-family="sans-serif">{labels[method]}</text>')
        parts.append(f'<rect x="{left}" y="{y}" width="{end-left:.1f}" height="32" rx="5" fill="{colors[method]}"/>')
        parts.append(f'<text x="{end+10:.1f}" y="{y+23}" font-size="15" font-family="sans-serif">{value:,.2f} µs</text>')

    parts.append("</svg>")
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text("\n".join(parts))


if __name__ == "__main__":
    main()
