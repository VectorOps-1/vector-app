using System.Globalization;
using System.Text;
using vector_app_local.Models;

namespace vector_app_local.Services;

public class ChecklistReportPdfService
{
    private const double PageWidth = 612;
    private const double PageHeight = 792;
    private const double LeftMargin = 42;
    private const double RightMargin = 42;
    private const double ContentWidth = PageWidth - LeftMargin - RightMargin;
    private const double ContentTop = 704;
    private const double ContentBottom = 54;
    private const int BodyWrapLength = 92;

    public byte[] BuildDailyReadinessPdf(DailyVehicleReadinessReport report)
    {
        return BuildDailyReadinessPdf(report, ChecklistEvidenceSnapshotResolver.Resolve(report));
    }

    public byte[] BuildDailyReadinessPdf(
        DailyVehicleReadinessReport report,
        ChecklistEvidenceSnapshot evidence)
    {
        var blocks = BuildEvidenceBlocks(evidence).ToList();
        var pages = Paginate(blocks);
        return BuildPdf(pages, evidence);
    }

    private static IEnumerable<PdfBlock> BuildEvidenceBlocks(ChecklistEvidenceSnapshot evidence)
    {
        var submission = evidence.Submission;
        var recordedAt = submission.SubmittedAtUtc ?? submission.LastSavedAtUtc ?? evidence.CapturedAtUtc;

        yield return PdfBlock.Section("Submission summary");
        yield return PdfBlock.Field("Submitted / recorded", FormatDateTime(recordedAt));
        yield return PdfBlock.Field("Workflow status", Text(submission.WorkflowStatus));
        yield return PdfBlock.Field("Readiness status", Text(submission.ReadinessStatus));
        yield return PdfBlock.Field("Critical / warning issues", $"{submission.CriticalIssueCount} / {submission.WarningIssueCount}");
        yield return PdfBlock.Field("Shift", Text(submission.ShiftName));
        yield return PdfBlock.Field("Shift window", $"{FormatDateTime(submission.ShiftStartedAtUtc)} to {FormatDateTime(submission.ShiftEndsAtUtc)}");

        yield return PdfBlock.Section("Completed by");
        yield return PdfBlock.Field("Name", Text(evidence.Submitter.FullName));
        yield return PdfBlock.Field("Role", Text(evidence.Submitter.Role));
        yield return PdfBlock.Field("Email", Text(evidence.Submitter.Email));
        yield return PdfBlock.Field("Staff ID", Text(evidence.Submitter.StaffIdentifier));
        yield return PdfBlock.Field("Clinical qualification / scope", Text(evidence.Submitter.QualificationFunction));
        yield return PdfBlock.Field("Practitioner number", Text(evidence.Submitter.PractitionerNumber));
        yield return PdfBlock.Field("Annual licence expiry", FormatDate(evidence.Submitter.AnnualLicenseExpiryDate));
        yield return PdfBlock.Field("CPD compliance", Text(evidence.Submitter.CpdComplianceStatus));
        yield return PdfBlock.Field("CPD valid until", FormatDate(evidence.Submitter.CpdComplianceExpiryDate));
        yield return PdfBlock.Field("Assigned area", Text(evidence.Submitter.AssignedOperationalAreaName));

        yield return PdfBlock.Section("Vehicle");
        yield return PdfBlock.Field("Registration", Text(evidence.Vehicle.RegistrationNumber));
        yield return PdfBlock.Field("Callsign at check", Text(evidence.Vehicle.Callsign));
        yield return PdfBlock.Field("Vehicle type", Text(evidence.Vehicle.VehicleType));
        yield return PdfBlock.Field("Vehicle function", Text(evidence.Vehicle.VehicleFunction));
        yield return PdfBlock.Field("Vehicle subtype", Text(evidence.Vehicle.VehicleSubtype));
        yield return PdfBlock.Field("Qualification level", Text(evidence.Vehicle.QualificationLevel));
        yield return PdfBlock.Field("Area / base", Text(evidence.Vehicle.OperationalAreaName));
        yield return PdfBlock.Field("Vehicle next service at check", FormatDate(evidence.Vehicle.NextServiceDate));

        yield return PdfBlock.Section("Checklist template");
        yield return PdfBlock.Field("Template", Text(evidence.Template.Name));
        yield return PdfBlock.Field("Template type", Text(evidence.Template.ChecklistType));
        yield return PdfBlock.Field("Template vehicle target", Text(evidence.Template.TargetVehicleType));
        yield return PdfBlock.Field("Template version at check", Text(evidence.Template.Version));
        yield return PdfBlock.Field("Published scope at check", Text(evidence.Template.PublishScopeSummary));

        yield return PdfBlock.Section("Same as previous shift");
        yield return PdfBlock.Field("Vehicle section used", YesNo(submission.VehicleSameAsPreviousShiftUsed));
        yield return PdfBlock.Field("Vehicle copied from report", submission.VehicleSameAsPreviousSourceReportId?.ToString(CultureInfo.InvariantCulture) ?? "N/A");
        yield return PdfBlock.Field("Equipment section used", YesNo(submission.EquipmentSameAsPreviousShiftUsed));
        yield return PdfBlock.Field("Equipment copied from report", submission.EquipmentSameAsPreviousSourceReportId?.ToString(CultureInfo.InvariantCulture) ?? "N/A");

        yield return PdfBlock.Section("Checklist evidence");
        if (evidence.Sections.Count == 0)
        {
            yield return PdfBlock.Field("Checklist responses", Text(evidence.Notes.ChecklistResponseSummary));
        }
        else
        {
            foreach (var section in evidence.Sections.OrderBy(section => section.DisplayOrder))
            {
                yield return PdfBlock.Subsection(section.Name);
                foreach (var item in section.Items)
                {
                    if (item.Fields.Count == 0)
                    {
                        yield return PdfBlock.Field(item.Prompt, "N/A");
                        continue;
                    }

                    foreach (var field in item.Fields)
                    {
                        var label = string.IsNullOrWhiteSpace(field.Heading)
                            ? item.Prompt
                            : $"{item.Prompt} - {field.Heading}";
                        yield return PdfBlock.Field(label, Text(field.Value));
                    }
                }
            }
        }

        yield return PdfBlock.Section("Notes and unit schematic");
        yield return PdfBlock.Field("Operational notes", Text(evidence.Notes.OperationalNotes));
        yield return PdfBlock.Field("Damage notes", Text(evidence.Notes.DamageNotes));
        yield return PdfBlock.Field("Schematic", Text(evidence.Schematic.DisplayName));
        yield return PdfBlock.Field("Schematic notes", Text(evidence.Notes.SchematicNotes));
        yield return PdfBlock.Field("Schematic mark summary", Text(evidence.Schematic.MarkSummary));
        yield return PdfBlock.Field("Schematic mark data", Text(evidence.Schematic.MarkData));
        yield return PdfBlock.Field("General notes", Text(evidence.Notes.GeneralNotes));

        yield return PdfBlock.Section("Linked evidence alerts");
        if (evidence.IssueReferences.Count == 0)
        {
            yield return PdfBlock.Note("No linked issue or alert references were recorded with this submission.");
        }
        else
        {
            foreach (var issue in evidence.IssueReferences)
            {
                yield return PdfBlock.Field(
                    $"{Text(issue.ReferenceType)} #{issue.ReferenceId}",
                    $"{Text(issue.Label)} | {Text(issue.Severity)} | {Text(issue.Status)}");
            }
        }

        yield return PdfBlock.Section("Equipment checks");
        if (evidence.Equipment.Count == 0)
        {
            yield return PdfBlock.Note("No equipment rows were saved against this checklist.");
            yield break;
        }

        foreach (var check in evidence.Equipment.OrderBy(check => check.SortOrder))
        {
            yield return PdfBlock.Subsection($"#{check.SortOrder + 1} {Text(check.Name)}");
            yield return PdfBlock.Field("S/N / ID", Text(check.SerialOrAssetId));
            yield return PdfBlock.Field("Type / model", $"{Text(check.EquipmentType)} / {Text(check.Model)}");
            yield return PdfBlock.Field("Next service at check", FormatDate(check.NextServiceDate));
            yield return PdfBlock.Field("Present", Text(check.PresentStatus));
            yield return PdfBlock.Field("Damage", Text(check.DamageStatus));
            yield return PdfBlock.Field("Battery", Text(check.BatteryStatus));
            yield return PdfBlock.Field("Operational", YesNo(check.IsOperational));
            yield return PdfBlock.Field("Readiness impact", Text(check.ReadinessImpact));
            yield return PdfBlock.Field("Same as previous shift", YesNo(check.SameAsPreviousShiftUsed));
            yield return PdfBlock.Field("Issue notes", Text(check.IssueNotes));
            yield return PdfBlock.Field("General notes", Text(check.Notes));
        }
    }

