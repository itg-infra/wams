#!/usr/bin/env python3
"""
Provisions the named users from USER_MATRIX_WAMS_FROM_CLIENT.xlsx (USER sheet) against
a running WAMS API. Idempotent: re-running skips users/roles that already exist.

Not part of DatabaseSeeder on purpose - these are real employees with real emails/passwords,
not catalog data, and the seeder runs on every app boot (incl. prod-pointed envs).

Usage:
    python3 scripts/provision_users.py [path/to/USER_MATRIX_WAMS_FROM_CLIENT.xlsx]

When run from this repository, the script automatically loads backend/portainer.env
and uses backend/USER_MATRIX_WAMS_FROM_CLIENT.xlsx when no matrix path is supplied.

Env vars (defaults match local dev .env):
    WAMS_BASE_URL       default http://localhost:8080
    WAMS_ADMIN_EMAIL    default admin@example.com
    WAMS_ADMIN_PASSWORD default Admin123!
    WAMS_COMPANY_ID     default 1
    WAMS_EMAIL_DOMAIN   default gerbangcahayautama.com
    WAMS_DEFAULT_PASSWORD  default one-time temp password assigned to every created user
"""
import json
import os
import re
import sys
from getpass import getpass
from pathlib import Path
from urllib.parse import urlsplit, urlunsplit
import urllib.error
import urllib.request

import openpyxl

SCRIPT_DIR = Path(__file__).resolve().parent
BACKEND_DIR = SCRIPT_DIR.parent
DEFAULT_ENV_FILE = BACKEND_DIR / "portainer.env"
DEFAULT_XLSX_PATH = BACKEND_DIR / "USER_MATRIX_WAMS_FROM_CLIENT.xlsx"


def parse_env_file(path):
    values = {}
    path = Path(path)
    if not path.is_file():
        return values

    for raw_line in path.read_text(encoding="utf-8").splitlines():
        line = raw_line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, value = line.split("=", 1)
        value = value.strip()
        if len(value) >= 2 and value[0] == value[-1] and value[0] in "'\"":
            value = value[1:-1]
        values[key.strip()] = value
    return values


def infer_base_url(cors_origins):
    if not cors_origins:
        return None

    parsed = urlsplit(cors_origins.split(",", 1)[0].strip())
    if not parsed.scheme or not parsed.hostname:
        return None

    hostname = parsed.hostname
    if ":" in hostname and not hostname.startswith("["):
        hostname = f"[{hostname}]"
    return urlunsplit((parsed.scheme, f"{hostname}:8080", "", "", ""))


def load_configuration(env_file=DEFAULT_ENV_FILE, environment=None):
    values = parse_env_file(env_file)
    values.update(dict(os.environ if environment is None else environment))

    admin_email = values.get("WAMS_ADMIN_EMAIL") or values.get(
        "InitialAdmin__Email", "admin@example.com"
    )
    email_domain = values.get("WAMS_EMAIL_DOMAIN")
    if not email_domain and "@" in admin_email:
        email_domain = admin_email.rsplit("@", 1)[1]

    return {
        "base_url": (
            values.get("WAMS_BASE_URL")
            or infer_base_url(values.get("CORS__Origins"))
            or "http://localhost:8080"
        ).rstrip("/"),
        "admin_email": admin_email,
        "admin_password": values.get(
            "WAMS_ADMIN_PASSWORD", values.get("InitialAdmin__Password", "")
        ),
        "company_id": int(values.get("WAMS_COMPANY_ID", "1")),
        "email_domain": email_domain or "gerbangcahayautama.com",
        "default_password": values.get("WAMS_DEFAULT_PASSWORD") or None,
    }


def apply_configuration(config):
    global BASE_URL, ADMIN_EMAIL, ADMIN_PASSWORD, COMPANY_ID, EMAIL_DOMAIN, DEFAULT_PASSWORD
    BASE_URL = config["base_url"]
    ADMIN_EMAIL = config["admin_email"]
    ADMIN_PASSWORD = config["admin_password"]
    COMPANY_ID = config["company_id"]
    EMAIL_DOMAIN = config["email_domain"]
    DEFAULT_PASSWORD = config["default_password"]


apply_configuration(load_configuration())


def slugify_fullname(fullname):
    # drop single-letter initials (e.g. "M RIDWAN NASUTION" -> "ridwannasution"),
    # matches the existing account-naming convention in the client's data.
    words = [w for w in fullname.split() if len(w) > 1]
    slug = "".join(words).lower()
    return re.sub(r"[^a-z0-9]", "", slug)


def call(method, path, token=None, body=None):
    url = f"{BASE_URL}{path}"
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", f"Bearer {token}")
    try:
        with urllib.request.urlopen(req) as resp:
            return resp.status, json.loads(resp.read())
    except urllib.error.HTTPError as e:
        return e.code, json.loads(e.read())


