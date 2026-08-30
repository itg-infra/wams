namespace WAMS.Application.Export;

public record ExportColumnDefinition<T>(
    string Header,
    Func<T, object?> Accessor,
    double Width = 20,
    string? Format = null
);
