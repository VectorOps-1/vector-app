using vector_app_local.Models;

namespace vector_app_local.Services;

public static class ChecklistEvidenceSnapshotFactory
{
    public static ChecklistEvidenceSnapshot Create(
        DailyVehicleReadinessReport report,
        Company company,
        AppUser submitter,
        OperationalArea? submitterArea,
        Vehicle vehicle,
        OperationalArea? vehicleArea,
        ChecklistTemplate template,
        IReadOnlyList<EvidenceSectionSnapshot> sections,
        IReadOnlyList<EvidenceEquipmentSnapshot> equipment,
        IReadOnlyList<EvidenceIssueReference> issueReferences)
    {
        return new ChecklistEvidenceSnapshot
        {
            CapturedAtUtc = report.SubmittedAtUtc ?? report.LastSavedAtUtc ?? report.CreatedAtUtc,
            Tenant = new EvidenceTenantSnapshot
            {
                CompanyId = report.CompanyId,
                DisplayName = CompanyBranding.GetDisplayCompanyName(company)
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
                UserId = submitter.Id,
                FullName = submitter.FullName,
                Email = submitter.Email,
                Role = submitter.AppRole?.Name ?? string.Empty,
                StaffIdentifier = submitter.StaffIdentifier,
                QualificationFunction = submitter.QualificationFunction,
                PractitionerNumber = submitter.PractitionerNumber,
                AnnualLicenseExpiryDate = submitter.AnnualLicenseExpiryDate,
                CpdComplianceStatus = submitter.CpdComplianceStatus,
                CpdComplianceExpiryDate = submitter.CpdComplianceExpiryDate,
                AssignedOperationalAreaId = submitter.AssignedOperationalAreaId,
                AssignedOperationalAreaName = submitterArea?.Name
            },
            Vehicle = new EvidenceVehicleSnapshot
            {
                VehicleId = vehicle.Id,
                RegistrationNumber = report.VehicleRegistrationNumber,
                Callsign = report.CallsignAtCheck,
                VehicleType = report.VehicleTypeAtCheck,
                VehicleFunction = vehicle.VehicleFunction,
                VehicleSubtype = vehicle.VehicleSubtype,
                QualificationLevel = report.QualificationLevelAtCheck,
                OperationalAreaId = vehicle.CurrentOperationalAreaId,
                OperationalAreaName = vehicleArea?.Name,
                NextServiceDate = report.VehicleNextServiceDateAtCheck
            },
            Template = new EvidenceTemplateSnapshot
            {
                TemplateId = template.Id,
                Name = ChecklistDisplayService.TemplateName(template.Name),
                Version = report.ChecklistTemplateVersion ?? template.Version,
                ChecklistType = template.ChecklistType,
                TargetVehicleType = template.TargetVehicleType,
                PublishScopeSummary = template.PublishScopeSummary
            },
            Sections = sections.ToList(),
            Equipment = equipment.ToList(),
            IssueReferences = issueReferences.ToList(),
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
