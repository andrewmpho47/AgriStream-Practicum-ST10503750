# Azure Portal Setup Tracker

Last updated: 2026-11-06

This file tracks portal setup for the AgriStream farm practicum. Never record passwords, SQL keys, Function keys, storage access keys, or secret-bearing connection strings here.

## Azure target

- Subscription: `sub-ter-rcgpbr-rosebankcollege-studrsg-dts-001`
- Subscription ID: Omitted from the public repository.
- Tenant: `ADvTECH Ltd.`
- Resource group: `RSG-RCGPON-ST10503750-SANORTH`
- Region: `southafricanorth`

## Verified portal resources

- [x] Existing institution resources inspected; left unchanged.
- [x] New Storage account `rtp10503750farm26` created: Standard, LRS, South Africa North; secure transfer enabled, TLS 1.2, Blob anonymous access disabled.
- [x] Private `drone-images` container created (student reported completion).
- [x] `SensorReadings` Table Storage table created.
- [x] `irrigation-commands` Storage Queue created.
- [x] SQL logical server `rtp10503750-sql26` and database `AgriStreamDb` created; database is Online, Basic (2 GB), Entra-only authentication, LRS backup. Review estimate was USD 7.15/month; check Cost Management for actual charges.
- [x] Run `database/farm-practicum.sql` in the `AgriStreamDb` Query Editor; verified `FarmUsers`, `Devices`, `FarmWebAppRole`, and `FarmFunctionsRole`.
- [x] Added the system-assigned identity of `st10503750-abc-retail-functions` to `FarmFunctionsRole` in `AgriStreamDb`.
- [ ] Remove the temporary SQL firewall rule for the current client IP when local database testing is complete. Public network access is set to Selected networks; the broad Azure-services exception is disabled.

## Code and app configuration

- [x] Replaced the order-processing Azure Functions with exactly four farm workflow Functions in the local project.
- [x] Replaced the e-commerce MVC dashboard with farm telemetry, test reading, private drone image upload, irrigation queue request, and user registration/login/role management.
- [x] Added the practicum SQL schema with password hashes and least-privilege database roles.
- [x] Updated the MVC and Functions projects to target .NET 10 because the portal warned .NET 8 support ends on 2026-11-10.
- [x] Built both .NET 10 projects after user approval; both succeeded with 0 warnings and 0 errors. Updated transitive `System.Security.Cryptography.Pkcs` to patched 10.0.14; NuGet vulnerability scans report no vulnerable packages.
- [x] Verify the generated Functions metadata contains exactly four Functions and validate the local settings sample JSON.
- [x] Created separate practicum Web App `ST10503750-agristream` in South Africa North, Windows, .NET 10, using existing Shared D1 plan `ASP-RSGRCGPONST10503750SANORTH-90c7`; no additional database or App Insights resource was selected.
- [x] New Function App creation was blocked: Flex Consumption required `Microsoft.App/register/action`; Windows Consumption and a new App Service plan were denied by subscription/management-group SKU policy.
- [x] With the user's approval, repurposed existing `st10503750-abc-retail-functions` (formerly retail functions) rather than the blocked new app. The resource remains named for the retail project and runs on Shared D1; this is not a supported Functions hosting tier and does not provide the practicum's autoscaling evidence.
- [x] Changed the existing Function App runtime to .NET 10 isolated; confirmed .NET v10.0 in Overview. Enabled its system-assigned identity.
- [x] Configured `FarmStorage` as an app setting using the existing storage account connection string, and configured `SqlConnectionString` without a SQL username/password. Values are intentionally not recorded here.
- [x] Added the Function App identity to `FarmFunctionsRole` in `AgriStreamDb`.
- [x] Added `.github/workflows/deploy-functions.yml`, a manual-only GitHub Actions workflow that publishes `RealTimeProcessing.Functions` with .NET 10. Repository secret `AZURE_FUNCTIONAPP_PUBLISH_PROFILE` is used for deployment; do not record its value.
- [x] Initial workflow deployment succeeded and Azure showed the four farm Functions enabled: `GetSensorReadings` (HTTP), `ProcessIrrigationCommand` (Queue), `RecordDroneImageUpload` (Blob), and `RequestIrrigation` (HTTP).
- [x] Initial `GetSensorReadings` test returned HTTP 500 because duplicate `TableClient` registrations caused dependency injection to supply `BlobProcessingLog` instead of `SensorReadings`. The table-client selection fix was built, pushed, redeployed, and verified by a subsequent HTTP 200 response (`[]`).
- [x] Confirmed privately that `FarmStorage` uses HTTPS and the account name is `rtp10503750farm26`; confirmed that account contains `SensorReadings`.
- [x] Configured the MVC App Service settings and system identity, added its SQL role, and deployed it through the manual GitHub Actions workflow.
- [ ] Verify MVC registration/login, dashboard telemetry, drone upload, irrigation queue processing, and SQL updates end to end.
- [ ] Resolve SQL networking: the temporary client-IP firewall rule may not allow App Service/Functions traffic. Do not enable broad Azure-service access unless specifically approved.
- [x] Enabled the MVC system identity and added it to `FarmWebAppRole` in SQL.
- [x] Added `.github/workflows/deploy-webapp.yml`, a manual-only workflow to publish `RealTimeProcessingSystem` to `ST10503750-agristream`. The MVC project builds successfully; the `AZURE_WEBAPP_PUBLISH_PROFILE` repository secret was configured and the deployment workflow succeeded.
- [x] Added an MVC storage connection-string fallback for restricted subscriptions where Storage data-role assignment is unavailable. The managed-identity endpoint path remains preferred; any account key must stay in the private App Service setting.
- [x] Published the MVC and Function projects using the approved GitHub Actions workflows; the Function HTTP telemetry endpoint returned HTTP 200 with an empty JSON array.
- [ ] Verify the remaining end-to-end MVC and event-triggered Function flows.

