namespace ChopChop.Api.Contracts;

public record SubmitScoreRequest(string Category, long Score);

public record ScoreEntryDto(int Rank, string DisplayName, long Score, DateTime CreatedUtc);
