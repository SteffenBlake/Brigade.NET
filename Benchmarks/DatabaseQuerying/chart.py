"""Draw the ORM × database point lookup chart from a BenchmarkDotNet JSON report."""

import json
import math
import sys
from pathlib import Path
from xml.sax.saxutils import escape


def main() -> None:
    report = json.loads(Path(sys.argv[1]).read_text())
    output = Path(sys.argv[2])
    databases = ["sqlserver", "postgresql", "mysql", "mariadb", "sqlite"]
    methods = ["MiseAsync", "DapperAsync", "EfCoreAsync"]
    names = {"MiseAsync": "Mise", "DapperAsync": "Dapper", "EfCoreAsync": "EF Core"}
    colors = {"MiseAsync": "#2563eb", "DapperAsync": "#ea580c", "EfCoreAsync": "#16a34a"}
    values = {}
    for benchmark in report["Benchmarks"]:
        if benchmark["Statistics"] is None:
            continue
        database = benchmark["Parameters"].split("=", 1)[1]
        values[database, benchmark["Method"]] = benchmark["Statistics"]["Mean"] / 1_000_000

    missing = [(db, method) for db in databases for method in methods if (db, method) not in values]
    if missing:
        raise ValueError(f"Missing results: {missing}")

    width, height = 1050, 580
    left, right, top, bottom = 80, 35, 95, 100
    plot_width = width - left - right
    plot_height = height - top - bottom
    maximum = max(values.values()) * 1.2
    group_width = plot_width / len(databases)
    bar_width = 38
    parts = [
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">',
        '<rect width="100%" height="100%" fill="white"/>',
        '<text x="80" y="40" font-size="24" font-family="sans-serif">Point lookup latency by ORM and database</text>',
        '<text x="80" y="65" font-size="14" fill="#555" font-family="sans-serif">Lower is faster · one SQL read of a keyed row · milliseconds per operation</text>',
    ]

    for tick in range(6):
        value = maximum * tick / 5
        y = top + plot_height * (1 - tick / 5)
        parts.append(f'<line x1="{left}" y1="{y:.1f}" x2="{width-right}" y2="{y:.1f}" stroke="#ddd"/>')
        parts.append(f'<text x="{left-12}" y="{y+5:.1f}" text-anchor="end" font-size="13" font-family="sans-serif">{value:.2f}</text>')

    for index, database in enumerate(databases):
        center = left + group_width * (index + 0.5)
        for offset, method in enumerate(methods):
            value = values[database, method]
            bar_height = plot_height * value / maximum
            x = center + (offset - 1) * (bar_width + 5) - bar_width / 2
            y = top + plot_height - bar_height
            parts.append(f'<rect x="{x:.1f}" y="{y:.1f}" width="{bar_width}" height="{bar_height:.1f}" fill="{colors[method]}"/>')
            parts.append(f'<text x="{x+bar_width/2:.1f}" y="{y-7:.1f}" text-anchor="middle" font-size="12" font-family="sans-serif">{value:.2f}</text>')
        parts.append(f'<text x="{center:.1f}" y="{height-bottom+27}" text-anchor="middle" font-size="14" font-family="sans-serif">{escape(database)}</text>')

    for index, method in enumerate(methods):
        x = left + index * 140
        parts.append(f'<rect x="{x}" y="{height-35}" width="18" height="18" fill="{colors[method]}"/>')
        parts.append(f'<text x="{x+25}" y="{height-20}" font-size="14" font-family="sans-serif">{names[method]}</text>')

    parts.append("</svg>")
    output.write_text("\n".join(parts))

    minimum_log = math.log10(min(values.values()))
    maximum_log = math.log10(max(values.values()))
    heatmap = [
        '<svg xmlns="http://www.w3.org/2000/svg" width="1050" height="440" viewBox="0 0 1050 440">',
        '<rect width="100%" height="100%" fill="white"/>',
        '<text x="150" y="42" font-size="24" font-family="sans-serif">Point lookup latency matrix</text>',
        '<text x="150" y="68" font-size="14" fill="#555" font-family="sans-serif">Each cell shows microseconds per operation; darker means slower. Color uses a log scale.</text>',
    ]
    for column, database in enumerate(databases):
        x = 150 + column * 175
        heatmap.append(f'<text x="{x+82}" y="112" text-anchor="middle" font-size="15" font-family="sans-serif">{escape(database)}</text>')
        for row_index, method in enumerate(methods):
            y = 135 + row_index * 82
            value = values[database, method]
            scale = (math.log10(value) - minimum_log) / (maximum_log - minimum_log)
            lightness = 94 - 52 * scale
            heatmap.append(f'<rect x="{x}" y="{y}" width="165" height="72" rx="8" fill="hsl(215 85% {lightness:.1f}%)"/>')
            heatmap.append(f'<text x="{x+82}" y="{y+44}" text-anchor="middle" font-size="21" font-family="sans-serif">{value*1000:.0f} μs</text>')
    for row_index, method in enumerate(methods):
        y = 135 + row_index * 82
        heatmap.append(f'<text x="135" y="{y+44}" text-anchor="end" font-size="17" font-family="sans-serif">{names[method]}</text>')
    heatmap.append("</svg>")
    output.with_name(f"{output.stem}-matrix.svg").write_text("\n".join(heatmap))


if __name__ == "__main__":
    main()
