// using Lucene.Net.Documents;
//
// namespace Octopus.SearchCore.IndexDemo;
//
// public class News2
// {
//     [Lucene(FieldStore = Field.Store.YES, IsUnique = true, type = LuceneFieldType.Int32)]
//     public int Id { get; set; }
//     [Lucene(FieldStore = Field.Store.YES, IsUnique = false, type = LuceneFieldType.Text)]
//     public string Title { get; set; }
//
//
//     [Lucene(FieldStore = Field.Store.YES, IsUnique = false, type = LuceneFieldType.Text)]
//     public string Content { get; set; }
//     
// }