## Monitoring, load, and scaling evidence

- [ ] Set up/verify Application Insights and Azure Monitor if permitted. Function App invocation diagnostics are currently available through Log stream; Application Insights is not enabled.
- [ ] Run a controlled, approved load test. Record test settings, response times, errors, and actual Azure resource metrics.
- [ ] The MVC App Service uses Shared D1 and the reused Function App is also on Shared D1. Shared does not support the requested CPU/memory autoscale evidence. Subscription policies blocked creation of dynamic/Flex plans and plans outside the allowed Basic/Shared SKU set; do not work around policy. Document the limitation and request the institution's accepted alternative.
- [x] Created the written report with theory responses, code excerpts, and the available deployment, Function, Storage, SQL, and Web App screenshots.
- [ ] Capture remaining evidence for the login/dashboard result, end-to-end Blob/Queue tests, and hosting-policy denials. Review/redact screenshots before sharing them outside the course submission.

## Login diagnostic and current code fix

- The user reported that the login POST returned HTTP 302 with `Location: /Account/Login?ReturnUrl=%2FHome%2FError`. A 302 is a redirect, not by itself proof of a failed login.
- AppLens reported no application crashes or HTTP server errors in its diagnostic window and detected an app stop/restart event. That result does not identify the request's underlying exception.
- `HomeController` is protected by `[Authorize]`, and production exception handling routes to `/Home/Error`. The Error action now has `[AllowAnonymous]` so the exception handler can render its generic error page instead of redirecting to Login. The MVC project builds with zero warnings and errors after this local fix.
- The fix is not yet deployed or verified in Azure. If the dashboard still fails after deployment, inspect the App Service Log stream during one sign-in attempt; do not enable Development mode or share app secrets.
- The App Service SCM Basic Auth Publishing Credentials setting must be turned off after deployments are complete. FTP Basic Auth should remain off.

## Important caveats

- The database estimate is not a guarantee of actual charges; inspect Cost Management.
- The MVC sample-reading button creates synthetic data for testing and must be labeled as such.
- Function keys belong only in server-side app configuration/approved secret storage.
- The reused Function App's `FarmStorage` value is a storage account connection string. Treat it as a secret with broad account access; never place it in source control, screenshots, or the tracker. Prefer managed identity with scoped data roles when the subscription permits role assignment.
- `GetSensorReadings` returned HTTP 200 with an empty JSON array after the table-client fix. Add a sample reading before treating this as evidence of populated telemetry.
- The legacy `RealTimeProcessingSystem/azure/deploy-azure.ps1` is disabled because it targets a different resource group and region.
- The old `database/processed-orders.sql` and order API are not the farm practicum implementation.
- No local changes are automatically published to Azure.
