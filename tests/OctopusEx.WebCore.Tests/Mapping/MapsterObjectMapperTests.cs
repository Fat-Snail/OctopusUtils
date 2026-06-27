namespace OctopusEx.WebCore.Tests.Mapping;

using Mapster;
using OctopusEx.WebCore.DomainCore.Mapping;

public class MapsterObjectMapperTests
{
    private readonly MapsterObjectMapper _mapper = new(new TypeAdapterConfig());

    private record SourceDto(Int32 Id, String Name, Int32 Age);
    private class DestEntity { public Int32 Id { get; set; } public String? Name { get; set; } public Int32 Age { get; set; } }

    [Fact]
    public void Map_StronglyTyped_CopiesProperties()
    {
        var src = new SourceDto(1, "Alice", 30);

        var dest = _mapper.Map<SourceDto, DestEntity>(src);

        dest.Id.Should().Be(1);
        dest.Name.Should().Be("Alice");
        dest.Age.Should().Be(30);
    }

    [Fact]
    public void Map_OntoExistingTarget_UpdatesProperties()
    {
        var src = new SourceDto(2, "Bob", 25);
        var existing = new DestEntity { Id = 99, Name = "Old", Age = 0 };

        _mapper.Map(src, existing);

        existing.Id.Should().Be(2);
        existing.Name.Should().Be("Bob");
        existing.Age.Should().Be(25);
    }

    [Fact]
    public void MapList_TransformsAllItems()
    {
        var src = new[]
        {
            new SourceDto(1, "A", 10),
            new SourceDto(2, "B", 20),
        };

        var result = _mapper.MapList<SourceDto, DestEntity>(src);

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("A");
        result[1].Name.Should().Be("B");
    }

    [Fact]
    public void ProjectTo_ProjectsQueryable()
    {
        var src = new[]
        {
            new SourceDto(1, "A", 10),
            new SourceDto(2, "B", 20),
        }.AsQueryable();

        var projected = _mapper.ProjectTo<DestEntity>(src).ToList();

        projected.Should().HaveCount(2);
        projected[0].Id.Should().Be(1);
    }
}