    private static List<PdfPage> Paginate(IReadOnlyList<PdfBlock> blocks)
    {
        var pages = new List<PdfPage>();
        var page = new PdfPage();
        var y = ContentTop;

        foreach (var block in blocks)
        {
            var prepared = PrepareBlock(block);
            if (y - prepared.Height < ContentBottom)
            {
                pages.Add(page);
                page = new PdfPage();
                y = ContentTop;
            }

            page.Items.Add(new PositionedBlock(prepared, y));
            y -= prepared.Height;
        }

        if (page.Items.Count > 0 || pages.Count == 0)
        {
            pages.Add(page);
        }

        return pages;
    }

    private static PreparedBlock PrepareBlock(PdfBlock block)
    {
        return block.Kind switch
        {
            PdfBlockKind.Section => new PreparedBlock(block.Kind, Wrap(block.Value, 68), [], 31),
            PdfBlockKind.Subsection => new PreparedBlock(block.Kind, Wrap(block.Value, 78), [], 23),
            PdfBlockKind.Note => PrepareBodyBlock(block, 0),
            _ => PrepareBodyBlock(block, 152)
        };
    }

    private static PreparedBlock PrepareBodyBlock(PdfBlock block, int labelWidth)
    {
        var labelLines = labelWidth == 0 ? new List<string>() : Wrap(block.Label, 28);
        var valueLines = Wrap(block.Value, labelWidth == 0 ? BodyWrapLength : 64);
        var lineCount = Math.Max(1, Math.Max(labelLines.Count, valueLines.Count));
        return new PreparedBlock(block.Kind, labelLines, valueLines, 8 + (lineCount * 12));
    }

