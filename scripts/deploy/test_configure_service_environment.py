import os
from pathlib import Path
import tempfile
import unittest

from configure_service_environment import read_environment, write_environments


class ServiceEnvironmentTests(unittest.TestCase):
    def test_projection_keeps_own_settings_and_round_trips_sensitive_characters(self):
        environment = {
            "POSTGRES_PASSWORD": "foreign-database-canary",
            "Jwt__Secret": "foreign-signing-canary",
            "Paddle__ApiKey": "foreign-billing-canary",
            "MailRelayBroker__Password": "dollar $UNSET ${POSTGRES_PASSWORD} slash \\ quote \"\nsecond line",
            "RelaySmtp__Password": "own-smtp-password",
            "DirectMx__LocalDomain": "mail.example.test",
            "MailRelayDkim__PrivateKeyPath": "/run/secrets/dkim.pem",
            "MailRelayQueue__MaxAttempts": "8",
            "MAILRELAY_MailRelayQueue__BatchSize": "20",
            "FOODDIARY_ConnectionStrings__DefaultConnection": "foreign-connection-canary",
            "MailInboxSmtp__AllowedRecipients__19": "проверка@example.test",
            "MailInboxHttp__MetadataApiKey": "own-inbox-key",
            "TelegramBot__Token": "own-bot-token",
            "TelegramBot__OperationsEnabled": "true",
        }
        with tempfile.TemporaryDirectory() as directory:
            write_environments(directory, environment)
            relay = read_environment(Path(directory) / ".env.mail-relay")
            inbox = read_environment(Path(directory) / ".env.mail-inbox")
            bot = read_environment(Path(directory) / ".env.telegram-bot")
            initializer = read_environment(Path(directory) / ".env.mailrelay-init")

            for actual in [relay, inbox, bot, initializer]:
                for foreign in ["POSTGRES_PASSWORD", "Jwt__Secret", "Paddle__ApiKey", "FOODDIARY_ConnectionStrings__DefaultConnection"]:
                    self.assertNotIn(foreign, actual)
                for key, value in actual.items():
                    self.assertEqual(environment[key], value)
            self.assertIn("MailRelayBroker__Password", relay)
            self.assertNotIn("TelegramBot__Token", relay)
            self.assertNotIn("MailRelayBroker__Password", inbox)
            self.assertNotIn("MailInboxHttp__MetadataApiKey", bot)
            self.assertIn("MailInboxSmtp__AllowedRecipients__19", inbox)
            self.assertEqual({"MailRelayQueue__MaxAttempts": "8", "MAILRELAY_MailRelayQueue__BatchSize": "20"}, initializer)
            if os.name != "nt":
                self.assertEqual(0o600, (Path(directory) / ".env.mail-relay").stat().st_mode & 0o777)

    def test_replaces_old_values_and_drops_removed_keys(self):
        with tempfile.TemporaryDirectory() as directory:
            write_environments(directory, {"TelegramBot__Token": "old"})
            write_environments(directory, {"TelegramBot__WebAppUrl": "https://example.test"})
            self.assertEqual({"TelegramBot__WebAppUrl": "https://example.test"},
                             read_environment(Path(directory) / ".env.telegram-bot"))

    @unittest.skipIf(os.name == "nt", "Windows symlink creation depends on host privileges")
    def test_refuses_symlink_destination(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            other = root / "outside"
            other.write_text("keep")
            (root / ".env.mail-relay").symlink_to(other)
            with self.assertRaises(ValueError):
                write_environments(root, {"RelaySmtp__Password": "owned"})
            self.assertEqual("keep", other.read_text())


if __name__ == "__main__":
    unittest.main()
