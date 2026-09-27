import contextlib
import importlib.util
import io
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET

spec = importlib.util.spec_from_file_location("pr_checks", Path(__file__).parents[1] / "pr_checks.py")
checks = importlib.util.module_from_spec(spec)
spec.loader.exec_module(checks)


def coverage(first, second):
    return {"Brigade.Net.Core.dll": {"/_/Source/Core/Value.cs": {"Value": {"Get()": {
        "Lines": {"10": first, "11": second},
        "Branches": [{"Line": 10, "Offset": 1, "EndOffset": 2, "Path": 0, "Hits": first},
                     {"Line": 10, "Offset": 1, "EndOffset": 3, "Path": 1, "Hits": second}]
    }}}}}


class PrChecksTests(unittest.TestCase):
    def test_merges_complementary_execution_without_double_counting(self):
        with tempfile.TemporaryDirectory() as work:
            paths = [Path(work) / "first.json", Path(work) / "second.json"]
            paths[0].write_text(json.dumps(coverage(1, 0)))
            paths[1].write_text(json.dumps(coverage(0, 1)))
            module = checks.merge(paths)["Brigade.Net.Core.dll"]
            self.assertEqual((2, 2), checks.count(module, "lines"))
            self.assertEqual((2, 2), checks.count(module, "branches"))

    def test_source_scope_handles_path_maps_and_excludes_generated_files(self):
        self.assertEqual("Source/Core/Value.cs", checks.source_path("/_/Source/Core/Value.cs"))
        self.assertEqual("Source/Core/Value.cs", checks.source_path(r"C:\repo\Source\Core\Value.cs"))
        self.assertIsNone(checks.source_path("/_/Tests/Core.Tests/Value.cs"))
        self.assertIsNone(checks.source_path("/_/Source/Core/obj/Generated.cs"))
        self.assertIsNone(checks.source_path("/_/Source/Core/.generated/Generated.cs"))

    def test_threshold_uses_exact_counts(self):
        self.assertTrue(checks.passes(19, 20))
        self.assertFalse(checks.passes(18999, 20000))
        self.assertFalse(checks.passes(0, 0))

    def fixture(self, root, first=1, second=1, exit_code=0):
        results = root / "results"
        suite = results / "Unit.Tests"
        suite.mkdir(parents=True)
        root_xml = ET.Element("TestRun", xmlns=checks.TRX_NS["t"])
        ET.SubElement(ET.SubElement(root_xml, "ResultSummary"), "Counters",
                      {"total": "2", "executed": "2", "passed": "2" if not exit_code else "1", "failed": str(exit_code)})
        ET.ElementTree(root_xml).write(suite / "tests.trx")
        folder = suite / "collector-id"
        folder.mkdir()
        (folder / "coverage.json").write_text(json.dumps(coverage(first, second)))
        (results / "executions.json").write_text(json.dumps([{"suite": "Unit.Tests", "exit_code": exit_code, "coverage": True}]))
        project = root / "Unit.Tests/Test.csproj"
        return results, project

    def test_pr_fails_low_coverage_and_reports_skill_hint(self):
        with tempfile.TemporaryDirectory() as work:
            results, project = self.fixture(Path(work), second=0)
            with patch.object(checks, "RESULTS", results), patch.object(checks, "test_projects", return_value=[project]), \
                    contextlib.redirect_stdout(io.StringIO()) as output:
                self.assertEqual(1, checks.report("success"))
            self.assertIn("writing-unit-tests/SKILL.md", output.getvalue())
            self.assertIn("::error::", output.getvalue())
            self.assertIn("50.00%", (results / "coverage.svg").read_text())

    def test_main_reports_failed_tests_and_low_coverage_without_failing(self):
        with tempfile.TemporaryDirectory() as work:
            results, project = self.fixture(Path(work), second=0, exit_code=1)
            with patch.object(checks, "RESULTS", results), patch.object(checks, "test_projects", return_value=[project]), \
                    contextlib.redirect_stdout(io.StringIO()) as output:
                self.assertEqual(0, checks.report("success", report_only=True))
            self.assertIn("| Unit.Tests | Failed |", output.getvalue())
            self.assertIn("::warning::", output.getvalue())
            self.assertNotIn("::error::", output.getvalue())
            self.assertIn("50.00%", (results / "coverage.svg").read_text())

    def test_failed_build_produces_unavailable_badge_and_success_in_report_mode(self):
        with tempfile.TemporaryDirectory() as work:
            results = Path(work) / "results"
            with patch.object(checks, "RESULTS", results), patch.object(checks, "test_projects", return_value=[Path("Unit.Tests/Test.csproj")]), \
                    contextlib.redirect_stdout(io.StringIO()):
                self.assertEqual(0, checks.report("failure", report_only=True))
                self.assertEqual(1, checks.report("failure"))
            self.assertIn("N/A", (results / "coverage.svg").read_text())

    def test_missing_coverage_cannot_pass_pr_or_publish_partial_badge(self):
        with tempfile.TemporaryDirectory() as work:
            results, project = self.fixture(Path(work))
            (results / "Unit.Tests/collector-id/coverage.json").unlink()
            with patch.object(checks, "RESULTS", results), patch.object(checks, "test_projects", return_value=[project]), \
                    contextlib.redirect_stdout(io.StringIO()):
                self.assertEqual(1, checks.report("success"))
            self.assertIn("N/A", (results / "coverage.svg").read_text())

    def test_ignores_vstest_attachment_copies(self):
        with tempfile.TemporaryDirectory() as work:
            results, project = self.fixture(Path(work))
            duplicate = results / "Unit.Tests/run/In/host/coverage.json"
            duplicate.parent.mkdir(parents=True)
            duplicate.write_text(json.dumps(coverage(1, 1)))
            with patch.object(checks, "RESULTS", results), patch.object(checks, "test_projects", return_value=[project]), \
                    contextlib.redirect_stdout(io.StringIO()):
                self.assertEqual(0, checks.report("success"))
            self.assertIn("100.00%", (results / "coverage.svg").read_text())


if __name__ == "__main__":
    unittest.main()
