using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
//using Microsoft.Extensions.FileProviders;
using System.Reflection;
using System.Text;
using JiebaNet.Segmenter.Common;

namespace JiebaNet.Segmenter
{
    public class WordDictionary
    {
        private static readonly Lazy<WordDictionary> lazy = new Lazy<WordDictionary>(() => new WordDictionary());
        private static readonly String MainDict = ConfigManager.MainDictFile;

        internal IDictionary<String, Int32> Trie = new Dictionary<String, Int32>();

        /// <summary>
        /// total occurrence of all words.
        /// </summary>
        public Double Total { get; set; }

        private WordDictionary()
        {
            LoadDict();

            Debug.WriteLine("{0} words (and their prefixes)", Trie.Count);
            Debug.WriteLine("total freq: {0}", Total);
        }

        public static WordDictionary Instance
        {
            get { return lazy.Value; }
        }

        private void LoadDict()
        {
            try
            {
                // var stopWatch = new Stopwatch();
                // stopWatch.Start();
                // var filePath = ConfigManager.MainDictFile;
                // var provider = new EmbeddedFileProvider(GetType().GetTypeInfo().Assembly);
                // var fileInfo = provider.GetFileInfo(filePath);
                // using (var sr = new StreamReader(fileInfo.CreateReadStream(), Encoding.UTF8))
                // {

                var text = String.Empty;

                // if (File.Exists(MainDict))
                // {
                //     using (var sr = new StreamReader(MainDict, Encoding.UTF8))
                //     {
                //         text = sr.ReadToEnd();
                //     }
                // }
                // else
                // {
                //     text = ResourceHelper.GetResourceInputString(ConfigManager.MainDictFile);
                // }

                text = FileExtension.ReadEmbeddedAllLine(ConfigManager.MainDictFile);

                var stopWatch = new Stopwatch();
                stopWatch.Start();


                var lines = text.Split(new[] { "\r\n", "\n" },
                    StringSplitOptions.None
                );
                foreach (var line in lines)
                {
                    // string line = null;
                    // while ((line = sr.ReadLine()) != null)
                    // {
                    var tokens = line.Split(' ');
                    if (tokens.Length < 2)
                    {
                        Debug.Fail(String.Format("Invalid line: {0}", line));
                        continue;
                    }

                    var word = tokens[0];
                    var freq = Int32.Parse(tokens[1]);

                    Trie[word] = freq;
                    Total += freq;

                    foreach (var ch in Enumerable.Range(0, word.Length))
                    {
                        var wfrag = word.Sub(0, ch + 1);
                        if (!Trie.ContainsKey(wfrag))
                        {
                            Trie[wfrag] = 0;
                        }
                    }
                    //}
                }

                stopWatch.Stop();
                Debug.WriteLine("main dict load finished, time elapsed {0} ms", stopWatch.ElapsedMilliseconds);
            }
            catch (IOException e)
            {
                Debug.Fail(String.Format("{0} load failure, reason: {1}", MainDict, e.Message));
            }
            catch (FormatException fe)
            {
                Debug.Fail(fe.Message);
            }
        }

        public Boolean ContainsWord(String word)
        {
            return Trie.ContainsKey(word) && Trie[word] > 0;
        }

        public Int32 GetFreqOrDefault(String key)
        {
            if (ContainsWord(key))
                return Trie[key];
            else
                return 1;
        }

        public void AddWord(String word, Int32 freq, String tag = null)
        {
            if (ContainsWord(word))
            {
                Total -= Trie[word];
            }

            Trie[word] = freq;
            Total += freq;
            for (var i = 0; i < word.Length; i++)
            {
                var wfrag = word.Substring(0, i + 1);
                if (!Trie.ContainsKey(wfrag))
                {
                    Trie[wfrag] = 0;
                }
            }
        }

        public void DeleteWord(String word)
        {
            AddWord(word, 0);
        }

        internal Int32 SuggestFreq(String word, IEnumerable<String> segments)
        {
            Double freq = 1;
            foreach (var seg in segments)
            {
                freq *= GetFreqOrDefault(seg) / Total;
            }

            return Math.Max((Int32)(freq * Total) + 1, GetFreqOrDefault(word));
        }
    }
}