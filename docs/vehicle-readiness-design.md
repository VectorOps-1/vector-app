# Vehicle Readiness Design

## Product Principle

The core Vector workflow is a daily readiness decision for a specific vehicle. The report must combine vehicle checks and the equipment carried on that vehicle. Equipment is not a separate floating checklist; it is part of whether that vehicle can respond.

## Daily Readiness Flow

1. Staff or management user selects a vehicle registration number.
2. Vector loads the vehicle callsign, type, schematic, next service date, and expected equipment.
3. The user completes operational checks such as lights, sirens, warning lights, tyres, and radio connectivity.
4. The user completes schematic/damage checks and notes.
5. The user checks every expected equipment item for that vehicle.
6. Vector saves one daily readiness report and calculates the readiness result.

Managers must be able to complete the same vehicle checks themselves. The report is always recorded against the signed-in user who performed it.

Checks must be resumable during the shift. Starting a check creates a draft readiness report. Section saves update that draft, and the draft remains available until the shift ends or the configured draft expiry is reached, normally 12 hours.

## Equipment On A Vehicle

Each expected item can carry:

- Equipment name
- Optional type or model, such as LP15, Zoll X, or Zoll M
- Serial number or ID
- Next service date
- Present, missing, or out-of-service status
- Damage status and damage notes
- Battery status when relevant
- General notes

Expected equipment depends on the client's company setup. Ambulances, response vehicles, rescue vehicles, and different qualification levels may have different loadouts.

## Setup Model

Senior management defines the operating rules:

- Vehicle register
- Equipment register
- Vehicle types and schematic types
- Equipment loadouts by vehicle, vehicle type, or qualification level
- Checklist sections and fields
- Readiness rules, including hard stops and warnings

Uploaded client checklists and registers should eventually generate editable setup records, not just static uploaded files.

## Product Tiers

Base should support the core readiness loop:

- Vehicle register
- Daily vehicle readiness report
- Basic issue reporting
- Task feedback
- Basic audit trail
- Local file storage

Pro should add deeper operational control:

- Vehicle-specific equipment loadouts
- Custom checklist builder
- Equipment service, battery, and expiry tracking
- Staff files
- Medication and stock registers
- Manager issue pool
- Same-as-previous-shift controls

Premium should add scale and intelligence:

- Readiness analytics
- AI checklist import and mapping
- Advanced exports
- Azure Blob Storage
- Multi-site reporting
- Escalation rules and notifications

## Storage Rule

Base and Pro do not require Azure Blob Storage. File uploads can use local storage while the database stores provider, path, original file name, content type, linked record, uploader, and timestamp. Premium can switch the provider to Azure Blob Storage later without changing the product workflow.

## First Implementation Order

1. Company subscription tier and feature gates.
2. File storage abstraction with local storage implementation.
3. Vehicle register table.
4. Equipment register table.
5. Vehicle equipment loadout table.
6. Daily vehicle readiness report table.
7. Daily vehicle equipment check table.
8. Combined daily vehicle readiness UI.
9. Manager readiness dashboard.

## Implemented Backbone Tables

The current backend model includes:

- `Vehicles`
- `EquipmentItems`
- `VehicleEquipmentAssignments`
- `DailyVehicleReadinessReports`
- `DailyVehicleEquipmentChecks`

Daily readiness reports store snapshots of key vehicle and equipment details at the time of the check. This protects historical records when a vehicle callsign, schematic, service date, or equipment setup changes later.

Same-as-previous-shift support is split into two management-controlled permissions:

- Vehicle inspection carry-forward
- Equipment check carry-forward

Reports record which carry-forward option was used, when it was applied, which previous report it came from, and a summary of the copied data. Equipment rows can also link back to the previous equipment check row they were copied from.
