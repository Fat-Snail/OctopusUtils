using System;
using System.Collections.Generic;
using System.Linq;

namespace JiebaNet.Analyser
{
    public class Edge
    {
        public String Start { get; set; }
        public String End { get; set; }
        public Double Weight { get; set; }
    }

    public class UndirectWeightedGraph
    {
        private static readonly Double d = 0.85;

        public IDictionary<String, List<Edge>> Graph { get; set; }
        public UndirectWeightedGraph()
        {
            Graph = new Dictionary<String, List<Edge>>();
        }

        public void AddEdge(String start, String end, Double weight)
        {
            if (!Graph.ContainsKey(start))
            {
                Graph[start] = new List<Edge>();
            }

            if (!Graph.ContainsKey(end))
            {
                Graph[end] = new List<Edge>();
            }

            Graph[start].Add(new Edge() { Start = start, End = end, Weight = weight });
            Graph[end].Add(new Edge() { Start = end, End = start, Weight = weight });
        }

        public IDictionary<String, Double> Rank()
        {
            var ws = new Dictionary<String, Double>();
            var outSum = new Dictionary<String, Double>();

            // init scores
            var count = Graph.Count > 0 ? Graph.Count : 1;
            var wsdef = 1.0 / count;

            foreach (var pair in Graph)
            {
                ws[pair.Key] = wsdef;
                outSum[pair.Key] = pair.Value.Sum(e => e.Weight);
            }

            // TODO: 10 iterations?
            var sortedKeys = Graph.Keys.OrderBy(k => k);
            for (var i = 0; i < 10; i++)
            {
                foreach (var n in sortedKeys)
                {
                    var s = 0d;
                    foreach (var edge in Graph[n])
                    {
                        s += edge.Weight / outSum[edge.End] * ws[edge.End];
                    }
                    ws[n] = (1 - d) + d * s;
                }
            }

            var minRank = Double.MaxValue;
            var maxRank = Double.MinValue;

            foreach (var w in ws.Values)
            {
                if (w < minRank)
                {
                    minRank = w;
                }
                if (w > maxRank)
                {
                    maxRank = w;
                }
            }

            foreach (var pair in ws.ToList())
            {
                ws[pair.Key] = (pair.Value - minRank / 10.0) / (maxRank - minRank / 10.0);
            }

            return ws;
        }
    }
}