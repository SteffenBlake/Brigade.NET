import importlib.util
import io
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
from zipfile import ZipFile

spec = importlib.util.spec_from_file_location("release", Path(__file__).parents[1] / "release.py")
release = importlib.util.module_from_spec(spec)
spec.loader.exec_module(release)


def package(files):
    stream = io.BytesIO()
    with ZipFile(stream, "w") as archive:
        for name, contents in files.items():
            archive.writestr(name, contents)
    return stream.getvalue()


class ReleaseTests(unittest.TestCase):
    def test_accepts_suite_versions(self):
        for value in ["1.0.0", "0.1.0", "1.1.0-rc.1", "2.0.0-preview-2"]:
            with self.subTest(value=value):
                self.assertEqual(value, release.version(value))

    def test_rejects_ambiguous_or_invalid_versions(self):
        for value in ["expo-1.0.0", "v1.0.0", "1.0", "01.0.0", "1.0.0-rc.01", "1.0.0+build", "1.0.0-RC", "1.0.0\n", "1.٢.0"]:
            with self.subTest(value=value), self.assertRaises(ValueError):
                release.version(value)

    def test_manifest_classifies_all_source_projects(self):
        self.assertEqual(20, len(release.public_packages()))
        self.assertEqual(2, len(release.catalog()["internal"]))

    def test_existing_package_allows_only_added_signature(self):
        local = package({"lib/test.dll": b"assembly", "test.nuspec": b"metadata"})
        signed = package({"test.nuspec": b"metadata", "lib/test.dll": b"assembly", ".signature.p7s": b"signature"})
        release.same_contents(local, signed)
        changed = package({"lib/test.dll": b"different", "test.nuspec": b"metadata"})
        with self.assertRaises(ValueError):
            release.same_contents(local, changed)

    def test_release_flag_must_match_version(self):
        with tempfile.TemporaryDirectory() as work:
            event = Path(work) / "event.json"
            event.write_text(json.dumps({"release": {"tag_name": "1.0.0-rc.1", "draft": False, "prerelease": False}}))
            with patch.dict(os.environ, {"GITHUB_EVENT_PATH": str(event)}), self.assertRaises(ValueError):
                release.release_event()

    def test_release_tag_must_match_commit(self):
        with tempfile.TemporaryDirectory() as work:
            event = Path(work) / "event.json"
            event.write_text(json.dumps({"release": {"tag_name": "1.0.0", "draft": False, "prerelease": False}}))
            with patch.dict(os.environ, {"GITHUB_EVENT_PATH": str(event), "GITHUB_SHA": "expected"}), \
                    patch.object(release.subprocess, "check_output", return_value="wrong\n"), self.assertRaises(ValueError):
                release.release_event()

    def test_release_event_exports_validated_version(self):
        with tempfile.TemporaryDirectory() as work:
            event = Path(work) / "event.json"
            output = Path(work) / "output"
            event.write_text(json.dumps({"release": {"tag_name": "1.0.0", "draft": False, "prerelease": False}}))
            with patch.dict(os.environ, {"GITHUB_EVENT_PATH": str(event), "GITHUB_SHA": "commit", "GITHUB_OUTPUT": str(output)}), \
                    patch.object(release.subprocess, "check_output", return_value="commit\n"):
                release.release_event()
            self.assertEqual("version=1.0.0\n", output.read_text())

    def test_artifacts_reject_tampering_and_wrong_commit(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            entry = {"id": "Brigade.Net.Core", "group": "core", "file": "Brigade.Net.Core.1.0.0.nupkg"}
            data = package({"lib/test.dll": b"assembly"})
            entry["sha256"] = release.hashlib.sha256(data).hexdigest()
            manifest = {"version": "1.0.0", "commit": "commit", "packages": [entry]}
            (root / "release-manifest.json").write_text(json.dumps(manifest))
            (root / entry["file"]).write_bytes(data)
            with patch.object(release, "PACKAGES", root), \
                    patch.object(release, "public_packages", return_value=[("core", "Core")]), \
                    patch.object(release.ET, "parse", return_value=release.ET.ElementTree(release.ET.Element("Project"))), \
                    patch.object(release.subprocess, "check_output", return_value="commit\n"):
                self.assertEqual(manifest, release.artifacts("1.0.0"))
                (root / entry["file"]).write_bytes(b"tampered")
                with self.assertRaisesRegex(ValueError, "hash mismatch"):
                    release.artifacts("1.0.0")
                with self.assertRaisesRegex(ValueError, "version or commit"):
                    release.artifacts("2.0.0")

    def test_publish_skips_matching_contents_without_pushing(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            entry = {"id": "Brigade.Net.Core", "group": "core", "file": "core.nupkg"}
            data = package({"lib/test.dll": b"assembly"})
            (root / entry["file"]).write_bytes(data)
            with patch.object(release, "PACKAGES", root), \
                    patch.object(release, "artifacts", return_value={"packages": [entry]}), \
                    patch.object(release, "remote_package", return_value=data), \
                    patch.dict(os.environ, {"NUGET_API_KEY": "test-key"}), \
                    patch.object(release.subprocess, "run") as push:
                release.publish("1.0.0", "core")
                push.assert_not_called()

    def test_remote_404_is_unpublished_but_server_errors_fail(self):
        entry = {"id": "Brigade.Net.Core"}
        for code in [404, 403, 500]:
            error = release.urllib.error.HTTPError("https://example.test", code, "error", {}, None)
            with patch.object(release.urllib.request, "urlopen", side_effect=error):
                if code == 404:
                    self.assertIsNone(release.remote_package(entry, "1.0.0"))
                else:
                    with self.assertRaises(release.urllib.error.HTTPError):
                        release.remote_package(entry, "1.0.0")

    def test_publish_never_pushes_when_existing_contents_differ(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            entry = {"id": "Brigade.Net.Core", "group": "core", "file": "core.nupkg"}
            (root / entry["file"]).write_bytes(package({"lib/test.dll": b"new"}))
            with patch.object(release, "PACKAGES", root), \
                    patch.object(release, "artifacts", return_value={"packages": [entry]}), \
                    patch.object(release, "remote_package", return_value=package({"lib/test.dll": b"old"})), \
                    patch.dict(os.environ, {"NUGET_API_KEY": "test-key"}), \
                    patch.object(release.subprocess, "run") as push:
                with self.assertRaises(ValueError):
                    release.publish("1.0.0", "core")
                push.assert_not_called()

    def test_inspection_rejects_empty_package_and_wrong_dependency(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            name = "Brigade.Net.Core"
            filename = f"{name}.1.0.0.nupkg"
            metadata = '<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"><metadata><id>Brigade.Net.Core</id><version>1.0.0</version><repository commit="commit" />{dependencies}</metadata></package>'
            files = {"LICENSE": "license", "README.md": "readme", f"{name}.nuspec": metadata.format(dependencies="")}
            with patch.object(release, "PACKAGES", root), \
                    patch.object(release, "public_packages", return_value=[("core", "Core")]), \
                    patch.object(release.subprocess, "check_output", return_value="commit\n"):
                (root / filename).write_bytes(package(files))
                with self.assertRaisesRegex(ValueError, "Missing assembly"):
                    release.inspect("1.0.0")
                files[f"lib/net10.0/{name}.dll"] = "assembly"
                files[f"{name}.nuspec"] = metadata.format(dependencies='<dependencies><group><dependency id="Brigade.Net.Other" version="0.9.0" /></group></dependencies>')
                (root / filename).write_bytes(package(files))
                with self.assertRaisesRegex(ValueError, "dependencies do not match"):
                    release.inspect("1.0.0")
                files[f"{name}.nuspec"] = metadata.format(dependencies="")
                (root / filename).write_bytes(package(files))
                release.inspect("1.0.0")
                self.assertEqual("1.0.0", json.loads((root / "release-manifest.json").read_text())["version"])


if __name__ == "__main__":
    unittest.main()
