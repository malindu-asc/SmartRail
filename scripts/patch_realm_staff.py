"""Adds the staff roles, demo staff users, sign-in protection and event settings to the Keycloak realm file.

Safe to run again: it only adds what is missing. Passwords are development values for the local realm only."""
import json
import pathlib

path = pathlib.Path(__file__).resolve().parent.parent / "infra" / "keycloak" / "smartrail-realm.json"
realm = json.loads(path.read_text(encoding="utf-8"))

# ---- roles
have = {r["name"] for r in realm["roles"]["realm"]}
for name, description in [
    ("inspector", "Ticket inspector spot-checking tickets in the staff app"),
    ("station_master", "Station master working on their own station only"),
]:
    if name not in have:
        realm["roles"]["realm"].append({"name": name, "description": description})

# ---- the token carries the stations a station master may act on
station_mapper = {
    "name": "stations",
    "protocol": "openid-connect",
    "protocolMapper": "oidc-usermodel-attribute-mapper",
    "config": {
        "user.attribute": "station_codes",
        "claim.name": "stations",
        "jsonType.label": "String",
        "multivalued": "true",
        "access.token.claim": "true",
        "id.token.claim": "false",
    },
}
for client in realm["clients"]:
    mappers = client.get("protocolMappers")
    if mappers is not None and any(m["name"] == "realm-roles" for m in mappers) and not any(m["name"] == "stations" for m in mappers):
        mappers.append(station_mapper)

# ---- staff users
STAFF_PASSWORD = "Staff#123"


def user(username, first, last, role, uid, enabled=True, attributes=None):
    entry = {
        "username": username,
        "email": f"{username}@smartrail.test",
        "firstName": first,
        "lastName": last,
        "enabled": enabled,
        "emailVerified": True,
        "credentials": [{"type": "password", "value": STAFF_PASSWORD, "temporary": False}],
        "realmRoles": [role],
        "id": uid,
    }
    if attributes:
        entry["attributes"] = attributes
    return entry


staff = [
    user("controller2", "Kamal", "Rajapaksa", "controller", "a1000000-0000-4000-8000-000000000006"),
    user("stationmaster1", "Oshadi", "Wijesinghe", "station_master", "a1000000-0000-4000-8000-000000000007", attributes={"station_codes": ["OAK"]}),
    user("stationmaster2", "Tharindu", "Bandara", "station_master", "a1000000-0000-4000-8000-000000000008", attributes={"station_codes": ["CEN"]}),
    user("inspector", "Ishara", "Inspector", "inspector", "a1000000-0000-4000-8000-000000000009"),
    user("michael", "Michael", "Fernando", "driver", "a1000000-0000-4000-8000-000000000010"),
    user("anil", "Anil", "Silva", "driver", "a1000000-0000-4000-8000-000000000011"),
    user("jayasuriya", "R.", "Jayasuriya", "driver", "a1000000-0000-4000-8000-000000000012"),
    user("gunasekara", "T.", "Gunasekara", "driver", "a1000000-0000-4000-8000-000000000013", attributes={"employment_end": ["2026-10-10"]}),
    user("nperera", "N.", "Perera", "driver", "a1000000-0000-4000-8000-000000000014", enabled=False),
]
existing = {u["username"] for u in realm["users"]}
service_at = next(i for i, u in enumerate(realm["users"]) if u["username"].startswith("service-account"))
for u in staff:
    if u["username"] not in existing:
        realm["users"].insert(service_at, u)
        service_at += 1

# ---- the service account may read what the admin console shows
for u in realm["users"]:
    if u["username"].startswith("service-account"):
        roles = u["clientRoles"]["realm-management"]
        for r in ["view-events", "view-realm", "manage-realm", "view-clients"]:
            if r not in roles:
                roles.append(r)

# ---- sign-in protection and events (read by the Admin console security page and dashboard)
realm.update(
    {
        "bruteForceProtected": True,
        "failureFactor": 5,
        "maxFailureWaitSeconds": 900,
        "waitIncrementSeconds": 900,
        "permanentLockout": False,
        "eventsEnabled": True,
        "eventsExpiration": 604800,
        "enabledEventTypes": ["LOGIN", "LOGIN_ERROR", "LOGOUT", "REGISTER", "UPDATE_PASSWORD", "USER_DISABLED_BY_TEMPORARY_LOCKOUT"],
        "adminEventsEnabled": True,
        "adminEventsDetailsEnabled": False,
    }
)

path.write_text(json.dumps(realm, indent=2) + "\n", encoding="utf-8")
print("realm patched:", len(realm["users"]), "users,", len(realm["roles"]["realm"]), "roles")
