using System.Text.Json;

namespace SKF_Product_Assistant.Services;

public sealed class ProductDataService
{
    private readonly string _dataDirectory;

    public ProductDataService()
    {
        _dataDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "Data");
    }

    public async Task<string?> GetProductAttributeAsync(
        string designation,
        string attribute)
    {
        if (string.IsNullOrWhiteSpace(designation) ||
            string.IsNullOrWhiteSpace(attribute))
        {
            return null;
        }

        var fileName = GetFileName(designation);

        if (fileName is null)
        {
            return null;
        }

        var filePath = Path.Combine(
            _dataDirectory,
            fileName);

        if (!File.Exists(filePath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(filePath);

        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;

        var sections = new[]
        {
            "dimensions",
            "properties",
            "performance",
            "logistics",
            "specifications"
        };

        foreach (var sectionName in sections)
        {
            if (!root.TryGetProperty(
                    sectionName,
                    out var section))
            {
                continue;
            }

            if (section.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var item in section.EnumerateArray())
            {
                if (!item.TryGetProperty(
                        "name",
                        out var nameElement))
                {
                    continue;
                }

                var name = nameElement.GetString();

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (!name.Equals(
                        attribute.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!item.TryGetProperty(
                        "value",
                        out var valueElement))
                {
                    return null;
                }

                var value = valueElement.ToString();

                if (item.TryGetProperty(
                        "unit",
                        out var unitElement))
                {
                    var unit = unitElement.GetString();

                    if (!string.IsNullOrWhiteSpace(unit))
                    {
                        return $"{value} {unit}";
                    }
                }

                return value;
            }
        }

        return null;
    }

    private static string? GetFileName(string designation)
    {
        var normalizedDesignation = designation.Trim();

        if (normalizedDesignation.Equals(
                "6205 N",
                StringComparison.OrdinalIgnoreCase))
        {
            return "6205 N.json";
        }

        if (normalizedDesignation.Equals(
                "6205",
                StringComparison.OrdinalIgnoreCase))
        {
            return "6205.json";
        }

        return null;
    }
}
