using JiebaNet.Segmenter.Common;

namespace JiebaNet.Segmenter.Spelling
{
    public interface ISpellChecker
    {
        IEnumerable<String> Suggests(String word);
    }

    public class SpellChecker : ISpellChecker
    {
        internal static readonly WordDictionary WordDict = WordDictionary.Instance;

        internal readonly Trie WordTrie;
        internal readonly Dictionary<Char, HashSet<Char>> FirstChars;

        public SpellChecker()
        {
            var wordDict = WordDictionary.Instance;
            WordTrie = new Trie();
            FirstChars = new Dictionary<Char, HashSet<Char>>();

            foreach (var wd in wordDict.Trie)
            {
                if (wd.Value > 0)
                {
                    WordTrie.Insert(wd.Key, wd.Value);

                    if (wd.Key.Length >= 2)
                    {
                        var second = wd.Key[1];
                        var first = wd.Key[0];
                        if (!FirstChars.ContainsKey(second))
                        {
                            FirstChars[second] = new HashSet<Char>();
                        }
                        FirstChars[second].Add(first);
                    }
                }
            }
        }

        internal ISet<String> GetEdits1(String word)
        {
            var splits = new List<WordSplit>();
            for (var i = 0; i <= word.Length; i++)
            {
                splits.Add(new WordSplit() { Left = word.Substring(0, i), Right = word.Substring(i) });
            }

            var deletes = splits
                .Where(s => !String.IsNullOrEmpty(s.Right))
                .Select(s => s.Left + s.Right.Substring(1));

            var transposes = splits
                .Where(s => s.Right.Length > 1)
                .Select(s => s.Left + s.Right[1] + s.Right[0] + s.Right.Substring(2));

            var replaces = new HashSet<String>();
            if (word.Length > 1)
            {
                var firsts = FirstChars[word[1]];
                foreach (var first in firsts)
                {
                    if (first != word[0])
                    {
                        replaces.Add(first + word.Substring(1));
                    }
                }

                var node = WordTrie.Root.Children[word[0]];
                for (var i = 1; node.IsNotNull() && node.Children.IsNotEmpty() && i < word.Length; i++)
                {
                    foreach (var c in node.Children.Keys)
                    {
                        replaces.Add(word.Substring(0, i) + c + word.Substring(i + 1));
                    }
                    node = node.Children.GetValueOrDefaultEx(word[i]);
                }
            }

            var inserts = new HashSet<String>();
            if (word.Length > 1)
            {
                if (FirstChars.ContainsKey(word[0]))
                {
                    var firsts = FirstChars[word[0]];
                    foreach (var first in firsts)
                    {
                        inserts.Add(first + word);
                    }
                }

                var node = WordTrie.Root.Children.GetValueOrDefaultEx(word[0]);
                for (var i = 0; node.IsNotNull() && node.Children.IsNotEmpty() && i < word.Length; i++)
                {
                    foreach (var c in node.Children.Keys)
                    {
                        inserts.Add(word.Substring(0, i + 1) + c + word.Substring(i + 1));
                    }

                    if (i < word.Length - 1)
                    {
                        node = node.Children.GetValueOrDefault(word[i + 1]);
                    }
                }
            }

            var result = new HashSet<String>();
            result.UnionWith(deletes);
            result.UnionWith(transposes);
            result.UnionWith(replaces);
            result.UnionWith(inserts);

            return result;
        }

        internal ISet<String> GetKnownEdits2(String word)
        {
            var result = new HashSet<String>();
            foreach (var e1 in GetEdits1(word))
            {
                result.UnionWith(GetEdits1(e1).Where(e => WordDictionary.Instance.ContainsWord(e)));
            }
            return result;
        }

        internal ISet<String> GetKnownWords(IEnumerable<String> words)
        {
            return new HashSet<String>(words.Where(w => WordDictionary.Instance.ContainsWord(w)));
        }

        public IEnumerable<String> Suggests(String word)
        {
            if (WordDict.ContainsWord(word))
            {
                return new[] { word };
            }

            var candicates = GetKnownWords(GetEdits1(word));
            if (candicates.IsNotEmpty())
            {
                return candicates.OrderByDescending(c => WordDict.GetFreqOrDefault(c));
            }

            candicates.UnionWith(GetKnownEdits2(word));
            return candicates.OrderByDescending(c => WordDict.GetFreqOrDefault(c));
        }
    }

    internal class WordSplit
    {
        public String Left { get; set; }
        public String Right { get; set; }
    }
}