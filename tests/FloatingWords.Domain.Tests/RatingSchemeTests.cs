using FloatingWords.Domain.Ratings;

namespace FloatingWords.Domain.Tests;

public sealed class RatingSchemeTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    public void NormalizeUnknownValueReturnsFuzzy(string? ratingId)
        => Assert.Equal(RatingIds.Fuzzy, RatingScheme.Default.Normalize(ratingId).Id);

    [Fact]
    public void NormalizeKnownValueReturnsSameRating()
        => Assert.Equal(RatingIds.Easy, RatingScheme.Default.Normalize(RatingIds.Easy).Id);

    [Fact]
    public void DefaultHasFourPositiveWeights()
    {
        Assert.Equal(4, RatingScheme.Default.All.Count);
        Assert.All(RatingScheme.Default.All, rating => Assert.True(rating.Weight > 0));
    }
}
