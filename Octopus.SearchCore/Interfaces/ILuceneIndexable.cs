#if Guid
using System;
#endif

using Lucene.Net.Documents;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Serialization;

namespace Octopus.SearchCore.Interfaces
{
    /// <summary>
    /// 需要被索引的实体基类
    /// </summary>
    public interface ILuceneIndexable
    {
        /// <summary>
        /// 主键id
        /// </summary>
        [LuceneIndex(Name = "Id", Store = Field.Store.YES), Key]

        int Id { get; set; }
        
#if Long
        long Id { get; set; }
#endif
#if String
        string Id { get; set; }
#endif
#if Guid
        Guid Id { get; set; }
#endif

        /// <summary>
        /// 索引id
        /// </summary>
        [LuceneIndex(Name = "IndexId", Store = Field.Store.YES)]
        [XmlIgnore, NotMapped]
        internal string IndexId { get; set; }

        /// <summary>
        /// 转换成Lucene文档
        /// </summary>
        /// <returns></returns>
        Document ToDocument();
    }
}
