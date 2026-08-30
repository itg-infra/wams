namespace WAMS.Domain.ValueObjects;

using WAMS.Domain.Constants;
using WAMS.Domain.Exceptions;

public sealed class GpsCoordinate
{
    private static readonly TimeSpan FutureTolerance = TimeSpan.FromMinutes(5);

    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public decimal? Accuracy { get; init; }
    public DateTime RecordedAt { get; init; }

    private GpsCoordinate() { }

    public GpsCoordinate(decimal latitude, decimal longitude, decimal? accuracy, DateTime recordedAt)
    {
        if (latitude < -90 || latitude > 90)
            throw new ValidationException(ErrorMessages.Gps.LatitudeOutOfRange);
        if (longitude < -180 || longitude > 180)
            throw new ValidationException(ErrorMessages.Gps.LongitudeOutOfRange);
        if (recordedAt > DateTime.UtcNow.Add(FutureTolerance))
            throw new ValidationException(ErrorMessages.Gps.RecordedAtInFuture);

        Latitude = latitude;
        Longitude = longitude;
        Accuracy = accuracy;
        RecordedAt = recordedAt;
    }
}
