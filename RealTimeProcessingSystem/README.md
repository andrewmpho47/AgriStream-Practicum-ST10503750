# AgriStream IoT practicum

This solution implements the farm-monitoring practicum with an ASP.NET Core MVC dashboard, Azure SQL user/device data, Azure Blob/Table/Queue Storage, and **exactly four Azure Functions**.

## Four-function pipeline

1. `RecordDroneImageUpload`: Blob trigger on `drone-images/{name}`. Logs the image name, size, and processing status to the `BlobProcessingLog` Table Storage table.
2. `GetSensorReadings`: HTTP GET `GET /api/telemetry`. Returns the latest 100 entities from the `SensorReadings` table.
3. `RequestIrrigation`: HTTP POST `POST /api/irrigation`. Validates a `deviceId` and `Start`/`Stop` command, then queues it in `irrigation-commands`.
4. `ProcessIrrigationCommand`: Queue trigger on `irrigation-commands`. Updates or registers the device's operational state in `dbo.Devices`.

The MVC dashboard reads telemetry through Function 2, uploads private drone images to Blob Storage, and sends irrigation actions through Function 3. “Add one sample reading” creates clearly labeled synthetic telemetry in Table Storage; it is for practicum testing, not live field sensor data. Sensor entities use `PartitionKey` `farm` and a row key beginning with a 19-digit descending UTC-tick value, so the GET Function can retrieve the newest records without scanning the full table.

## SQL schema

Run [`../database/farm-practicum.sql`](../database/farm-practicum.sql) once against `AgriStreamDb` using an authorized Microsoft Entra administrator. It creates:

- `dbo.FarmUsers` with PBKDF2 password hashes and Farmer/Agronomist/Admin roles.
- `dbo.Devices` with the device's current operational status.
- Least-privilege database roles `FarmWebAppRole` and `FarmFunctionsRole`.

New self-registrations are Farmers. To use the MVC role-management page, an authorized database administrator can promote the first approved account to Admin:

```sql
UPDATE dbo.FarmUsers
SET SystemRole = N'Admin'
WHERE Username = N'<approved-username>';
```

Confirm exactly one row was updated. Do not store plaintext passwords or promote an account without authorization.

## Required Azure resources and portal-created data

- App Service for this MVC project.
- One approved Function App that can host all four Functions.
- Storage account with Blob, Table, and Queue services; private `drone-images` container; `SensorReadings` table; and `irrigation-commands` queue. The blob trigger creates `BlobProcessingLog` on first invocation.
- Azure SQL Database `AgriStreamDb` and logical server.
- Application Insights/Monitor for runtime evidence.

The deployed storage account name and SQL server/database values are recorded in the root [`../AZURE-PORTAL-TRACKER.md`](../AZURE-PORTAL-TRACKER.md). The old orders-processing SQL script is intentionally not part of this farm practicum repository.

## Authentication and configuration

Both apps use `DefaultAzureCredential` only in development and `ManagedIdentityCredential` in Azure. Do not put keys or passwords in source control or browser JavaScript.

### Function App application settings

Configure these on the Function App. Keep `AzureWebJobsStorage` set to the Function host's existing storage account. The preferred configuration uses a system-assigned identity and identity-based service endpoints (the double underscore maps to `:` in .NET configuration):

```text
FarmStorage__blobServiceUri=https://rtp10503750farm26.blob.core.windows.net/
FarmStorage__queueServiceUri=https://rtp10503750farm26.queue.core.windows.net/
FarmStorage__tableServiceUri=https://rtp10503750farm26.table.core.windows.net/
SqlConnectionString=Server=tcp:rtp10503750-sql26.database.windows.net,1433;Database=AgriStreamDb;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

Give the Function App identity the required Blob/Queue/Table data roles and create its contained SQL user in `AgriStreamDb` as an Entra administrator:

```sql
CREATE USER [<function-app-name>] FROM EXTERNAL PROVIDER;
ALTER ROLE FarmFunctionsRole ADD MEMBER [<function-app-name>];
```

Replace the placeholder with the Function App's system-assigned identity name. Review the Function App's host-storage configuration before changing or publishing it.

If role assignment is unavailable in a restricted student subscription, the current implementation also accepts a `FarmStorage` storage connection string instead of the service URI settings above. This is a less-privileged-control (account-key) fallback with broad storage-account access: store it only in the Function App's private app settings, never in source control or evidence. The deployed student Function App currently uses this fallback because storage role assignment was disabled. The host setting `AzureWebJobsStorage` is separate and must not be overwritten.

### GitHub Actions deployment

The repository workflow at [`../.github/workflows/deploy-functions.yml`](../.github/workflows/deploy-functions.yml) is manually triggered (`workflow_dispatch`) and publishes the Functions project folder using .NET 10. Add the Azure Function App publish profile as repository Actions secret `AZURE_FUNCTIONAPP_PUBLISH_PROFILE`; never commit or share the profile. Start a deployment from GitHub **Actions** only after reviewing the workflow and target app. This workflow currently targets the user's explicitly repurposed existing `st10503750-abc-retail-functions` resource because Azure policy blocked creation of a new Function App plan.

### MVC App Service application settings

Configure these on the approved App Service. The Function key is secret: store it only in the App Service's server-side application settings (or an approved Key Vault reference), never in this file, page markup, or JavaScript.

```text
FarmStorage__blobServiceUri=https://rtp10503750farm26.blob.core.windows.net/
FarmStorage__tableServiceUri=https://rtp10503750farm26.table.core.windows.net/
FarmStorage__container=drone-images
FarmFunctions__BaseUrl=https://<approved-function-app>.azurewebsites.net
FarmFunctions__Key=<set-the-function-key-in-app-service-settings>
SqlConnectionString=Server=tcp:rtp10503750-sql26.database.windows.net,1433;Database=AgriStreamDb;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

