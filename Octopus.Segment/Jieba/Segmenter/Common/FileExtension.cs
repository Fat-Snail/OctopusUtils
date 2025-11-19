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
        private static string _embeddedRoot = "Jieba.Segmenter";

        public static string ReadEmbeddedAllLine(string path)
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

        public static List<string> ReadEmbeddedAllLines(string path, Encoding encoding)
        {
            var assmbly = typeof(FileExtension).GetTypeInfo().Assembly;
            return ReadEmbeddedAllLines(assmbly, path, encoding);
        }

        public static List<string> ReadEmbeddedAllLines(string path)
        {
            return ReadEmbeddedAllLines(path, Encoding.UTF8);
        }

        public static List<string> ReadAllLines(string path)
        {
            return ReadAllLines(path, Encoding.UTF8);
        }

        public static List<string> ReadAllLines(string path, Encoding encoding)
        {
            var list = new List<string>();
            using (var streamReader = new StreamReader(path, encoding))
            {
                string item;
                while ((item = streamReader.ReadLine()) != null)
                {
                    list.Add(item);
                }
            }
            return list;
        }

        public static List<string> ReadEmbeddedAllLines(Assembly assembly, string path)
        {
            return ReadEmbeddedAllLines(assembly, path, Encoding.UTF8);
        }

        public static List<string> ReadEmbeddedAllLines(Assembly assembly, string path, Encoding encoding)
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
            return new List<string>(lines);
        }
    }
}