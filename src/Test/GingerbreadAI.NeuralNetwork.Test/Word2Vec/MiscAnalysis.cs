using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GingerbreadAI.NLP.Word2Vec;
using GingerbreadAI.NLP.Word2Vec.AnalysisFunctions;
using GingerbreadAI.NLP.Word2Vec.DistanceFunctions;
using GingerbreadAI.NLP.Word2Vec.Embeddings;
using GingerbreadAI.NLP.Word2Vec.Extensions;

namespace GingerbreadAI.NeuralNetwork.Test.Word2Vec
{
    public class MiscAnalysis
    {
        private const string InputFileLoc = "C:\\Temp\\misc.csv";
        private const string ResultsDirectory = nameof(MiscAnalysis);

        [RunnableInDebugOnly]
        public void TrainWordEmbeddings()
        {
            var id = DateTime.Now.Ticks;
            var embeddingsFileLoc = $@"{Directory.GetCurrentDirectory()}/{ResultsDirectory}/wordEmbeddings-{id}.csv";
            Directory.CreateDirectory($@"{Directory.GetCurrentDirectory()}/{ResultsDirectory}");

            var word2Vec = new Word2VecTrainer();
            word2Vec.Setup(InputFileLoc, minWordOccurrences: 3);
            word2Vec.TrainModel(useCbow: false, numberOfIterations: 20);
            word2Vec.WriteWordEmbeddings(embeddingsFileLoc);
        }

        [RunnableInDebugOnly]
        public void GenerateReportFromLatestEmbeddings()
        {
            var embeddingsFile = new DirectoryInfo($@"{Directory.GetCurrentDirectory()}/{ResultsDirectory}").EnumerateFiles()
                .Where(f => Regex.IsMatch(f.Name, "^wordEmbeddings-.*$"))
                .OrderBy(f => f.CreationTime)
                .Last();
            var reportFileLoc = $@"{Directory.GetCurrentDirectory()}/{ResultsDirectory}/report-{DateTime.Now.Ticks}.csv";

            using var fileStream = new FileStream(embeddingsFile.FullName, FileMode.OpenOrCreate, FileAccess.Read);
            using var reader = new StreamReader(fileStream, Encoding.UTF8);
            var wordEmbeddings = new List<WordEmbedding>();
            wordEmbeddings.PopulateWordEmbeddingsFromStream(reader);

            var articles = new List<ArticleEmbedding>();
            foreach (var line in File.ReadLines(InputFileLoc))
            {
                var splitLine = line.Split(',');
                articles.Add(new ArticleEmbedding(splitLine[0], string.Join(' ', splitLine.Skip(1))));
            }

            articles.AssignWeightedVectorsFromWordEmbeddings(wordEmbeddings);

            var titleClusterMap = DBSCAN.GetLabelClusterIndexMap(
                articles,
                epsilon: 0.1,
                minimumSamples: 3,
                distanceFunctionType: DistanceFunctionType.Cosine,
                concurrentThreads: 4
            );

            var reportHandler = new ReportWriter(reportFileLoc);
            reportHandler.WriteLabelsWithClusterIndex(titleClusterMap, articles.Select(we => we.Label));
        }
    }
}
