using System.Text;
using vector_app_local.Models;

namespace vector_app_local.Services;

public class ChecklistReportPdfService
{
    private const int PageLineLimit = 58;
    private const int MaxLineLength = 106;

    public byte[] BuildDailyReadinessPdf(DailyVehicleReadinessReport report)
    {
        return BuildDailyReadinessPdf(report, ChecklistEvidenceSnapshotResolver.Resolve(report));
    }

    public byte[] BuildDailyReadinessPdf(
        DailyVehicleReadinessReport report,
        ChecklistEvidenceSnapshot evidence)
    {
        var lines = BuildReportLines(evidence)
            .SelectMany(line => WrapLine(line, MaxLineLength))
            .ToList();

        if (lines.Count == 0)
        {
            lines.Add("No report data available.");
        }

        return BuildPdf(lines);
    }

    private static IEnumerable<string> BuildReportLines(ChecklistEvidenceSnapshot evidence)
    {
        var submission = evidence.Submission;
        var recordedAt = submission.SubmittedAtUtc ?? submission.LastSavedAtUtc ?? evidence.CapturedAtUtc;

        yield return "AcuityOps Checklist Evidence";
        yield return $"Report ID: {submission.ReportId}";
        yield return $"Client: {Text(evidence.Tenant.DisplayName)}";
        yield return $"Generated: {DateTime.UtcNow.ToLocalTime():yyyy-MM-dd HH:mm}";
        yield return $"Evidence contract: {evidence.EvidenceStatus}";
        yield return string.Empty;

        yield return "Submission";
        yield return $"Submitted / recorded: {FormatDateTime(recordedAt)}";
        yield return $"Workflow status: {Text(submission.WorkflowStatus)}";
        yield return $"Readiness status: {Text(submission.ReadinessStatus)}";
        yield return $"Critical issues: {submission.CriticalIssueCount}";
        yield return $"Warning issues: {submission.WarningIssueCount}";
        yield return $"Shift: {Text(submission.ShiftName)}";
        yield return $"Shift started: {FormatDateTime(submission.ShiftStartedAtUtc)}";
        yield return $"Shift ended: {FormatDateTime(submission.ShiftEndsAtUtc)}";
        yield return string.Empty;

        yield return "Completed By";
        yield return $"Name: {Text(evidence.Submitter.FullName)}";
        yield return $"Email: {Text(evidence.Submitter.Email)}";
        yield return $"Role: {Text(evidence.Submitter.Role)}";
        yield return $"Staff ID: {Text(evidence.Submitter.StaffIdentifier)}";
        yield return $"Clinical qualification / scope: {Text(evidence.Submitter.QualificationFunction)}";
        yield return $"Practitioner Number: {Text(evidence.Submitter.PractitionerNumber)}";
        yield return $"Annual licence expiry: {FormatDate(evidence.Submitter.AnnualLicenseExpiryDate)}";
        yield return $"CPD compliance: {Text(evidence.Submitter.CpdComplianceStatus)}";
        yield return $"CPD valid until: {FormatDate(evidence.Submitter.CpdComplianceExpiryDate)}";
        yield return $"Assigned area: {Text(evidence.Submitter.AssignedOperationalAreaName)}";
        yield return string.Empty;

        yield return "Vehicle";
        yield return $"Registration: {Text(evidence.Vehicle.RegistrationNumber)}";
        yield return $"Callsign at check: {Text(evidence.Vehicle.Callsign)}";
        yield return $"Vehicle type: {Text(evidence.Vehicle.VehicleType)}";
        yield return $"Vehicle function: {Text(evidence.Vehicle.VehicleFunction)}";
        yield return $"Vehicle subtype: {Text(evidence.Vehicle.VehicleSubtype)}";
        yield return $"Qualification level: {Text(evidence.Vehicle.QualificationLevel)}";
        yield return $"Area / base: {Text(evidence.Vehicle.OperationalAreaName)}";
        yield return $"Unit schematic: {Text(evidence.Schematic.DisplayName)}";
        yield return $"Vehicle next service at check: {FormatDate(evidence.Vehicle.NextServiceDate)}";
        yield return string.Empty;

        yield return "Checklist Template";
        yield return $"Template: {Text(evidence.Template.Name)}";
        yield return $"Template type: {Text(evidence.Template.ChecklistType)}";
        yield return $"Template vehicle target: {Text(evidence.Template.TargetVehicleType)}";
        yield return $"Template version at check: {Text(evidence.Template.Version)}";
        yield return $"Published scope at check: {Text(evidence.Template.PublishScopeSummary)}";
        yield return string.Empty;

        yield return "Same As Previous Shift";
        yield return $"Vehicle section used: {YesNo(submission.VehicleSameAsPreviousShiftUsed)}";
        yield return $"Vehicle copied from report: {submission.VehicleSameAsPreviousSourceReportId?.ToString() ?? "N/A"}";
        yield return $"Equipment section used: {YesNo(submission.EquipmentSameAsPreviousShiftUsed)}";
        yield return $"Equipment copied from report: {submission.EquipmentSameAsPreviousSourceReportId?.ToString() ?? "N/A"}";
        yield return string.Empty;

        yield return "Checklist Evidence";
        if (evidence.Sections.Count == 0)
        {
            yield return $"Checklist responses: {Text(evidence.Notes.ChecklistResponseSummary)}";
        }
        else
        {
            foreach (var section in evidence.Sections.OrderBy(section => section.DisplayOrder))
            {
                yield return $"[{section.Name}]";
                foreach (var item in section.Items)
                {
                    foreach (var field in item.Fields)
                    {
                        yield return $"{item.Prompt} - {field.Heading}: {Text(field.Value)}";
                    }
                }
            }
        }
        yield return string.Empty;

        yield return "Damage / Unit Schematic / General Notes";
        yield return $"Damage notes: {Text(evidence.Notes.DamageNotes)}";
        yield return $"Unit schematic notes: {Text(evidence.Schematic.MarkSummary)}";
        yield return $"Unit schematic mark data: {Text(evidence.Schematic.MarkData)}";
        yield return $"General notes: {Text(evidence.Notes.GeneralNotes)}";
        yield return string.Empty;

        yield return "Linked Evidence Alerts";
        if (evidence.IssueReferences.Count == 0)
        {
            yield return "No linked issue or alert references were recorded with this submission.";
        }
        else
        {
            foreach (var issue in evidence.IssueReferences)
            {
                yield return $"{issue.ReferenceType} #{issue.ReferenceId}: {issue.Label} | {Text(issue.Severity)} | {Text(issue.Status)}";
            }
        }
        yield return string.Empty;

        yield return "Equipment Checks";
        if (evidence.Equipment.Count == 0)
        {
            yield return "No equipment rows were saved against this checklist.";
            yield break;
        }

        foreach (var check in evidence.Equipment.OrderBy(check => check.SortOrder))
        {
            yield return $"#{check.SortOrder + 1} {Text(check.Name)}";
            yield return $"  S/N / ID: {Text(check.SerialOrAssetId)}";
            yield return $"  Type / model: {Text(check.EquipmentType)} / {Text(check.Model)}";
            yield return $"  Next service at check: {FormatDate(check.NextServiceDate)}";
            yield return $"  Present: {Text(check.PresentStatus)}";
            yield return $"  Damage: {Text(check.DamageStatus)}";
            yield return $"  Battery: {Text(check.BatteryStatus)}";
            yield return $"  Operational: {YesNo(check.IsOperational)}";
            yield return $"  Readiness impact: {Text(check.ReadinessImpact)}";
            yield return $"  Same as previous shift: {YesNo(check.SameAsPreviousShiftUsed)}";
            yield return $"  Issue notes: {Text(check.IssueNotes)}";
            yield return $"  General notes: {Text(check.Notes)}";
            yield return string.Empty;
        }
    }

