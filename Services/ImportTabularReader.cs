using System.Globalization;
using System.Text;
using ExcelDataReader;
using vector_app_local.Models;

namespace vector_app_local.Services;

public sealed record ImportSourceColumn(int Index, string Heading, IReadOnlyList<string> Samples);
public sealed record ImportSourceRow(int SourceRowNumber, IReadOnlyDictionary<int, string?> Values);
public sealed record ImportTabularData(
    string Worksheet,
    int HeaderRowNumber,
    IReadOnlyList<ImportSourceColumn> Columns,
    IReadOnlyList<ImportSourceRow> Rows);

public interface IImportTabularReader
{
    Task<ImportTabularData> ReadAsync(
        AssetFile sourceFile,
        string? worksheet,
        int headerRowNumber,
        CancellationToken cancellationToken = default);
}

public sealed class ImportTabularReader : IImportTabularReader
{
    private const int MaximumSamplesPerColumn = 3;
    private readonly IFileStorageService _storage;

    static ImportTabularReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public ImportTabularReader(IFileStorageService storage)
    {
        _storage = storage;
    }

    public async Task<ImportTabularData> ReadAsync(
        AssetFile sourceFile,
        string? worksheet,
        int headerRowNumber,
        CancellationToken cancellationToken = default)
    {
        if (headerRowNumber < 1)
        {
            throw new ImportSourceValidationException("Header row must be 1 or greater.");
        }

        var extension = Path.GetExtension(sourceFile.OriginalFileName).ToLowerInvariant();
        await using var stream = await _storage.OpenReadAsync(sourceFile.StoragePath, cancellationToken);
        using var reader = extension == ".csv"
            ? ExcelReaderFactory.CreateCsvReader(stream, new ExcelReaderConfiguration
            {
                AutodetectSeparators = [',', ';', '\t', '|'],
                FallbackEncoding = Encoding.UTF8,
                LeaveOpen = false
            })
            : ExcelReaderFactory.CreateReader(stream, new ExcelReaderConfiguration
            {
                FallbackEncoding = Encoding.GetEncoding(1252),
                LeaveOpen = false
            });

        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentName = string.IsNullOrWhiteSpace(reader.Name) ? "Sheet 1" : reader.Name;
            if (!string.IsNullOrWhiteSpace(worksheet)
                && !string.Equals(currentName, worksheet, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var rawRows = new List<object?[]>();
            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var values = new object?[reader.FieldCount];
                for (var index = 0; index < reader.FieldCount; index++)
                {
                    values[index] = reader.GetValue(index);
                }
                rawRows.Add(values);
            }

            if (headerRowNumber > rawRows.Count)
            {
                throw new ImportSourceValidationException($"Header row {headerRowNumber} is outside worksheet '{currentName}'.");
            }

            var header = rawRows[headerRowNumber - 1];
            var columns = new List<ImportSourceColumn>();
            var usedHeadings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < header.Length; index++)
            {
                var heading = FormatCell(header[index]);
                if (string.IsNullOrWhiteSpace(heading) || !usedHeadings.Add(heading))
                {
                    heading = $"Column {ToColumnName(index)}";
                    usedHeadings.Add(heading);
                }

                var samples = rawRows
                    .Skip(headerRowNumber)
                    .Select(row => index < row.Length ? FormatCell(row[index]) : null)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(MaximumSamplesPerColumn)
                    .Cast<string>()
                    .ToList();
                columns.Add(new ImportSourceColumn(index, heading, samples));
            }

            var rows = rawRows
                .Skip(headerRowNumber)
                .Select((row, offset) => new ImportSourceRow(
                    headerRowNumber + offset + 1,
                    columns.ToDictionary(
                        column => column.Index,
                        column => column.Index < row.Length ? FormatCell(row[column.Index]) : null)))
                .Where(row => row.Values.Values.Any(value => !string.IsNullOrWhiteSpace(value)))
                .ToList();

            return new ImportTabularData(currentName, headerRowNumber, columns, rows);
        }
        while (reader.NextResult());

        throw new ImportSourceValidationException($"Worksheet '{worksheet}' was not found in the source file.");
    }

    private static string? FormatCell(object? value)
    {
        return value switch
        {
            null or DBNull => null,
            DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
            double number => number.ToString("R", CultureInfo.InvariantCulture),
            float number => number.ToString("R", CultureInfo.InvariantCulture),
            decimal number => number.ToString(CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim()
        };
    }

    private static string ToColumnName(int zeroBasedIndex)
    {
        var value = zeroBasedIndex + 1;
        var result = string.Empty;
        while (value > 0)
        {
            value--;
            result = (char)('A' + value % 26) + result;
            value /= 26;
        }
        return result;
    }
}
