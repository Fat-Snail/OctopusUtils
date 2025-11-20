using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using JiebaNet.Segmenter.Common;

namespace JiebaNet.Analyser
{
    public class IdfLoader
    {
        internal String IdfFilePath { get; set; }
        internal IDictionary<String, Double> IdfFreq { get; set; }
        internal Double MedianIdf { get; set; }

        public IdfLoader(String idfPath = null)
        {
            IdfFilePath = String.Empty;
            IdfFreq = new Dictionary<String, Double>();
            MedianIdf = 0.0;
            if (!String.IsNullOrWhiteSpace(idfPath))
            {
                SetNewPath(idfPath);
            }
        }

        public void SetNewPath(String newIdfPath)
        {
            var idfPath = newIdfPath;
            if (IdfFilePath != idfPath)
            {
                IdfFilePath = idfPath;
                var lines = FileExtension.ReadEmbeddedAllLines(idfPath, Encoding.UTF8);
                IdfFreq = new Dictionary<String, Double>();
                foreach (var line in lines)
                {
                    var parts = line.Trim().Split(' ');
                    var word = parts[0];
                    var freq = Double.Parse(parts[1]);
                    IdfFreq[word] = freq;
                }

                MedianIdf = IdfFreq.Values.OrderBy(v => v).ToList()[IdfFreq.Count / 2];
            }
        }
    }
}