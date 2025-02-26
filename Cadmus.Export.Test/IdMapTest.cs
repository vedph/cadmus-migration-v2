using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Cadmus.Export.Test;

public sealed class IdMapTest
{
    [Fact]
    public void Map_CreatesNewId_WhenNotExists()
    {
        IdMap map = new();

        int id = map.MapSourceId("prefix", "suffix");

        Assert.Equal(1, id);
        Assert.Equal(1, map.Count);
    }

    [Fact]
    public void Map_ReturnsSameId_WhenAlreadyMapped()
    {
        IdMap map = new();
        int id1 = map.MapSourceId("prefix", "suffix");

        int id2 = map.MapSourceId("prefix", "suffix");

        Assert.Equal(id1, id2);
        Assert.Equal(1, map.Count);
    }

    [Fact]
    public void Map_ReturnsUniqueIds_ForDifferentSources()
    {
        IdMap map = new();

        int id1 = map.MapSourceId("prefix1", "suffix1");
        int id2 = map.MapSourceId("prefix1", "suffix2");
        int id3 = map.MapSourceId("prefix2", "suffix1");

        Assert.Equal(1, id1);
        Assert.Equal(2, id2);
        Assert.Equal(3, id3);
        Assert.Equal(3, map.Count);
    }

    [Fact]
    public void GetSourceId_ReturnsNull_WhenIdDoesNotExist()
    {
        IdMap map = new();

        string? source = map.GetSourceId(999);

        Assert.Null(source);
    }

    [Fact]
    public void GetSourceId_ReturnsSourceKey_WhenIdExists()
    {
        IdMap map = new();
        int id = map.MapSourceId("prefix", "suffix");

        string? source = map.GetSourceId(id);

        Assert.Equal("prefix_suffix", source);
    }

    [Fact]
    public void Reset_ClearsMap()
    {
        IdMap map = new();
        map.MapSourceId("prefix", "suffix");
        Assert.Equal(1, map.Count);

        map.Reset();

        Assert.Equal(0, map.Count);
        Assert.Null(map.GetSourceId(1));
    }

    [Fact]
    public void Reset_WithSeed_ResetsCounter()
    {
        IdMap map = new();
        map.MapSourceId("prefix1", "suffix1");

        map.Reset(seed: true);
        int newId = map.MapSourceId("prefix2", "suffix2");

        Assert.Equal(1, newId);
    }

    [Fact]
    public void MapSourceId_ThrowsArgumentNullException_WhenPrefixIsNull()
    {
        IdMap map = new();

        Assert.Throws<ArgumentNullException>(() =>
            map.MapSourceId(null!, "suffix"));
    }

    [Fact]
    public void MapSourceId_ThrowsArgumentNullException_WhenSuffixIsNull()
    {
        IdMap map = new();

        Assert.Throws<ArgumentNullException>(() =>
            map.MapSourceId("prefix", null!));
    }

    [Fact]
    public void ToString_ReturnsExpectedFormat()
    {
        IdMap map = new();
        map.MapSourceId("prefix1", "suffix1");
        map.MapSourceId("prefix2", "suffix2");

        string result = map.ToString();

        Assert.Equal("IdMap: 2", result);
    }

    [Fact]
    public async Task MapSourceId_IsThreadSafe()
    {
        IdMap map = new();
        const int iterationCount = 1000;
        const string prefix = "prefix";

        Task<int>[] tasks = [.. Enumerable.Range(0, iterationCount)
            .Select(i => Task.Run(() => map.MapSourceId(prefix, i.ToString())))];

        await Task.WhenAll(tasks);

        Assert.Equal(iterationCount, map.Count);

        HashSet<int> allIds = new HashSet<int>(tasks.Select(t => t.Result));
        Assert.Equal(iterationCount, allIds.Count);

        // check min/max range is as expected
        Assert.Equal(1, allIds.Min());
        Assert.Equal(iterationCount, allIds.Max());
    }
}
