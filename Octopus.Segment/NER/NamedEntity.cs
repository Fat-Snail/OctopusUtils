namespace Octopus.Segment.NER;

/// <summary>命名实体类型</summary>
public enum EntityType
{
    /// <summary>人名</summary>
    Person,
    /// <summary>地名</summary>
    Place,
    /// <summary>机构名</summary>
    Organization,
    /// <summary>时间</summary>
    Time,
}

/// <summary>命名实体识别结果</summary>
public sealed record NamedEntity(EntityType Type, String Text, Int32 StartIndex, Int32 EndIndex);
