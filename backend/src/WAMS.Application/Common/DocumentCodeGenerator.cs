namespace WAMS.Application.Common;

using WAMS.Application.Interfaces.Common;

public static class DocumentCodeGenerator
{
    public static async Task<string> NextCodeAsync(
        ICodeCounterRepository counterRepo, string prefix, CancellationToken ct = default)
    {
        var seq = await counterRepo.NextValueAsync(prefix, ct);
        return $"{prefix}{seq:D6}";
    }
}
