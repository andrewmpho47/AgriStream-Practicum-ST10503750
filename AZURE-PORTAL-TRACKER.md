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
- [ ] Remove the temporary SQL firewall rule for the current client IP when local database testing is complete. Public network access is set to Selected networks; the broad Azure-services exception is disabled.

## Code and app configuration

- [x] Replaced the order-processing Azure Functions with exactly four farm workflow Functions in the local project.
- [x] Replaced the e-commerce MVC dashboard with farm telemetry, test reading, private drone image upload, irrigation queue request, and user registration/login/role management.
- [x] Added the practicum SQL schema with password hashes and least-privilege database roles.
- [x] Updated the MVC and Functions projects to target .NET 10 because the portal warned .NET 8 support ends on 2026-11-10.
- [x] Built both .NET 10 projects after user approval; both succeeded with 0 warnings and 0 errors. Updated transitive `System.Security.Cryptography.Pkcs` to patched 10.0.14; NuGet vulnerability scans report no vulnerable packages.
- [x] Verify the generated Functions metadata contains exactly four Functions and validate the local settings sample JSON.
- [x] Created separate practicum Web App `ST10503750-agristream` in South Africa North, Windows, .NET 10, using existing Shared D1 plan `ASP-RSGRCGPONST10503750SANORTH-90c7`; no additional database or App Insights resource was selected.
- [ ] Create a separate Function App for the practicum. Do not overwrite `st10503750` or `st10503750-abc-retail-functions`.
- [ ] Set up the new apps' application settings using the non-secret examples in `RealTimeProcessingSystem/README.md`.
- [ ] Confirm institutional approval before publishing to or changing existing `st10503750` or `st10503750-abc-retail-functions`.
- [ ] Enable the approved managed identities and assign only required Blob, Table, Queue, and SQL permissions.
- [ ] Resolve SQL networking with the institution; do not enable broad Azure-service access unless specifically approved.
- [ ] Publish the MVC and Function projects using an approved method and verify the end-to-end flows.

## Monitoring, load, and scaling evidence

- [ ] Set up/verify Application Insights and Azure Monitor for the approved app resources.
- [ ] Run a controlled, approved load test. Record test settings, response times, errors, and actual Azure resource metrics.
- [ ] The selected App Service plan is Shared D1. The practicum requests CPU/memory-based scale-out, which Shared does not support. Do not upgrade an existing school plan without approval; document the portal limitation and ask for an accepted alternative/evidence.
- [ ] Capture sanitized screenshots and complete the written analysis/theory questions.

## Important caveats

- The database estimate is not a guarantee of actual charges; inspect Cost Management.
- The MVC sample-reading button creates synthetic data for testing and must be labeled as such.
- Function keys belong only in server-side app configuration/approved secret storage.
- The legacy `RealTimeProcessingSystem/azure/deploy-azure.ps1` is disabled because it targets a different resource group and region.
- The old `database/processed-orders.sql` and order API are not the farm practicum implementation.
- No local changes are automatically published to Azure.
