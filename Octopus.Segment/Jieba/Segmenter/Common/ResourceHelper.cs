using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


internal class ResourceHelper
{
    /// <summary>
    ///  Current Assembly
    /// </summary>
    private static Assembly asm = null;

    /// <summary>
    /// Current Assembly
    /// </summary>
    private static Assembly Asm
    {
        get
        {
            if (asm == null)
                asm = Assembly.GetExecutingAssembly();
            return asm;
        }
    }

    /// <summary>
    /// resource (mainly file in file system or file in compressed package) as BufferedInputStream
    /// </summary>
    /// <param name="resourceName">resourceName</param>
    /// <returns></returns>
    public static Stream GetResourceInputStream(string resourceName)
    {
        return Asm.GetManifestResourceStream(string.Format("{0}.{1}", Asm.GetName().Name, resourceName));
    }

    public static string GetResourceInputString(string resourceName)
    {
        string result = string.Empty;
        using (StreamReader sr = new StreamReader(GetResourceInputStream(resourceName)))
        {
            result = sr.ReadToEnd();
            sr.Close();
        }
        return result;
    }
}

