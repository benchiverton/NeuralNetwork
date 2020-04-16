﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GingerbreadAI.Model.NeuralNetwork.Models;
using GingerbreadAI.NLP.Word2Vec.AnalysisFunctions;
using GingerbreadAI.NLP.Word2Vec.DistanceFunctions;

namespace GingerbreadAI.NLP.Word2Vec.Extensions
{
    public static class FileHandlerExtensions
    {
        public static void WriteDescription(this FileHandler fileHandler, WordCollection wordCollection, int numberOfDimensions)
        {
            using (var fs = fileHandler.GetOutputFileStream())
            {
                fs.Seek(0, SeekOrigin.End);
                using (var writer = new StreamWriter(fs, Encoding.UTF8))
                {
                    writer.WriteLine(wordCollection.GetNumberOfUniqueWords());
                    writer.WriteLine(numberOfDimensions);
                }
            }
        }

        public static void WriteWordVectors(this FileHandler fileHandler, WordCollection wordCollection, int numberOfDimensions, float[,] hiddenLayerWeights)
        {
            using (var fs = fileHandler.GetOutputFileStream())
            {
                fs.Seek(0, SeekOrigin.End);
                using (var writer = new StreamWriter(fs, Encoding.UTF8))
                {
                    var keys = wordCollection.GetWords().ToArray();
                    for (var a = 0; a < wordCollection.GetNumberOfUniqueWords(); a++)
                    {
                        var bytes = new List<byte>();
                        for (var dimensionIndex = 0; dimensionIndex < numberOfDimensions; dimensionIndex++)
                        {
                            bytes.AddRange(BitConverter.GetBytes(hiddenLayerWeights[a, dimensionIndex]));
                        }

                        writer.WriteLine($"{keys[a]}\t{Convert.ToBase64String(bytes.ToArray())}");
                    }
                }
            }
        }

        /// <summary>
        /// Writes each word and it's associated vector.
        /// </summary>
        public static void WriteWordVectors(
            this FileHandler fileHandler,
            IEnumerable<(string word, double[] vector)> wordVectors)
        {
            using (var fs = fileHandler.GetOutputFileStream())
            {
                fs.Seek(0, SeekOrigin.End);
                using (var writer = new StreamWriter(fs, Encoding.UTF8))
                {
                    foreach (var (word, vector) in wordVectors)
                    {
                        writer.WriteLine($"{word},{string.Join(',', vector)}");
                    }
                }
            }
        }

        /// <summary>
        /// Writes each word and the top n words that it is most similar to.
        /// </summary>
        public static void WriteSimilarWords(
            this FileHandler fileHandler,
            IEnumerable<(string word, double[] vector)> wordVectors,
            int topn = 10)
        {
            using (var fs = fileHandler.GetOutputFileStream())
            {
                fs.Seek(0, SeekOrigin.End);
                using (var writer = new StreamWriter(fs, Encoding.UTF8))
                {
                    foreach (var word in wordVectors.Select(wv => wv.word))
                    {
                        var similarWords = WordVectorAnalysisFunctions.GetMostSimilarWords(word, wordVectors, topn);
                        writer.WriteLine($"{word},{string.Join(',', similarWords.Select(sw => $"{sw.word},{sw.similarity:0.00000}"))}");
                    }
                }
            }
        }

        /// <summary>
        /// Writes each word and the top n words that it is most similar to.
        /// </summary>
        public static void WriteWordClusterLabels(
            this FileHandler fileHandler, 
            IEnumerable<(string word, double[] vector)> wordVectors,
            double epsilon = 0.5,
            int minimumSamples = 5,
            DistanceFunctionType distanceFunctionType = DistanceFunctionType.Euclidean,
            int concurrentThreads = 4)
        {
            using (var fs = fileHandler.GetOutputFileStream())
            {
                fs.Seek(0, SeekOrigin.End);
                using (var writer = new StreamWriter(fs, Encoding.UTF8))
                {
                    foreach (var (word, clusterIndex) in WordVectorAnalysisFunctions.GetClusterLabels(
                        wordVectors.ToList(),
                        epsilon,
                        minimumSamples,
                        distanceFunctionType,
                        concurrentThreads))
                    {
                        writer.WriteLine($"{word},{clusterIndex}");
                    }
                }
            }
        }

        /// <summary>
        /// Writes a matrix where x and y axis are the words in the collection, and the point (x, y) is E(x|y).
        /// </summary>
        public static void WriteProbabilityMatrix(
            this FileHandler fileHandler,
            WordCollection wordCollection, 
            Layer neuralNetwork)
        {
            using (var fs = fileHandler.GetOutputFileStream())
            {
                fs.Seek(0, SeekOrigin.End);
                using (var writer = new StreamWriter(fs, Encoding.UTF8))
                {
                    var words = wordCollection.GetWords().ToList();

                    var stringBuilder = new StringBuilder();

                    stringBuilder.Append(",");
                    foreach (var word in words)
                    {
                        stringBuilder.Append($"{word},");
                    }
                    stringBuilder.AppendLine();
                    for (var i = 0; i < words.Count; i++)
                    {
                        var inputs = new double[words.Count];
                        inputs[i] = 1;
                        neuralNetwork.CalculateOutputs(inputs);

                        stringBuilder.Append($"{words[i]},");
                        for (var j = 0; j < words.Count; j++)
                        {
                            stringBuilder.Append($"{neuralNetwork.Nodes[j].Output},");
                        }
                        stringBuilder.AppendLine();
                    }

                    writer.WriteLine(stringBuilder.ToString());
                }
            }
        }
    }
}
