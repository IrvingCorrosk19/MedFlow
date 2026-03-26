using MedFlow.Application.Common;

namespace MedFlow.UnitTests;

public class PagedResultTests
{
    [Theory]
    [InlineData(100, 25, 1, 4)]
    [InlineData(100, 25, 2, 4)]
    [InlineData(101, 25, 1, 5)]
    [InlineData(0, 25, 1, 0)]
    [InlineData(25, 25, 1, 1)]
    public void TotalPages_CalculatedCorrectly(int total, int pageSize, int page, int expectedPages)
    {
        var result = new PagedResult<string>([], total, page, pageSize);
        Assert.Equal(expectedPages, result.TotalPages);
    }

    [Fact]
    public void HasPreviousPage_FalseOnFirstPage()
    {
        var result = new PagedResult<string>([], 100, 1, 25);
        Assert.False(result.HasPreviousPage);
    }

    [Fact]
    public void HasPreviousPage_TrueOnSecondPage()
    {
        var result = new PagedResult<string>([], 100, 2, 25);
        Assert.True(result.HasPreviousPage);
    }

    [Fact]
    public void HasNextPage_TrueWhenNotOnLastPage()
    {
        var result = new PagedResult<string>([], 100, 1, 25);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public void HasNextPage_FalseOnLastPage()
    {
        var result = new PagedResult<string>([], 100, 4, 25);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public void EmptyResult_HasNoPages()
    {
        var result = new PagedResult<string>([], 0, 1, 25);
        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }
}
