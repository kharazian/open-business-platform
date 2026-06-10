# V8 Practical Testing

Use this checklist to test V8 as a real administrator and integration consumer.

## Start The App

Start PostgreSQL and Redis:

```bash
docker compose up -d
```

Apply migrations if this is a fresh or older local database volume:

```bash
dotnet ef database update --project src/api/OpenBusinessPlatform.Api.csproj
```

Start the backend:

```bash
cd src/api
dotnet run
```

Start the frontend:

```bash
cd src/app
npm run dev
```

Open the app at:

```text
http://127.0.0.1:5174
```

Demo admin login:

```text
admin.demo@company.test
DemoUser!2026
```

## Browser Test Path

1. Log in as Demo Admin.
2. Open `/integrations`.
3. Create an API key named `Practical test key`.
4. Select these scopes:
   - `integrations.authenticate`
   - `integrations.records.read`
   - `integrations.records.create`
   - `integrations.webhooks.receive`
5. Copy the raw key from the one-time alert.
6. Refresh `/integrations` and confirm the raw key is gone but the key prefix remains.
7. Rotate the key and confirm a new raw key appears once.
8. Revoke the key and confirm the row changes to `Revoked` and action buttons disable.
9. Open the Logs tab and confirm integration activity appears after using API/webhook/import/export paths below.
10. Filter logs by direction, type, status, source, and since time.
11. Select a log row and inspect sanitized request/response metadata.

## API Smoke Path

These examples use the backend directly at `http://localhost:5080`.

First create a cookie session:

```bash
curl -i -c /tmp/obp.cookies \
  -H "Content-Type: application/json" \
  -d '{"email":"admin.demo@company.test","password":"DemoUser!2026"}' \
  http://localhost:5080/api/auth/login
```

List API keys:

```bash
curl -i -b /tmp/obp.cookies \
  http://localhost:5080/api/integrations/api-keys
```

Create an API key:

```bash
curl -i -b /tmp/obp.cookies \
  -H "Content-Type: application/json" \
  -d '{"name":"Practical curl key","integrationKey":"practical-curl","scopes":["integrations.authenticate","integrations.records.read","integrations.records.create","integrations.webhooks.receive"],"isActive":true}' \
  http://localhost:5080/api/integrations/api-keys
```

Save the `rawKey` from the response in a shell variable:

```bash
export OBP_API_KEY='paste-raw-key-here'
```

List sample employee records through the integration API:

```bash
curl -i \
  -H "Authorization: Bearer $OBP_API_KEY" \
  "http://localhost:5080/api/integration/v1/forms/10000000-0000-0000-0000-000000000001/records?page=1&pageSize=5"
```

Create a sample employee record through the integration API:

```bash
curl -i \
  -H "Authorization: Bearer $OBP_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{"values":{"first_name":"Integration","last_name":"Tester","email":"integration.tester@example.test","phone":"555-0199","department":"Operations","start_date":"2026-06-10","employment_type":"Contractor","notes":"Created through V8 practical testing."}}' \
  http://localhost:5080/api/integration/v1/forms/10000000-0000-0000-0000-000000000001/records
```

List integration logs:

```bash
curl -i -b /tmp/obp.cookies \
  http://localhost:5080/api/integrations/logs
```

Create a CSV import job:

```bash
curl -i -b /tmp/obp.cookies \
  -H "Content-Type: application/json" \
  -d '{"formId":"10000000-0000-0000-0000-000000000001","integrationKey":"practical-import","fileName":"employees.csv","csvContent":"first_name,last_name,email,phone,department,start_date,employment_type,notes\nCSV,Tester,csv.tester@example.test,555-0188,Operations,2026-06-10,Contractor,Imported through V8 practical testing.","mapping":{"fieldMappings":[{"csvHeader":"first_name","targetFieldId":"first_name"},{"csvHeader":"last_name","targetFieldId":"last_name"},{"csvHeader":"email","targetFieldId":"email"},{"csvHeader":"phone","targetFieldId":"phone"},{"csvHeader":"department","targetFieldId":"department"},{"csvHeader":"start_date","targetFieldId":"start_date"},{"csvHeader":"employment_type","targetFieldId":"employment_type"},{"csvHeader":"notes","targetFieldId":"notes"}]}}' \
  http://localhost:5080/api/integrations/imports
```

Create a form-record JSON export job:

```bash
curl -i -b /tmp/obp.cookies \
  -H "Content-Type: application/json" \
  -d '{"sourceType":"form_records","format":"json","integrationKey":"practical-export","formId":"10000000-0000-0000-0000-000000000001"}' \
  http://localhost:5080/api/integrations/exports
```

Refresh `/integrations` and confirm the Logs tab now shows API, import, and export activity.

## Scripted Smoke Path

After the backend is running and migrations are applied, run:

```bash
bash scripts/v8-smoke.sh
```

Optional overrides:

```bash
API_BASE_URL=http://localhost:5080 \
OBP_EMAIL=admin.demo@company.test \
OBP_PASSWORD=DemoUser!2026 \
FORM_ID=10000000-0000-0000-0000-000000000001 \
bash scripts/v8-smoke.sh
```

## Negative Tests Worth Running

- Skip the migration step on an older local volume and confirm V8 endpoints fail; then run the migration step and confirm they recover.
- Use a revoked API key against `/api/integration/v1/forms/{formId}/records`; expect `401`.
- Use an API key without `integrations.records.create` to create a record; expect a forbidden/error response.
- Try creating an API key with no scopes; expect validation failure.
- Try importing CSV without a required field mapping; expect row/job validation details.
- Confirm logs do not contain raw API keys, authorization headers, listener secrets, raw CSV row values, or hidden field values.

## Good Follow-Up Additions To Ask For

- Add server-side log filters and pagination for large integration log tables.
- Add downloadable export artifacts through a permission-protected endpoint.
- Add an explicit integration retry worker for failure classes that are safe to replay.
- Add a practical seed fixture for webhook/import/export demos.
- Add Playwright smoke tests for `/integrations`.
