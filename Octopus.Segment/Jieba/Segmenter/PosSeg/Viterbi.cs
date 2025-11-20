using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using JiebaNet.Segmenter.Common;
//using Newtonsoft.Json;

namespace JiebaNet.Segmenter.PosSeg
{
    public class Viterbi
    {
        private static readonly Lazy<Viterbi> Lazy = new Lazy<Viterbi>(() => new Viterbi());

        private static IDictionary<String, Double> _startProbs;
        private static IDictionary<String, IDictionary<String, Double>> _transProbs;
        private static IDictionary<String, IDictionary<Char, Double>> _emitProbs;
        private static IDictionary<Char, List<String>> _stateTab;

        private Viterbi()
        {
            LoadModel();
        }

        // TODO: synchronized
        public static Viterbi Instance
        {
            get { return Lazy.Value; }
        }

        public IEnumerable<Pair> Cut(String sentence)
        {
            var probPosList = ViterbiCut(sentence);
            var posList = probPosList.Item2;

            var tokens = new List<Pair>();
            Int32 begin = 0, next = 0;
            for (var i = 0; i < sentence.Length; i++)
            {
                var parts = posList[i].Split('-');
                var charState = parts[0][0];
                var pos = parts[1];
                if (charState == 'B')
                    begin = i;
                else if (charState == 'E')
                {
                    tokens.Add(new Pair(sentence.Sub(begin, i + 1), pos));
                    next = i + 1;
                }
                else if (charState == 'S')
                {
                    tokens.Add(new Pair(sentence.Sub(i, i + 1), pos));
                    next = i + 1;
                }
            }
            if (next < sentence.Length)
            {
                tokens.Add(new Pair(sentence.Substring(next), posList[next].Split('-')[1]));
            }

            return tokens;
        }

        #region Private Helpers

        private static void LoadModel()
        {
            var startJson = FileExtension.ReadEmbeddedAllLine(ConfigManager.PosProbStartFile);
            _startProbs = JsonSerializer
                    .Deserialize<IDictionary<String, Double>>(startJson);

            var transJson = FileExtension.ReadEmbeddedAllLine(ConfigManager.PosProbTransFile);
            _transProbs = JsonSerializer
                .Deserialize<IDictionary<String, IDictionary<String, Double>>>(transJson);

            var emitJson = FileExtension.ReadEmbeddedAllLine(ConfigManager.PosProbEmitFile);
            _emitProbs = JsonSerializer
                .Deserialize<IDictionary<String, IDictionary<Char, Double>>>(emitJson);

            var tabJson = FileExtension.ReadEmbeddedAllLine(ConfigManager.CharStateTabFile);
            _stateTab = JsonSerializer
                .Deserialize<IDictionary<Char, List<String>>>(tabJson);
        }

        // TODO: change sentence to obs?
        private Tuple<Double, List<String>> ViterbiCut(String sentence)
        {
            var v = new List<IDictionary<String, Double>>();
            var memPath = new List<IDictionary<String, String>>();

            var allStates = _transProbs.Keys.ToList();

            // Init weights and paths.
            v.Add(new Dictionary<String, Double>());
            memPath.Add(new Dictionary<String, String>());
            foreach (var state in _stateTab.GetDefault(sentence[0], allStates))
            {
                var emP = _emitProbs[state].GetDefault(sentence[0], Constants.MinProb);
                v[0][state] = _startProbs[state] + emP;
                memPath[0][state] = String.Empty;
            }

            // For each remaining char
            for (var i = 1; i < sentence.Length; ++i)
            {
                v.Add(new Dictionary<String, Double>());
                memPath.Add(new Dictionary<String, String>());

                var prevStates = memPath[i - 1].Keys.Where(k => _transProbs[k].Count > 0);
                var curPossibleStates = new HashSet<String>(prevStates.SelectMany(s => _transProbs[s].Keys));

                IEnumerable<String> obsStates = _stateTab.GetDefault(sentence[i], allStates);
                obsStates = curPossibleStates.Intersect(obsStates);

                if (!obsStates.Any())
                {
                    if (curPossibleStates.Count > 0)
                    {
                        obsStates = curPossibleStates;
                    }
                    else
                    {
                        obsStates = allStates;
                    }
                }

                foreach (var y in obsStates)
                {
                    var emp = _emitProbs[y].GetDefault(sentence[i], Constants.MinProb);

                    var prob = Double.MinValue;
                    var state = String.Empty;

                    foreach (var y0 in prevStates)
                    {
                        var tranp = _transProbs[y0].GetDefault(y, Double.MinValue);
                        tranp = v[i - 1][y0] + tranp + emp;
                        // TODO: compare two very small values;
                        // TODO: how to deal with negative infinity
                        if (prob < tranp ||
                            (prob == tranp && String.Compare(state, y0, StringComparison.CurrentCultureIgnoreCase) < 0))
                        {
                            prob = tranp;
                            state = y0;
                        }
                    }
                    v[i][y] = prob;
                    memPath[i][y] = state;
                }
            }

            var vLast = v.Last();
            var last = memPath.Last().Keys.Select(y => new { State = y, Prob = vLast[y] });
            var endProb = Double.MinValue;
            var endState = String.Empty;
            foreach (var endPoint in last)
            {
                // TODO: compare two very small values;
                if (endProb < endPoint.Prob ||
                    (endProb == endPoint.Prob && String.Compare(endState, endPoint.State, StringComparison.CurrentCultureIgnoreCase) < 0))
                {
                    endProb = endPoint.Prob;
                    endState = endPoint.State;
                }
            }

            var route = new String[sentence.Length];
            var n = sentence.Length - 1;
            var curState = endState;
            while (n >= 0)
            {
                route[n] = curState;
                curState = memPath[n][curState];
                n--;
            }

            return new Tuple<Double, List<String>>(endProb, route.ToList());
        }

        #endregion
    }
}