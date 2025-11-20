using System;
using System.Collections.Generic;
using System.Linq;

namespace JiebaNet.Segmenter.Common
{
    // Refer to: https://github.com/brianfromoregon/trie
    public class TrieNode
    {
        public Char Char { get; set; }
        public Int32 Frequency { get; set; }
        public Dictionary<Char, TrieNode> Children { get; set; }

        public TrieNode(Char ch)
        {
            Char = ch;
            Frequency = 0;

            // TODO: or an empty dict?
            //Children = null;
        }

        public Int32 Insert(String s, Int32 pos, Int32 freq = 1)
        {
            if (String.IsNullOrEmpty(s) || pos >= s.Length)
            {
                return 0;
            }

            if (Children == null)
            {
                Children = new Dictionary<Char, TrieNode>();
            }

            var c = s[pos];
            if (!Children.ContainsKey(c))
            {
                Children[c] = new TrieNode(c);
            }

            var curNode = Children[c];
            if (pos == s.Length - 1)
            {
                curNode.Frequency += freq;
                return curNode.Frequency;
            }

            return curNode.Insert(s, pos + 1, freq);
        }

        public TrieNode Search(String s, Int32 pos)
        {
            if (String.IsNullOrEmpty(s))
            {
                return null;
            }

            // if out of range or without any child nodes
            if (pos >= s.Length || Children == null)
            {
                return null;
            }
            // if reaches the last char of s, it's time to make the decision.
            if (pos == s.Length - 1)
            {
                return Children.ContainsKey(s[pos]) ? Children[s[pos]] : null;
            }
            // continue if necessary.
            return Children.ContainsKey(s[pos]) ? Children[s[pos]].Search(s, pos + 1) : null;
        }
    }

    public interface ITrie
    {
        //string BestMatch(string word, long maxTime);
        Boolean Contains(String word);
        Int32 Frequency(String word);
        Int32 Insert(String word, Int32 freq = 1);
        //bool Remove(string word);
        Int32 Count { get; }
        Int32 TotalFrequency { get; }
    }

    public class Trie : ITrie
    {
        private static readonly Char RootChar = '\0';

        internal TrieNode Root;

        public Int32 Count { get; private set; }
        public Int32 TotalFrequency { get; private set; }

        public Trie()
        {
            Root = new TrieNode(RootChar);
            Count = 0;
        }

        public Boolean Contains(String word)
        {
            CheckWord(word);

            var node = Root.Search(word.Trim(), 0);
            return node.IsNotNull() && node.Frequency > 0;
        }

        public Boolean ContainsPrefix(String word)
        {
            CheckWord(word);

            var node = Root.Search(word.Trim(), 0);
            return node.IsNotNull();
        }

        public Int32 Frequency(String word)
        {
            CheckWord(word);

            var node = Root.Search(word.Trim(), 0);
            return node.IsNull() ? 0 : node.Frequency;
        }

        public Int32 Insert(String word, Int32 freq = 1)
        {
            CheckWord(word);

            var i = Root.Insert(word.Trim(), 0, freq);
            if (i > 0)
            {
                TotalFrequency += freq;
                Count++;
            }

            return i;
        }

        public IEnumerable<Char> ChildChars(String prefix)
        {
            var node = Root.Search(prefix.Trim(), 0);
            return node.IsNull() || node.Children.IsNull() ? null : node.Children.Select(p => p.Key);
        }

        private void CheckWord(String word)
        {
            if (String.IsNullOrWhiteSpace(word))
            {
                throw new ArgumentException("word must not be null or whitespace");
            }
        }
    }
}