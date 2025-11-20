//using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace JiebaNet.Segmenter.Common
{
    public static class FileExtension
    {
        private static String _embeddedRoot = "Jieba.Segmenter";

        public static String ReadEmbeddedAllLine(String path)
        {
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }

            path = path.Replace("/", ".");
            path = path.Replace("\\", ".");
            var embeddedPath = $"{_embeddedRoot}.{path}";

            return ResourceHelper.GetResourceInputString(embeddedPath);
            //return ReadEmbeddedAllLine(path, Encoding.UTF8);
        }

        // public static string ReadEmbeddedAllLine(string path,Encoding encoding)
        // {
        //     // var provider = new EmbeddedFileProvider(typeof(FileExtension).GetTypeInfo().Assembly);
        //     // var fileInfo = provider.GetFileInfo(path);
        //     //return ResourceHelper.GetResourceInputString(path);
        //     // using (var sr = new StreamReader(fileInfo.CreateReadStream(), encoding))
        //     // {
        //     //     return sr.ReadToEnd();
        //     // }
        // }

        public static List<String> ReadEmbeddedAllLines(String path, Encoding encoding)
        {
            var assmbly = typeof(FileExtension).GetTypeInfo().Assembly;
            return ReadEmbeddedAllLines(assmbly, path, encoding);
        }

        public static List<String> ReadEmbeddedAllLines(String path)
        {
            return ReadEmbeddedAllLines(path, Encoding.UTF8);
        }

        public static List<String> ReadAllLines(String path)
        {
            return ReadAllLines(path, Encoding.UTF8);
        }

        public static List<String> ReadAllLines(String path, Encoding encoding)
        {
            var list = new List<String>();
            using (var streamReader = new StreamReader(path, encoding))
            {
                String item;
                while ((item = streamReader.ReadLine()) != null)
                {
                    list.Add(item);
                }
            }
            return list;
        }

        public static List<String> ReadEmbeddedAllLines(Assembly assembly, String path)
        {
            return ReadEmbeddedAllLines(assembly, path, Encoding.UTF8);
        }

        public static List<String> ReadEmbeddedAllLines(Assembly assembly, String path, Encoding encoding)
        {
            // var provider = new EmbeddedFileProvider(assembly);
            // var fileInfo = provider.GetFileInfo(path);
            // List<string> list = new List<string>();
            // using (StreamReader streamReader = new StreamReader(fileInfo.CreateReadStream(), encoding))
            // {
            //     string item;
            //     while ((item = streamReader.ReadLine()) != null)
            //     {
            //         list.Add(item);
            //     }
            // }
            var text = ReadEmbeddedAllLine(path);
            var lines = System.Text.RegularExpressions.Regex.Split(text, @"\n");
            return new List<String>(lines);
        }
    }
}