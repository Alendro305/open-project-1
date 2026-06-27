namespace ChopChop.Api.Contracts;

public record ProfileDto(
    string UserId,
    string DisplayName,
    int Level,
    long Experience,
    int Coins,
    long TotalPlayTimeSeconds,
    DateTime UpdatedUtc);

public record UpdateProfileRequest(
    string? DisplayName,
    int? Level,
    long? Experience,
    int? Coins,
    long? TotalPlayTimeSeconds);

public record SaveDataDto(string? SaveDataJson, DateTime UpdatedUtc);

public record UploadSaveRequest(string SaveDataJson);
