namespace FloatingWords.Domain.Ratings;

public sealed record Rating(string Id, string DisplayName, int Weight, string ColorHex);
