using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExcelDataReader;

namespace vector_app_local.Services;

public sealed record ImportWorksheetProfile(string Name, int RowCount, int ColumnCount, int NonEmptyCellCount);

public sealed record ImportSourceProfile(
    int ContractVersion,
    string FileHash,
    string FileType,
    int WorksheetCount,
    int TotalRows,
    int TotalNonEmptyCells,
    IReadOnlyList<ImportWorksheetProfile> Worksheets)
{
    public string ToJson() => JsonSerializer.Serialize(this);

    public static ImportSourceProfile? FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ImportSourceProfile>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

public interface IImportSourceInspector
{
    Task<ImportSourceProfile> InspectAsync(IFormFile file, CancellationToken cancellationToken = default);
}

public sealed class ImportSourceInspector : IImportSourceInspector
{
    public const int ContractVersion = 1;
    public const int MaximumWorksheets = 50;
    public const int MaximumColumns = 250;
    public const int MaximumRows = 50_000;
    public const int MaximumNonEmptyCells = 1_000_000;

    static ImportSourceInspector()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task<ImportSourceProfile> InspectAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".xlsx" or ".xls" or ".csv"))
        {
            throw new ImportSourceValidationException("Accepted import file types are .xlsx, .xls, and .csv.");
        }

        var hash = await ComputeHashAsync(file, cancellationToken);

        try
        {
            await using var source = file.OpenReadStream();
            using var reader = extension == ".csv"
                ? ExcelReaderFactory.CreateCsvReader(source, new ExcelReaderConfiguration
                {
                    AutodetectSeparators = [',', ';', '\t', '|'],
                    FallbackEncoding = Encoding.UTF8,
                    LeaveOpen = false
                })
                : ExcelReaderFactory.CreateReader(source, new ExcelReaderConfiguration
                {
                    FallbackEncoding = Encoding.GetEncoding(1252),
                    LeaveOpen = false
                });

            var worksheets = new List<ImportWorksheetProfile>();
            var totalRows = 0;
            var totalCells = 0;

            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (worksheets.Count >= MaximumWorksheets)
                {
                    throw new ImportSourceValidationException($"The workbook contains more than {MaximumWorksheets} worksheets.");
                }

                var rows = 0;
                var cells = 0;
                var columns = reader.FieldCount;
                if (columns > MaximumColumns)
                {
                    throw new ImportSourceValidationException($"Worksheet '{reader.Name}' contains more than {MaximumColumns} columns.");
                }

                while (reader.Read())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    rows++;
                    totalRows++;
                    if (totalRows > MaximumRows)
                    {
                        throw new ImportSourceValidationException($"The import contains more than {MaximumRows:N0} rows.");
                    }

                    for (var column = 0; column < reader.FieldCount; column++)
                    {
                        var value = reader.GetValue(column);
                        if (value is null || value == DBNull.Value || string.IsNullOrWhiteSpace(Convert.ToString(value)))
                        {
                            continue;
                        }

                        cells++;
                        totalCells++;
                        if (totalCells > MaximumNonEmptyCells)
                        {
                            throw new ImportSourceValidationException($"The import contains more than {MaximumNonEmptyCells:N0} non-empty cells.");
                        }
                    }
                }

                worksheets.Add(new ImportWorksheetProfile(
                    string.IsNullOrWhiteSpace(reader.Name) ? $"Sheet {worksheets.Count + 1}" : reader.Name,
                    rows,
                    columns,
                    cells));
            }
            while (reader.NextResult());

            if (worksheets.Count == 0 || worksheets.All(sheet => sheet.RowCount == 0))
            {
                throw new ImportSourceValidationException("The selected file does not contain any readable rows.");
            }

            return new ImportSourceProfile(
                ContractVersion,
                hash,
                extension.TrimStart('.').ToUpperInvariant(),
                worksheets.Count,
                totalRows,
                totalCells,
                worksheets);
        }
        catch (ImportSourceValidationException)
        {
            throw;
        }
        catch (Exception ex) when (ex is InvalidDataException or NotSupportedException or ArgumentException or DecoderFallbackException)
        {
            throw new ImportSourceValidationException(
                extension == ".xls"
                    ? "The legacy .xls file could not be read safely. Save it as .xlsx and upload it again."
                    : "The selected spreadsheet could not be read safely. Check the file and upload it again.",
                ex);
        }
    }

    private static async Task<string> ComputeHashAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }
}

public sealed class ImportSourceValidationException : InvalidOperationException
{
    public ImportSourceValidationException(string message) : base(message) { }
    public ImportSourceValidationException(string message, Exception innerException) : base(message, innerException) { }
}