Give the App Service identity `Storage Blob Data Contributor` and `Storage Table Data Contributor` on the storage account, then create its contained SQL user in `AgriStreamDb`:

```sql
CREATE USER [<app-service-name>] FROM EXTERNAL PROVIDER;
ALTER ROLE FarmWebAppRole ADD MEMBER [<app-service-name>];
```

Replace the placeholder with the App Service's system-assigned identity name.

SQL networking must allow the approved App Service and Function App to reach the database. Do not enable a broad firewall exception without institution approval; ask the institution for its permitted network pattern. Local development similarly requires an approved, temporary client-IP firewall rule.

## Local development

1. Install .NET 10 SDK, Azure Functions Core Tools v4, and Azurite. The web and Functions projects target .NET 10; select a .NET 10 runtime in Azure if the hosting plan offers it. Do not deploy them using a .NET 8-only runtime.
2. Copy `../RealTimeProcessing.Functions/local.settings.sample.json` to `../RealTimeProcessing.Functions/local.settings.json` (never commit the local file). The sample points `FarmStorage__*` to the provisioned storage account and uses Azurite only for the Functions host's `AzureWebJobsStorage`. Sign in with `az login`; your developer identity must have the required storage data roles and SQL permissions.
3. Start Azurite and, in one terminal, run:

   ```powershell
   Set-Location ..\RealTimeProcessing.Functions
   func start
   ```

4. Configure web-project local settings with `dotnet user-secrets` from the `RealTimeProcessingSystem` folder:

   ```powershell
   dotnet user-secrets set "FarmStorage:blobServiceUri" "https://rtp10503750farm26.blob.core.windows.net/"
   dotnet user-secrets set "FarmStorage:tableServiceUri" "https://rtp10503750farm26.table.core.windows.net/"
   dotnet user-secrets set "FarmStorage:container" "drone-images"
   dotnet user-secrets set "FarmFunctions:BaseUrl" "http://localhost:7071"
   dotnet user-secrets set "FarmFunctions:Key" "<local-function-key>"
   dotnet user-secrets set "SqlConnectionString" "Server=tcp:rtp10503750-sql26.database.windows.net,1433;Database=AgriStreamDb;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
   ```

   The Function key stays server-side in user secrets and is sent using the `x-functions-key` header. The SQL string contains no password.
5. In another terminal, run:

   ```powershell
   dotnet run --project RealTimeProcessingSystem.csproj
   ```

6. Open the HTTPS URL printed by ASP.NET. Register a Farmer and sign in. Promote an approved account to Admin through SQL before using User management.

Both processes access the same Azure Blob/Table/Queue account using your signed-in developer identity. Ensure SQL networking permits your temporary development IP through an institution-approved rule; remove it when finished. Do not use a storage account key or expose the Function key in browser code.

## Verification sequence

1. In SQL Query Editor, run `farm-practicum.sql`; verify `FarmUsers`, `Devices`, and the two database roles exist.
2. Register a Farmer from the MVC site; verify only a password hash (not a plaintext password) is stored in `dbo.FarmUsers`.
3. Add a sample sensor reading in the dashboard and verify it appears through `GET /api/telemetry`.
4. Upload a test image; verify a blob appears in the private `drone-images` container and a status row appears in `BlobProcessingLog`.
5. Queue a Start or Stop irrigation command; verify it is consumed and `dbo.Devices` is updated.
6. Test invalid HTTP payloads and capture sanitized screenshots. Do not include function keys, access keys, passwords, or connection strings in evidence.
7. Use an approved load-test method and Azure Monitor/Application Insights to record the actual response and resource metrics. Simulator/sample data is not production telemetry.

The existing MVC App Service and the repurposed Function App run on Shared D1. Shared does not support the requested CPU/memory-based autoscale evidence, and Azure subscription/management-group policies blocked creation of a new Function App on the attempted plans. Do not circumvent these policies or upgrade shared institutional plans without approval. Document the restriction and request an accepted alternative for the rubric.
