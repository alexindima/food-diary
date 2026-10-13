import json
import tempfile
import unittest
import sys
from email.message import EmailMessage
from pathlib import Path
from unittest.mock import patch

import bugtriage


class BridgeTests(unittest.TestCase):
    def test_completion_forwards_display_fields_without_replacing_detailed_evidence(self):
        with tempfile.TemporaryDirectory() as temp:
            lease = Path(temp) / "lease.json"
            result = Path(temp) / "result.json"
            lease.write_text(json.dumps({"id": "a9713e28-c439-434a-9f91-ebca6036824d", "leaseToken": "06bc7f7e-8348-4938-a4bb-a7a53c059f20"}))
            result.write_text(json.dumps({"outcome": "needs_information", "summary": "Detailed evidence", "title": "Button does not respond", "shortSummary": "More reproduction steps needed"}))
            arguments = ["bugtriage.py", "complete", "--lease", str(lease), "--result", str(result)]
            with patch.object(sys, "argv", arguments), patch.object(bugtriage, "load_config", return_value={}), patch.object(bugtriage, "call_api") as request:
                bugtriage.main()
            payload = request.call_args.args[3]
            self.assertEqual("Button does not respond", payload["title"])
            self.assertEqual("More reproduction steps needed", payload["shortSummary"])
            self.assertEqual("Detailed evidence", payload["summary"])

    def test_images_cannot_escape_output_using_mail_filenames(self):
        message = EmailMessage()
        message.set_content("Steps")
        message.add_attachment(b"test-image", maintype="image", subtype="png", filename="../../outside.png")
        message.add_attachment(b"do not execute", maintype="application", subtype="octet-stream", filename="run.exe")
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            files = bugtriage.extract_images(message.as_bytes(), root)
            self.assertEqual(1, len(files))
            self.assertEqual(root, Path(files[0]).parent)
            self.assertEqual(b"test-image", Path(files[0]).read_bytes())
            self.assertEqual(1, len(list(root.iterdir())))

    def test_remote_plain_http_and_url_credentials_are_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / "config.json"
            for url in ("http://example.com", "https://user:password@example.com", "file:///tmp/private"):
                path.write_text(json.dumps({"baseUrl": url, "apiKey": "k" * 32}))
                with self.assertRaises(ValueError):
                    bugtriage.load_config(path)

    def test_failed_mime_download_keeps_recoverable_lease(self):
        report = {"id": "a9713e28-c439-434a-9f91-ebca6036824d", "leaseToken": "06bc7f7e-8348-4938-a4bb-a7a53c059f20"}
        with tempfile.TemporaryDirectory() as temp, patch.object(bugtriage, "call_api", side_effect=[report, OSError()]):
            with self.assertRaises(OSError):
                bugtriage.claim({}, Path(temp))
            self.assertEqual(report, json.loads((Path(temp) / report["id"] / "lease.json").read_text()))


if __name__ == "__main__":
    unittest.main()
