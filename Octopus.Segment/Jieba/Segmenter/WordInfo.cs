using System;
namespace JiebaNet.Segmenter
{
    public class WordInfo
    {
    public WordInfo(String value, Int32 position)
    {
        this.value = value;
        this.position = position;
    }
    //分词的内容
    public String value { get; set; }
    //分词的初始位置
    public Int32 position { get; set; }
    }
}