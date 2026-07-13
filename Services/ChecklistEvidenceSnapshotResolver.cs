using vector_app_local.Models;

namespace vector_app_local.Services;

public static class ChecklistEvidenceSnapshotResolver
{
    public static ChecklistEvidenceSnapshot Resolve(DailyVehicleReadinessReport report)
    {
        if (report.EvidenceSnapshotVersion >= ChecklistEvidenceSnapshot.CurrentVersion &&
            ChecklistEvidenceSnapshotSerializer.TryDeserialize(report.EvidenceSnapshotJson, out var snapshot) &&
            snapshot is not null)
        {
            return snapshot;
        }

        return CreateLegacySnapshot(report);
    }

    private static ChecklistEvidenceSnapshot CreateLegacySnapshot(DailyVehicleReadinessReport report)
    {
        var vehicle = report.Vehicle;
        var submitter = report.PerformedByUser;
        var template = report.ChecklistTemplate;
        var recordedAt = report.SubmittedAtUtc ?? report.LastSavedAtUtc ?? report.CreatedAtUtc;

        return new ChecklistEvidenceSnapshot
        {
            ContractVersion = 0,
            IsLegacyRecord = true,
            EvidenceStatus = "Historical legacy record - immutable evidence snapshot was not available at submission.",
            CapturedAtUtc = recordedAt,
            Tenant = new EvidenceTenantSnapshot
            {
                CompanyId = report.CompanyId,
                DisplayName = CompanyBranding.GetDisplayCompanyName(report.Company)
            },
            Submission = new EvidenceSubmissionMetadata
            {
                ReportId = report.Id,
                WorkflowStatus = report.WorkflowStatus,
                ReadinessStatus = report.ReadinessStatus,
                CriticalIssueCount = report.CriticalIssueCount,
                WarningIssueCount = report.WarningIssueCount,
                InspectionDateUtc = report.InspectionDateUtc,
                SubmittedAtUtc = report.SubmittedAtUtc,
                LastSavedAtUtc = report.LastSavedAtUtc,
                ShiftName = report.ShiftName,
                ShiftStartedAtUtc = report.ShiftStartedAtUtc,
                ShiftEndsAtUtc = report.ShiftEndsAtUtc,
                VehicleSameAsPreviousShiftUsed = report.VehicleSameAsPreviousShiftUsed || report.SameAsPreviousShiftUsed,
                EquipmentSameAsPreviousShiftUsed = report.EquipmentSameAsPreviousShiftUsed,
                VehicleSameAsPreviousSourceReportId = report.VehicleSameAsPreviousSourceReportId,
                EquipmentSameAsPreviousSourceReportId = report.EquipmentSameAsPreviousSourceReportId
            },
            Submitter = new EvidenceSubmitterSnapshot
            {
                UserId = report.PerformedByUserId,
                FullName = submitter?.FullName ?? "Unknown",
                Email = submitter?.Email ?? string.Empty,
                Role = submitter?.AppRole?.Name ?? string.Empty,
                StaffIdentifier = submitter?.StaffIdentifier,
                QualificationFunction = submitter?.QualificationFunction,
                PractitionerNumber = submitter?.PractitionerNumber,
                AnnualLicenseExpiryDate = submitter?.AnnualLicenseExpiryDate,
                CpdComplianceStatus = submitter?.CpdComplianceStatus,
                CpdComplianceExpiryDate = submitter?.CpdComplianceExpiryDate,
                AssignedOperationalAreaId = submitter?.AssignedOperationalAreaId,
                AssignedOperationalAreaName = submitter?.AssignedOperationalArea?.Name
            },
            Vehicle = new EvidenceVehicleSnapshot
            {
                VehicleId = report.VehicleId,
                RegistrationNumber = report.VehicleRegistrationNumber,
                Callsign = report.CallsignAtCheck,
                VehicleType = report.VehicleTypeAtCheck,
                VehicleFunction = vehicle?.VehicleFunction,
                VehicleSubtype = vehicle?.VehicleSubtype,
                QualificationLevel = report.QualificationLevelAtCheck,
                OperationalAreaId = vehicle?.CurrentOperationalAreaId,
                OperationalAreaName = vehicle?.CurrentOperationalArea?.Name,
                NextServiceDate = report.VehicleNextServiceDateAtCheck
            },
            Template = new EvidenceTemplateSnapshot
            {
                TemplateId = report.ChecklistTemplateId,
                Name = template is null
                    ? (report.ChecklistTemplateId.HasValue ? "Historical snapshot - template unavailable" : "Historical snapshot - no template link")
                    : ChecklistDisplayService.TemplateName(template.Name),
                Version = report.ChecklistTemplateVersion ?? template?.Version ?? "N/A",
                ChecklistType = template?.ChecklistType ?? "Not linked",
                TargetVehicleType = template?.TargetVehicleType ?? "Not linked",
                PublishScopeSummary = template?.PublishScopeSummary
            },
            Equipment = report.EquipmentChecks
                .OrderBy(check => check.SortOrder)
                .ThenBy(check => check.Id)
                .Select(check => new EvidenceEquipmentSnapshot
                {
                    ChecklistItemId = check.ChecklistItemId,
                    Name = check.EquipmentName,
                    EquipmentType = check.EquipmentType,
                    Model = check.Model,
                    SerialOrAssetId = check.SerialOrAssetId,
                    NextServiceDate = check.NextServiceDateAtCheck,
                    PresentStatus = check.PresentStatus,
                    DamageStatus = check.DamageStatus,
                    BatteryStatus = check.BatteryStatus,
                    IsOperational = check.IsOperational,
                    IssueNotes = check.IssueNotes,
                    ReadinessImpact = check.ReadinessImpact,
                    SameAsPreviousShiftUsed = check.SameAsPreviousShiftUsed,
                    Notes = check.Notes,
                    SortOrder = check.SortOrder
                })
                .ToList(),
            Schematic = new EvidenceSchematicSnapshot
            {
                Key = report.SchematicTypeAtCheck,
                DisplayName = report.SchematicTypeAtCheck,
                MarkData = report.SchematicMarkData,
                MarkSummary = report.SchematicNotes
            },
            Notes = new EvidenceNotesSnapshot
            {
                ChecklistResponseSummary = report.OperationalNotes,
                OperationalNotes = report.OperationalNotes,
                DamageNotes = report.DamageNotes,
                SchematicNotes = report.SchematicNotes,
                GeneralNotes = report.GeneralNotes
            }
        };
    }
}
