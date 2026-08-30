namespace WAMS.Application.Interfaces.Files;

public interface IFileMimeDetector
{
    /// <summary>
    /// Detects the MIME type from file magic bytes.
    /// Returns the detected MIME string if it is in the allowed list, null otherwise.
    /// </summary>
    string? Detect(byte[] header, int headerLength);
}