    private static byte[] BuildPdf(IReadOnlyList<PdfPage> pages, ChecklistEvidenceSnapshot evidence)
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            string.Empty,
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>"
        };

        var pageObjectNumbers = new List<int>();
        for (var index = 0; index < pages.Count; index++)
        {
            var pageObjectNumber = objects.Count + 1;
            var contentObjectNumber = objects.Count + 2;
            pageObjectNumbers.Add(pageObjectNumber);

            var content = BuildPageContent(pages[index], evidence, index + 1, pages.Count);
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth:0} {PageHeight:0}] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentObjectNumber} 0 R >>");
            objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream");
        }

        objects[1] = $"<< /Type /Pages /Kids [{string.Join(" ", pageObjectNumbers.Select(number => $"{number} 0 R"))}] /Count {pageObjectNumbers.Count} >>";

        var output = new StringBuilder();
        var offsets = new List<int> { 0 };
        output.Append("%PDF-1.4\n%AcU1\n");

        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(output.ToString()));
            output.Append(index + 1).Append(" 0 obj\n");
            output.Append(objects[index]).Append('\n');
            output.Append("endobj\n");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(output.ToString());
        output.Append("xref\n");
        output.Append("0 ").Append(objects.Count + 1).Append('\n');
        output.Append("0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            output.Append(offset.ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
        }

        output.Append("trailer\n");
        output.Append("<< /Size ").Append(objects.Count + 1).Append(" /Root 1 0 R >>\n");
        output.Append("startxref\n");
        output.Append(xrefOffset).Append('\n');
        output.Append("%%EOF");

        return Encoding.ASCII.GetBytes(output.ToString());
    }

    private static string BuildPageContent(
        PdfPage page,
        ChecklistEvidenceSnapshot evidence,
        int pageNumber,
        int pageCount)
    {
        var builder = new StringBuilder();
        DrawRectangle(builder, 0, 738, PageWidth, 54, "0.055 0.231 0.310");
        DrawText(builder, "F2", 17, 42, 765, "AcuityOps Checklist Evidence", "1 1 1");
        DrawText(builder, "F1", 9, 42, 749, $"{Text(evidence.Tenant.DisplayName)}  |  Report #{evidence.Submission.ReportId}", "0.84 0.93 0.96");

        foreach (var item in page.Items)
        {
            DrawBlock(builder, item);
        }

        DrawLine(builder, LeftMargin, 38, PageWidth - RightMargin, 38, "0.78 0.82 0.85");
        DrawText(builder, "F1", 8, LeftMargin, 24, $"{Text(evidence.EvidenceStatus)}  |  Contract v{evidence.ContractVersion}", "0.30 0.36 0.41");
        DrawText(builder, "F1", 8, PageWidth - RightMargin - 54, 24, $"Page {pageNumber} of {pageCount}", "0.30 0.36 0.41");
        return builder.ToString();
    }

    private static void DrawBlock(StringBuilder builder, PositionedBlock positioned)
    {
        var block = positioned.Block;
        var top = positioned.Top;

        if (block.Kind == PdfBlockKind.Section)
        {
            DrawRectangle(builder, LeftMargin, top - 22, ContentWidth, 24, "0.88 0.95 0.97");
            DrawText(builder, "F2", 11, LeftMargin + 10, top - 14, block.LabelLines.FirstOrDefault() ?? string.Empty, "0.055 0.310 0.400");
            return;
        }

        if (block.Kind == PdfBlockKind.Subsection)
        {
            DrawLine(builder, LeftMargin, top - 18, PageWidth - RightMargin, top - 18, "0.82 0.86 0.88");
            DrawText(builder, "F2", 9.5, LeftMargin + 4, top - 13, block.LabelLines.FirstOrDefault() ?? string.Empty, "0.10 0.16 0.21");
            return;
        }

        var lineCount = Math.Max(1, Math.Max(block.LabelLines.Count, block.ValueLines.Count));
        var baseline = top - 11;
        if (block.Kind == PdfBlockKind.Note)
        {
            for (var index = 0; index < block.ValueLines.Count; index++)
            {
                DrawText(builder, "F1", 9, LeftMargin + 6, baseline - (index * 12), block.ValueLines[index], "0.27 0.33 0.38");
            }
        }
        else
        {
            for (var index = 0; index < block.LabelLines.Count; index++)
            {
                DrawText(builder, "F2", 8.5, LeftMargin + 6, baseline - (index * 12), block.LabelLines[index], "0.30 0.36 0.41");
            }

            for (var index = 0; index < block.ValueLines.Count; index++)
            {
                DrawText(builder, "F1", 9.2, LeftMargin + 158, baseline - (index * 12), block.ValueLines[index], "0.06 0.15 0.20");
            }
        }

        DrawLine(builder, LeftMargin, top - block.Height + 2, PageWidth - RightMargin, top - block.Height + 2, "0.91 0.93 0.94");
    }

    private static void DrawText(
        StringBuilder builder,
        string font,
        double size,
        double x,
        double y,
        string value,
        string color)
    {
        builder.Append("BT\n")
            .Append(color).Append(" rg\n/").Append(font).Append(' ')
            .Append(size.ToString("0.0", CultureInfo.InvariantCulture)).Append(" Tf\n")
            .Append(x.ToString("0.0", CultureInfo.InvariantCulture)).Append(' ')
            .Append(y.ToString("0.0", CultureInfo.InvariantCulture)).Append(" Td\n(")
            .Append(EscapePdfText(Normalize(value))).Append(") Tj\nET\n");
    }

    private static void DrawRectangle(
        StringBuilder builder,
        double x,
        double y,
        double width,
        double height,
        string color)
    {
        builder.Append("q\n").Append(color).Append(" rg\n")
            .Append(x.ToString("0.0", CultureInfo.InvariantCulture)).Append(' ')
            .Append(y.ToString("0.0", CultureInfo.InvariantCulture)).Append(' ')
            .Append(width.ToString("0.0", CultureInfo.InvariantCulture)).Append(' ')
            .Append(height.ToString("0.0", CultureInfo.InvariantCulture)).Append(" re f\nQ\n");
    }

    private static void DrawLine(
        StringBuilder builder,
        double x1,
        double y1,
        double x2,
        double y2,
        string color)
    {
        builder.Append("q\n").Append(color).Append(" RG\n0.6 w\n")
            .Append(x1.ToString("0.0", CultureInfo.InvariantCulture)).Append(' ')
            .Append(y1.ToString("0.0", CultureInfo.InvariantCulture)).Append(" m\n")
            .Append(x2.ToString("0.0", CultureInfo.InvariantCulture)).Append(' ')
            .Append(y2.ToString("0.0", CultureInfo.InvariantCulture)).Append(" l S\nQ\n");
    }

    private static List<string> Wrap(string? value, int maxLength)
    {
        var remaining = Normalize(Text(value));
        var lines = new List<string>();

        while (remaining.Length > maxLength)
        {
            var splitAt = remaining.LastIndexOf(' ', maxLength);
            if (splitAt <= 0)
            {
                splitAt = maxLength;
            }

            lines.Add(remaining[..splitAt].TrimEnd());
            remaining = remaining[splitAt..].TrimStart();
        }

        lines.Add(remaining);
        return lines;
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
            else if (character is >= ' ' and <= '~')
            {
                builder.Append(character);
            }
            else
            {
                builder.Append(character switch
                {
                    '\u2013' or '\u2014' => '-',
                    '\u2018' or '\u2019' => '\'',
                    '\u201c' or '\u201d' => '"',
                    _ => '?'
                });
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
        return value.HasValue ? value.Value.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "N/A";
    }

    private static string FormatDateTime(DateTime? value)
    {
        return value.HasValue ? value.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) : "N/A";
    }

    private enum PdfBlockKind
    {
        Section,
        Subsection,
        Field,
        Note
    }

    private sealed record PdfBlock(PdfBlockKind Kind, string Label, string Value)
    {
        public static PdfBlock Section(string label) => new(PdfBlockKind.Section, string.Empty, label);
        public static PdfBlock Subsection(string label) => new(PdfBlockKind.Subsection, string.Empty, label);
        public static PdfBlock Field(string label, string value) => new(PdfBlockKind.Field, label, value);
        public static PdfBlock Note(string value) => new(PdfBlockKind.Note, string.Empty, value);
    }

    private sealed record PreparedBlock(
        PdfBlockKind Kind,
        IReadOnlyList<string> LabelLines,
        IReadOnlyList<string> ValueLines,
        double Height);

    private sealed record PositionedBlock(PreparedBlock Block, double Top);

    private sealed class PdfPage
    {
        public List<PositionedBlock> Items { get; } = [];
    }
}
