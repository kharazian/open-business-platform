#!/usr/bin/env bash
set -euo pipefail

API_BASE_URL="${API_BASE_URL:-http://localhost:5080}"
OBP_EMAIL="${OBP_EMAIL:-admin.demo@company.test}"
OBP_PASSWORD="${OBP_PASSWORD:-DemoUser!2026}"
FORM_ID="${FORM_ID:-10000000-0000-0000-0000-000000000001}"
COOKIE_JAR="$(mktemp)"

cleanup() {
  rm -f "$COOKIE_JAR"
}
trap cleanup EXIT

request() {
  local method="$1"
  local url="$2"
  local body="${3:-}"
  local output
  local status

  if [[ -n "$body" ]]; then
    output="$(curl -sS -w '\n%{http_code}' -b "$COOKIE_JAR" -c "$COOKIE_JAR" -X "$method" -H "Content-Type: application/json" -d "$body" "$url")"
  else
    output="$(curl -sS -w '\n%{http_code}' -b "$COOKIE_JAR" -c "$COOKIE_JAR" -X "$method" "$url")"
  fi

  status="$(printf '%s' "$output" | tail -n 1)"
  RESPONSE_BODY="$(printf '%s' "$output" | sed '$d')"

  if [[ "$status" -lt 200 || "$status" -ge 300 ]]; then
    printf 'Request failed: %s %s -> %s\n%s\n' "$method" "$url" "$status" "$RESPONSE_BODY" >&2
    exit 1
  fi
}

extract_json_string() {
  local key="$1"
  printf '%s' "$RESPONSE_BODY" | sed -n "s/.*\"$key\":\"\\([^\"]*\\)\".*/\\1/p" | head -n 1
}

printf 'Checking health...\n'
request GET "$API_BASE_URL/health"

printf 'Logging in as %s...\n' "$OBP_EMAIL"
request POST "$API_BASE_URL/api/auth/login" "{\"email\":\"$OBP_EMAIL\",\"password\":\"$OBP_PASSWORD\"}"

printf 'Creating integration API key...\n'
request POST "$API_BASE_URL/api/integrations/api-keys" '{"name":"V8 smoke key","integrationKey":"v8-smoke","scopes":["integrations.authenticate","integrations.records.read","integrations.records.create","integrations.webhooks.receive"],"isActive":true}'
RAW_KEY="$(extract_json_string rawKey)"

if [[ -z "$RAW_KEY" ]]; then
  printf 'Could not extract rawKey from API key response.\n%s\n' "$RESPONSE_BODY" >&2
  exit 1
fi

printf 'Listing records through integration API...\n'
curl -sS -f -H "Authorization: Bearer $RAW_KEY" "$API_BASE_URL/api/integration/v1/forms/$FORM_ID/records?page=1&pageSize=2" >/dev/null

printf 'Creating record through integration API...\n'
curl -sS -f -H "Authorization: Bearer $RAW_KEY" -H "Content-Type: application/json" \
  -d '{"values":{"first_name":"Smoke","last_name":"Tester","email":"smoke.tester@example.test","phone":"555-0177","department":"Operations","start_date":"2026-06-10","employment_type":"Contractor","notes":"Created by scripts/v8-smoke.sh."}}' \
  "$API_BASE_URL/api/integration/v1/forms/$FORM_ID/records" >/dev/null

printf 'Creating CSV import job...\n'
request POST "$API_BASE_URL/api/integrations/imports" "{\"formId\":\"$FORM_ID\",\"integrationKey\":\"v8-smoke-import\",\"fileName\":\"smoke.csv\",\"csvContent\":\"first_name,last_name,email,phone,department,start_date,employment_type,notes\\nSmoke,Import,smoke.import@example.test,555-0178,Operations,2026-06-10,Contractor,Imported by scripts/v8-smoke.sh.\",\"mapping\":{\"fieldMappings\":[{\"csvHeader\":\"first_name\",\"targetFieldId\":\"first_name\"},{\"csvHeader\":\"last_name\",\"targetFieldId\":\"last_name\"},{\"csvHeader\":\"email\",\"targetFieldId\":\"email\"},{\"csvHeader\":\"phone\",\"targetFieldId\":\"phone\"},{\"csvHeader\":\"department\",\"targetFieldId\":\"department\"},{\"csvHeader\":\"start_date\",\"targetFieldId\":\"start_date\"},{\"csvHeader\":\"employment_type\",\"targetFieldId\":\"employment_type\"},{\"csvHeader\":\"notes\",\"targetFieldId\":\"notes\"}]}}"

printf 'Creating export job...\n'
request POST "$API_BASE_URL/api/integrations/exports" "{\"sourceType\":\"form_records\",\"format\":\"json\",\"integrationKey\":\"v8-smoke-export\",\"formId\":\"$FORM_ID\"}"

printf 'Listing integration logs...\n'
request GET "$API_BASE_URL/api/integrations/logs"

printf 'V8 smoke passed.\n'
