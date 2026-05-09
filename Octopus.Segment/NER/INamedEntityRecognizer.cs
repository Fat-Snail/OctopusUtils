namespace Octopus.Segment.NER;

/// <summary>命名实体识别器抽象</summary>
public interface INamedEntityRecognizer
{
    /// <summary>识别文本中的命名实体。返回按出现顺序排列的实体列表。</summary>
    IReadOnlyList<NamedEntity> Recognize(String text);
}
