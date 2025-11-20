using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using JiebaNet.Segmenter.Common;
//using Newtonsoft.Json;

namespace JiebaNet.Segmenter.FinalSeg
{
    public class Viterbi : IFinalSeg
    {
        private static readonly Lazy<Viterbi> Lazy = new Lazy<Viterbi>(() => new Viterbi());
        private static readonly Char[] States = { 'B', 'M', 'E', 'S' };

        private static readonly Regex RegexChinese = new Regex(@"([\u4E00-\u9FD5]+)", RegexOptions.Compiled);
        private static readonly Regex RegexSkip = new Regex(@"(\d+\.\d+|[a-zA-Z0-9]+)", RegexOptions.Compiled);

        private static IDictionary<Char, IDictionary<Char, Double>> _emitProbs;
        private static IDictionary<Char, Double> _startProbs;
        private static IDictionary<Char, IDictionary<Char, Double>> _transProbs;
        private static IDictionary<Char, Char[]> _prevStatus;

        private Viterbi()
        {
            LoadModel();
        }

        // TODO: synchronized
        public static Viterbi Instance
        {
            get { return Lazy.Value; }
        }

        public IEnumerable<String> Cut(String sentence)
        {
            var tokens = new List<String>();
            foreach (var blk in RegexChinese.Split(sentence))
            {
                if (RegexChinese.IsMatch(blk))
                {
                    tokens.AddRange(ViterbiCut(blk));
                }
                else
                {
                    var segments = RegexSkip.Split(blk).Where(seg => !String.IsNullOrEmpty(seg));
                    tokens.AddRange(segments);
                }
            }
            return tokens;
        }

        #region Private Helpers

        private void LoadModel()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            _prevStatus = new Dictionary<Char, Char[]>()
            {
                {'B', new []{'E', 'S'}},
                {'M', new []{'M', 'B'}},
                {'S', new []{'S', 'E'}},
                {'E', new []{'B', 'M'}}
            };

            _startProbs = new Dictionary<Char, Double>()
            {
                {'B', -0.26268660809250016},
                {'E', -3.14e+100},
                {'M', -3.14e+100},
                {'S', -1.4652633398537678}
            };

            var transJson = FileExtension.ReadEmbeddedAllLine(ConfigManager.ProbTransFile);
            _transProbs = JsonSerializer
                .Deserialize<
                    IDictionary<Char, IDictionary<Char, Double>>>(transJson);

            var emitJson = FileExtension.ReadEmbeddedAllLine(ConfigManager.ProbEmitFile);
            _emitProbs = JsonSerializer
                .Deserialize<
                    IDictionary<Char, IDictionary<Char, Double>>>(emitJson);

            stopWatch.Stop();
            Debug.WriteLine("model loading finished, time elapsed {0} ms.", stopWatch.ElapsedMilliseconds);
        }

        private IEnumerable<String> ViterbiCut(String sentence)
        {
            var v = new List<IDictionary<Char, Double>>();
            IDictionary<Char, Node> path = new Dictionary<Char, Node>();

            // Init weights and paths.
            v.Add(new Dictionary<Char, Double>());
            foreach (var state in States)
            {
                var emP = _emitProbs[state].GetDefault(sentence[0], Constants.MinProb);
                v[0][state] = _startProbs[state] + emP;
                path[state] = new Node(state, null);
            }

            // For each remaining char
            for (var i = 1; i < sentence.Length; ++i)
            {
                IDictionary<Char, Double> vv = new Dictionary<Char, Double>();
                v.Add(vv);
                IDictionary<Char, Node> newPath = new Dictionary<Char, Node>();
                foreach (var y in States)
                {
                    var emp = _emitProbs[y].GetDefault(sentence[i], Constants.MinProb);

                    var candidate = new Pair<Char>('\0', Double.MinValue);
                    foreach (var y0 in _prevStatus[y])
                    {
                        var tranp = _transProbs[y0].GetDefault(y, Constants.MinProb);
                        tranp = v[i - 1][y0] + tranp + emp;
                        if (candidate.Freq <= tranp)
                        {
                            candidate.Freq = tranp;
                            candidate.Key = y0;
                        }
                    }
                    vv[y] = candidate.Freq;
                    newPath[y] = new Node(y, path[candidate.Key]);
                }
                path = newPath;
            }

            var probE = v[sentence.Length - 1]['E'];
            var probS = v[sentence.Length - 1]['S'];
            var finalPath = probE < probS ? path['S'] : path['E'];

            var posList = new List<Char>(sentence.Length);
            while (finalPath != null)
            {
                posList.Add(finalPath.Value);
                finalPath = finalPath.Parent;
            }
            posList.Reverse();

            var tokens = new List<String>();
            Int32 begin = 0, next = 0;
            for (var i = 0; i < sentence.Length; i++)
            {
                var pos = posList[i];
                if (pos == 'B')
                    begin = i;
                else if (pos == 'E')
                {
                    tokens.Add(sentence.Sub(begin, i + 1));
                    next = i + 1;
                }
                else if (pos == 'S')
                {
                    tokens.Add(sentence.Sub(i, i + 1));
                    next = i + 1;
                }
            }
            if (next < sentence.Length)
            {
                tokens.Add(sentence.Substring(next));
            }

            return tokens;
        }

        #endregion
    }
}