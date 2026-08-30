import importlib.util
import tempfile
import unittest
from pathlib import Path


SCRIPT_PATH = Path(__file__).with_name("provision_users.py")
SPEC = importlib.util.spec_from_file_location("provision_users", SCRIPT_PATH)
provision_users = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(provision_users)


class ProvisionConfigurationTests(unittest.TestCase):
    def test_loads_portainer_names_and_derives_public_api_settings(self):
        with tempfile.TemporaryDirectory() as directory:
            env_file = Path(directory) / "portainer.env"
            env_file.write_text(
                "InitialAdmin__Email=admin@example.com\n"
                "InitialAdmin__Password=secret\n"
                "CORS__Origins=http://43.156.121.95:3000\n"
                "WAMS_COMPANY_ID=7\n"
                "WAMS_DEFAULT_PASSWORD=temp-pass\n",
                encoding="utf-8",
            )

            config = provision_users.load_configuration(env_file, environment={})

        self.assertEqual(config["base_url"], "http://43.156.121.95:8080")
        self.assertEqual(config["admin_email"], "admin@example.com")
        self.assertEqual(config["admin_password"], "secret")
        self.assertEqual(config["company_id"], 7)
        self.assertEqual(config["default_password"], "temp-pass")
        self.assertEqual(config["email_domain"], "example.com")

    def test_explicit_environment_values_override_portainer_file(self):
        with tempfile.TemporaryDirectory() as directory:
            env_file = Path(directory) / "portainer.env"
            env_file.write_text(
                "InitialAdmin__Email=file@example.com\n"
                "InitialAdmin__Password=file-secret\n",
                encoding="utf-8",
            )

            config = provision_users.load_configuration(
                env_file,
                environment={
                    "WAMS_BASE_URL": "https://api.example.com",
                    "WAMS_ADMIN_EMAIL": "override@example.com",
                    "WAMS_ADMIN_PASSWORD": "override-secret",
                },
            )

        self.assertEqual(config["base_url"], "https://api.example.com")
        self.assertEqual(config["admin_email"], "override@example.com")
        self.assertEqual(config["admin_password"], "override-secret")


if __name__ == "__main__":
    unittest.main()