def login():
    status, res = call(
        "POST",
        "/api/v1/auth/login",
        body={"email": ADMIN_EMAIL, "password": ADMIN_PASSWORD, "companyId": COMPANY_ID},
    )
    if status != 200:
        sys.exit(f"login failed: {res}")
    return res["data"]["accessToken"]


def fetch_all(token, path, page_size=100):
    items = []
    page = 1
    while True:
        status, res = call("GET", f"{path}?page={page}&pageSize={page_size}", token=token)
        if status != 200:
            sys.exit(f"GET {path} failed: {res}")
        items.extend(res["data"])
        meta = res.get("meta") or {}
        if page >= meta.get("totalPages", 1):
            return items
        page += 1


def read_users(xlsx_path):
    wb = openpyxl.load_workbook(xlsx_path, data_only=True)
    ws = wb["USER"]
    rows = list(ws.iter_rows(values_only=True))[1:]  # skip header
    for username, real_pic, role_code, locations, *_ in rows:
        if not username or not real_pic or real_pic.strip().upper() == "VACANT":
            continue
        provinces = None if not locations or locations.strip().upper() == "ALL" else [
            p.strip() for p in locations.split(",")
        ]
        yield username.strip(), real_pic.strip(), role_code.strip(), provinces


def main():
    global DEFAULT_PASSWORD
    config = load_configuration()
    apply_configuration(config)

    if not DEFAULT_PASSWORD:
        DEFAULT_PASSWORD = getpass("Temporary password for newly created users: ")
        if not DEFAULT_PASSWORD:
            sys.exit("A temporary password is required")

    xlsx_path = Path(sys.argv[1]).expanduser() if len(sys.argv) > 1 else DEFAULT_XLSX_PATH
    if not xlsx_path.is_file():
        sys.exit(f"Matrix file not found: {xlsx_path}")

    print(f"Using API: {BASE_URL}")
    print(f"Using matrix: {xlsx_path}")
    token = login()

    roles_by_name = {r["name"]: r for r in fetch_all(token, "/api/v1/roles")}
    locations_by_name = {
        loc["name"]: loc["id"]
        for loc in call("GET", "/api/v1/warehouses/locations", token=token)[1]["data"]["locations"]
    }
    users_by_email = {u["email"]: u for u in fetch_all(token, "/api/v1/users")}

    created, role_assigned, skipped, errors = 0, 0, 0, []
    used_slugs = set()

    for username, fullname, role_code, provinces in read_users(xlsx_path):
        slug = slugify_fullname(fullname)
        if slug in used_slugs:
            slug = f"{slug}{sum(1 for s in used_slugs if s.startswith(slug)) + 1}"
        used_slugs.add(slug)
        email = f"{slug}@{EMAIL_DOMAIN}"

        role = roles_by_name.get(role_code)
        if not role:
            errors.append(f"{email}: role '{role_code}' does not exist in this DB, skipped")
            continue

        province_ids = None
        if provinces is not None and not role["globalAccess"]:
            province_ids = []
            for p in provinces:
                loc_id = locations_by_name.get(p)
                if loc_id is None:
                    errors.append(f"{email}: province '{p}' not found in /warehouses/locations")
                    continue
                province_ids.append(loc_id)

        user = users_by_email.get(email)
        if user is None:
            status, res = call(
                "POST",
                "/api/v1/users",
                token=token,
                body={
                    "email": email,
                    "password": DEFAULT_PASSWORD,
                    "fullname": fullname,
                    "employeeId": None,
                    "provinceIds": province_ids,
                },
            )
            if status not in (200, 201):
                errors.append(f"{email}: create failed - {res}")
                continue
            user = res["data"]
            created += 1
            print(f"created  {email:45s} ({fullname})")
        else:
            print(f"exists   {email:45s} ({fullname})")

        has_role = any(r["roleId"] == role["id"] for r in user.get("roles", []))
        if not has_role:
            status, res = call(
                "POST", f"/api/v1/users/{user['id']}/roles/{role['id']}", token=token
            )
            if status not in (200, 201):
                errors.append(f"{email}: role assign failed - {res}")
                continue
            role_assigned += 1
            print(f"  + role {role_code}")
        else:
            skipped += 1

    print(
        f"\n{created} user(s) created, {role_assigned} role(s) assigned, "
        f"{skipped} already correct, {len(errors)} error(s)"
    )
    if errors:
        print("\nErrors:")
        for e in errors:
            print(f"  - {e}")
    print(f"\nTemp password for newly created users: {DEFAULT_PASSWORD}  (have them change it on first login)")


if __name__ == "__main__":
    main()
