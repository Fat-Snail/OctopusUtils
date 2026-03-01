namespace OctopusEx.WebCore.Extensions.EFCoreExts;

using System.Collections;
using Attributes.EFCoreAttrs;

public static class EnumStringExtension
{
    /// <summary>
    /// 添加Enum的string属性
    /// </summary>
    /// <param name="modelBuilder"></param>
    public static void AddEnumStringProperties(this Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder, Microsoft.EntityFrameworkCore.DbContext context)
    {


        var maped = new Hashtable();
        var props = context.GetType().GetProperties().Where(x => x.PropertyType.FullName.StartsWith("Microsoft.EntityFrameworkCore.DbSet")).ToList();
        foreach (var typeProp in props)
        {
            var table = typeProp.PropertyType.GenericTypeArguments[0];

            var keyTableProps = table.GetProperties().Where(x =>
                x.GetCustomAttributes(typeof(EnumStringAttribute), true).Length > 0
            ).ToList();



            foreach (var item in keyTableProps)
            {
                modelBuilder.Entity(table).Property(item.PropertyType, item.Name).HasConversion<string>();
            }


        }
    }




}
