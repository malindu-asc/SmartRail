#!/usr/bin/env bash
# DEVELOPMENT ONLY. Prints an access token for a seeded Keycloak user.
#   scripts/get-token.sh controller
#   curl -H "Authorization: Bearer $(scripts/get-token.sh controller)" http://localhost:<api-port>/api/identity/whoami
# Passwords are the development values in infra/keycloak/smartrail-realm.json.
set -euo pipefail

# On Windows "python3" can be a Microsoft Store stub, so pick whichever interpreter really runs.
PYTHON=python3
python3 -c 'pass' >/dev/null 2>&1 || PYTHON=python

user="${1:?usage: get-token.sh <passenger|driver|controller|admin>}"
password="${SMARTRAIL_DEV_PASSWORD:-}"
if [ -z "$password" ]; then
  password="$("$PYTHON" - "$user" <<'PY'
import json, sys
realm = json.load(open("infra/keycloak/smartrail-realm.json", encoding="utf-8"))
for u in realm["users"]:
    if u["username"] == sys.argv[1]:
        print(u["credentials"][0]["value"])
PY
)"
fi

# Aspire serves Keycloak over HTTPS (development certificate); Docker Compose uses plain HTTP.
# Override with KEYCLOAK_URL. -k skips certificate checks, which is acceptable for this dev-only script.
base="${KEYCLOAK_URL:-}"
if [ -z "$base" ]; then
  if curl -fsSk -m 3 -o /dev/null "https://localhost:8080/realms/smartrail"; then
    base="https://localhost:8080"
  else
    base="http://localhost:8080"
  fi
fi

curl -fsSk "$base/realms/smartrail/protocol/openid-connect/token" \
  -d grant_type=password -d client_id=smartrail-dev-token \
  -d username="$user" --data-urlencode password="$password" \
  | "$PYTHON" -c 'import json,sys; print(json.load(sys.stdin)["access_token"])'
