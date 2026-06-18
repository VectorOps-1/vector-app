using System.Text;
using vector_app_local.Models;

namespace vector_app_local.Services;

public class ChecklistReportPdfService
{
    private const int PageLineLimit = 58;
    private const int MaxLineLength = 106;

    public byte[] BuildDailyReadinessPdf(DailyVehicleReadinessReport report)
    {
        var lines = BuildReportLines(report)
            .SelectMany(line => WrapLine(line, MaxLineLength))
            .ToList();

        if (lines.Count == 0)
        {
            lines.Add("No report data available.");
        }

        return BuildPdf(lines);
    }

    private static IEnumerable<string> BuildReportLines(DailyVehicleReadinessReport report)
    {
        var recordedAt = report.SubmittedAtUtc ?? report.LastSavedAtUtc ?? report.CreatedAtUtc;
        var vehicle = report.Vehicle;
        var performedBy = report.PerformedByUser;
        var template = report.ChecklistTemplate;
        var areaName = vehicle?.CurrentOperationalArea?.Name ?? "Unallocated";

        yield return "AcuityOps Checklist Evidence";
        yield return $"Report ID: {report.Id}";
        yield return $"Client: {CompanyBranding.GetDisplayCompanyName(report.Company)}";
        yield return $"Generated: {DateTime.UtcNow.ToLocalTime():yyyy-MM-dd HH:mm}";
        yield return string.Empty;

        yield return "Submission";
        yield return $"Submitted / recorded: {FormatDateTime(recordedAt)}";
        yield return $"Workflow status: {report.WorkflowStatus}";
        yield return $"Readiness status: {report.ReadinessStatus}";
        yield return $"Critical issues: {report.CriticalIssueCount}";
        yield return $"Warning issues: {report.WarningIssueCount}";
        yield return $"Shift: {Text(report.ShiftName)}";
        yield return $"Shift started: {FormatDateTime(report.ShiftStartedAtUtc)}";
        yield return $"Shift ended: {FormatDateTime(report.ShiftEndsAtUtc)}";
        yield return string.Empty;

        yield return "Completed By";
        yield return $"Name: {performedBy?.FullName ?? "Unknown"}";
        yield return $"Email: {performedBy?.Email ?? "Unknown"}";
        yield return $"Role: {performedBy?.AppRole?.Name ?? "Unknown"}";
        yield return $"Staff ID: {Text(performedBy?.StaffIdentifier)}";
        yield return $"Clinical qualification / scope: {Text(performedBy?.QualificationFunction)}";
        yield return $"Practitioner Number: {Text(performedBy?.PractitionerNumber)}";
        yield return $"Annual licence expiry: {FormatDate(performedBy?.AnnualLicenseExpiryDate)}";
        yield return $"CPD compliance: {Text(performedBy?.CpdComplianceStatus)}";
        yield return $"CPD valid until: {FormatDate(performedBy?.CpdComplianceExpiryDate)}";
        yield return $"Assigned area: {performedBy?.AssignedOperationalArea?.Name ?? "Not set"}";
        yield return string.Empty;

        yield return "Vehicle";
        yield return $"Registration: {Text(report.VehicleRegistrationNumber)}";
        yield return $"Callsign at check: {Text(report.CallsignAtCheck)}";
        yield return $"Vehicle type: {Text(report.VehicleTypeAtCheck)}";
        yield return $"Qualification level: {Text(report.QualificationLevelAtCheck)}";
        yield return $"Area / base: {areaName}";
        yield return $"Unit schematic: {Text(report.SchematicTypeAtCheck)}";
        yield return $"Vehicle next service at check: {FormatDate(report.VehicleNextServiceDateAtCheck)}";
        yield return $"Register current callsign: {Text(vehicle?.Callsign)}";
        yield return $"Register current status: {Text(vehicle?.Status)}";
        yield return string.Empty;

        yield return "Checklist Template";
        yield return $"Template: {template?.Name ?? "Not linked"}";
        yield return $"Template type: {template?.ChecklistType ?? "Not linked"}";
        yield return $"Template vehicle target: {template?.TargetVehicleType ?? "Not linked"}";
        yield return $"Template version at check: {Text(report.ChecklistTemplateVersion)}";
        yield return $"Template status: {template?.Status ?? "Not linked"}";
        yield return string.Empty;

        yield return "Same As Previous Shift";
        yield return $"Vehicle section used: {YesNo(report.VehicleSameAsPreviousShiftUsed || report.SameAsPreviousShiftUsed)}";
        yield return $"Vehicle copied from report: {report.VehicleSameAsPreviousSourceReportId?.ToString() ?? "N/A"}";
        yield return $"Vehicle copied at: {FormatDateTime(report.VehicleSameAsPreviousAppliedAtUtc)}";
        yield return $"Vehicle copied summary: {Text(report.VehicleSameAsPreviousCopiedSummary)}";
        yield return $"Equipment section used: {YesNo(report.EquipmentSameAsPreviousShiftUsed)}";
        yield return $"Equipment copied from report: {report.EquipmentSameAsPreviousSourceReportId?.ToString() ?? "N/A"}";
        yield return $"Equipment copied at: {FormatDateTime(report.EquipmentSameAsPreviousAppliedAtUtc)}";
        yield return $"Equipment copied summary: {Text(report.EquipmentSameAsPreviousCopiedSummary)}";
        yield return string.Empty;

        yield return "Operational Section";
        yield return $"Lights: {Text(report.LightsStatus)}";
        yield return $"Sirens: {Text(report.SirensStatus)}";
        yield return $"Warning lights: {Text(report.WarningLightsStatus)}";
        yield return $"Tyres: {Text(report.TyresStatus)}";
        yield return $"Ops radio connectivity: {Text(report.RadioConnectivityStatus)}";
        yield return $"Operational notes: {Text(report.OperationalNotes)}";
        yield return string.Empty;

        yield return "Damage / Unit Schematic / General Notes";
        yield return $"Damage notes: {Text(report.DamageNotes)}";
        yield return $"Unit schematic notes: {Text(report.SchematicNotes)}";
        yield return $"General notes: {Text(report.GeneralNotes)}";
        yield return string.Empty;

        yield return "Equipment Checks";
        if (report.EquipmentChecks.Count == 0)
        {
            yield return "No equipment rows were saved against this checklist.";
            yield break;
        }

        foreach (var check in report.EquipmentChecks.OrderBy(check => check.SortOrder).ThenBy(check => check.Id))
        {
            yield return $"#{check.SortOrder + 1} {Text(check.EquipmentName)}";
            yield return $"  S/N / ID: {Text(check.SerialOrAssetId)}";
            yield return $"  Type / model: {Text(check.EquipmentType)} / {Text(check.Model)}";
            yield return $"  Next service at check: {FormatDate(check.NextServiceDateAtCheck)}";
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