    private static byte[] BuildPdf(IReadOnlyList<string> lines)
    {
        var pages = lines
            .Chunk(PageLineLimit)
            .Select(chunk => chunk.ToList())
            .ToList();

        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            string.Empty,
            "<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>"
        };

        var pageObjectNumbers = new List<int>();
        foreach (var pageLines in pages)
        {
            var pageObjectNumber = objects.Count + 1;
            var contentObjectNumber = objects.Count + 2;
            pageObjectNumbers.Add(pageObjectNumber);

            var content = BuildPageContent(pageLines);
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentObjectNumber} 0 R >>");
            objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream");
        }

        objects[1] = $"<< /Type /Pages /Kids [{string.Join(" ", pageObjectNumbers.Select(number => $"{number} 0 R"))}] /Count {pageObjectNumbers.Count} >>";

        var output = new StringBuilder();
        var offsets = new List<int> { 0 };
        output.Append("%PDF-1.4\n");

        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(output.ToString()));
            output.Append(i + 1).Append(" 0 obj\n");
            output.Append(objects[i]).Append('\n');
            output.Append("endobj\n");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(output.ToString());
        output.Append("xref\n");
        output.Append("0 ").Append(objects.Count + 1).Append('\n');
        output.Append("0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            output.Append(offset.ToString("D10")).Append(" 00000 n \n");
        }

        output.Append("trailer\n");
        output.Append("<< /Size ").Append(objects.Count + 1).Append(" /Root 1 0 R >>\n");
        output.Append("startxref\n");
        output.Append(xrefOffset).Append('\n');
        output.Append("%%EOF");

        return Encoding.ASCII.GetBytes(output.ToString());
    }

    private static string BuildPageContent(IReadOnlyList<string> pageLines)
    {
        var builder = new StringBuilder();
        builder.Append("BT\n");
        builder.Append("/F1 9 Tf\n");
        builder.Append("50 760 Td\n");
        builder.Append("11 TL\n");

        foreach (var line in pageLines)
        {
            builder.Append('(').Append(EscapePdfText(line)).Append(") Tj\n");
            builder.Append("T*\n");
        }

        builder.Append("ET");
        return builder.ToString();
    }

    private static IEnumerable<string> WrapLine(string? value, int maxLength)
    {
        var line = Normalize(value ?? string.Empty);

        if (line.Length <= maxLength)
        {
            yield return line;
            yield break;
        }

        while (line.Length > maxLength)
        {
            var splitAt = line.LastIndexOf(' ', maxLength);
            if (splitAt <= 0)
            {
                splitAt = maxLength;
            }

            yield return line[..splitAt].TrimEnd();
            line = line[splitAt..].TrimStart();
        }

        if (line.Length > 0)
        {
            yield return line;
        }
    }

    private static string EscapePdfText(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
    }

    private static string Normalize(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (character is '\r' or '\n' or '\t')
            {
                builder.Append(' ');
            }
            else if (character < 32 || character > 126)
            {
                builder.Append('?');
            }
            else
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Trim();
    }

    private static string Text(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "N/A" : value.Trim();
    }

    private static string TemplateName(DailyVehicleReadinessReport report)
    {
        if (report.ChecklistTemplate is not null)
        {
            return ChecklistDisplayService.TemplateName(report.ChecklistTemplate.Name);
        }

        return report.ChecklistTemplateId.HasValue
            ? "Historical snapshot - template unavailable"
            : "Historical snapshot - no template link";
    }

    private static string YesNo(bool value)
    {
        return value ? "Yes" : "No";
    }

    private static string FormatDate(DateTime? value)
    {
        return value.HasValue ? value.Value.ToLocalTime().ToString("yyyy-MM-dd") : "N/A";
    }

    private static string FormatDateTime(DateTime? value)
    {
        return value.HasValue ? value.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm") : "N/A";
    }
}
