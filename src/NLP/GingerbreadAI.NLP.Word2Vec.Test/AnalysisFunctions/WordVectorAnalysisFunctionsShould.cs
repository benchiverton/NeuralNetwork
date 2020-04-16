﻿using System.Collections.Generic;
using System.Linq;
using GingerbreadAI.NLP.Word2Vec.AnalysisFunctions;
using Xunit;

namespace GingerbreadAI.NLP.Word2Vec.Test.AnalysisFunctions
{
    public class WordVectorAnalysisFunctionsShould
    {
        [Fact]
        public void CorrectlyGetMostSimilarWords()
        {
            var wordVectors = new (string word, double[] vector)[]
            {
                ("target", new [] {1d, 1d}),
                ("far", new [] {-1d, -1d}),
                ("close-ish", new [] {-1d, 1d}),
                ("same", new [] {1d, 1d}),
            };

            var orderedWords = WordVectorAnalysisFunctions.GetMostSimilarWords("target", wordVectors, 3).ToArray();
            Assert.Equal(1d, orderedWords[0].similarity, 8);
            Assert.Equal("same", orderedWords[0].word);
            Assert.Equal(0d, orderedWords[1].similarity, 8);
            Assert.Equal("close-ish", orderedWords[1].word);
            Assert.Equal(-1d, orderedWords[2].similarity, 8);
            Assert.Equal("far", orderedWords[2].word);
        }

        [Theory]
        [MemberData(nameof(GetCorrectlyGetClusterLabelsForWordsTestData))]
        public void CorrectlyGetClusterLabels(List<(string word, double[] vector)> wordVectorWeights, (string[] elements, bool isNoise)[] expectedGroups)
        {
            var labels = WordVectorAnalysisFunctions.GetClusterLabels(
                wordVectorWeights,
                3,
                2
            );

            foreach (var group in expectedGroups)
            {
                var groupLabel = group.isNoise
                    ? -1
                    : labels.First(l => l.Key == group.elements[0]).Value;
                foreach (var label in labels.Where(l => group.elements.Contains(l.Key)))
                {
                    Assert.Equal(groupLabel, label.Value);
                }
            }
        }

        public static IEnumerable<object[]> GetCorrectlyGetClusterLabelsForWordsTestData()
        {
            yield return new object[]
            {
                new List<(string word, double[] vector)>
                {
                    ("a", new[] {1d, 2d}),
                    ("b", new[] {2d, 2d}),
                    ("c", new[] {2d, 3d}),
                    ("d", new[] {8d, 7d}),
                    ("e", new[] {8d, 8d}),
                    ("f", new[] {25d, 80d})
                },
                new (string[] elements, bool isNoise)[]
                {
                    (new [] {"a", "b", "c"}, false),
                    (new [] {"d", "e"}, false),
                    (new [] {"f"}, true),
                }
            };
            yield return new object[]
            {
                new List<(string word, double[] vector)>
                {
                    ("a", new[] {0d, 0d}),
                    ("d", new[] {10d, 10d}),
                    ("e", new[] {11d, 11d}),
                    ("b", new[] {2d, 2d}),
                    ("f", new[] {25d, 80d}),
                    ("c", new[] {4d, 4d}),
                },
                new (string[] elements, bool isNoise)[]
                {
                    (new [] {"a", "b", "c"}, false),
                    (new [] {"d", "e"}, false),
                    (new [] {"f"}, true),
                }
            };
            yield return new object[]
            {
                new List<(string word, double[] vector)>
                {
                    ("a", new[] {25d, 80d}),
                    ("b", new[] {8d, 8d}),
                    ("c", new[] {8d, 7d}),
                    ("d", new[] {2d, 3d}),
                    ("e", new[] {2d, 2d}),
                    ("f", new[] {1d, 2d}),
                },
                new (string[] elements, bool isNoise)[]
                {
                    (new [] {"a"}, true),
                    (new [] {"b", "c"}, false),
                    (new [] {"d", "e", "f"}, false),
                }
            };
        }
    }
}
