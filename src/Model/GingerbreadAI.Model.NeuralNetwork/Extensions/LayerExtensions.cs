using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using GingerbreadAI.Model.NeuralNetwork.Models;

namespace GingerbreadAI.Model.NeuralNetwork.Extensions
{

    public static class LayerExtensions
    {
        /// <summary>
        /// Initialises each Node in the layer with random weights.
        /// </summary>
        public static void Initialise(this Layer layer, Random rand)
        {
            foreach (var node in layer.Nodes)
            {
                node.Initialise(rand, layer.InitialisationFunction, layer.Nodes.Count);
            }
            foreach (var nodeGroupPrev in layer.PreviousLayers)
            {
                nodeGroupPrev.Initialise(rand);
            }
        }

        public static double[] GetResults(this Layer layer, double[] inputs)
        {
            layer.CalculateOutputs(inputs);
            return layer.Nodes.Select(n => n.Output).ToArray();
        }

        public static double[] GetResults(this Layer layer, Dictionary<Layer, double[]> inputs)
        {
            layer.CalculateOutputs(inputs);
            return layer.Nodes.Select(n => n.Output).ToArray();
        }

        public static double GetResult(this Layer layer, int inputIndex, int outputIndex, double inputValue = 1)
        {
            layer.CalculateIndexedOutput(inputIndex, outputIndex, inputValue);
            return layer.Nodes[outputIndex].Output;
        }

        /// <summary>
        /// Returns a clone of the network with different outputs.
        /// This can solve the problem of the same network overwriting its outputs in a multi-threaded scenario
        /// </summary>
        public static Layer CloneWithDifferentOutputs(this Layer layer)
        {
            var nodes = new Node[layer.Nodes.Count];
            if (!layer.PreviousLayers.Any())
            {
                for (var i = 0; i < layer.Nodes.Count; i++)
                {
                    nodes[i] = new Node();
                }

                var newInputLayer = new Layer(
                    nodes,
                    Array.Empty<Layer>(),
                    layer.ActivationFunctionType,
                    layer.InitialisationFunctionType);
                
                return newInputLayer;
            }
            
            var previousLayers = layer.PreviousLayers.Select(pl => pl.CloneWithDifferentOutputs()).ToArray();
            for (var i = 0; i < layer.Nodes.Count; i++)
            {
                var newNode = new Node();

                for (var j = 0; j < layer.PreviousLayers.Count; j++)
                {
                    for (var k = 0; k < layer.PreviousLayers[j].Nodes.Count; k++)
                    {
                        if (layer.Nodes[i].Weights.TryGetValue(layer.PreviousLayers[j].Nodes[k], out var weight))
                        {
                            newNode.Weights.Add(previousLayers[j].Nodes[k], weight);
                        }
                    }
                    if (layer.Nodes[i].BiasWeights.TryGetValue(layer.PreviousLayers[j], out var biasWeight))
                    {
                        newNode.BiasWeights.Add(previousLayers[j], biasWeight);
                    }
                }

                nodes[i] = newNode;
            }

            return new Layer
            (
                nodes,
                previousLayers,
                layer.ActivationFunctionType,
                layer.InitialisationFunctionType
            );
        }

        public static Layer DeepCopy(this Layer layer)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, layer);
                ms.Position = 0;
                return (Layer)formatter.Deserialize(ms);
            }
        }

        public static void SaveNetwork(this Layer layer, string location)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, layer);
                using (var fileStream = File.Create(location))
                {
                    ms.WriteTo(fileStream);
                }
            }
        }

        public static Layer LoadNetwork(string location)
        {
            using (var byteStream = File.OpenRead(location))
            {
                using (var memoryStream = new MemoryStream())
                {
                    byteStream.CopyTo(memoryStream);
                    memoryStream.Position = 0;

                    var formatter = new BinaryFormatter();
                    return (Layer)formatter.Deserialize(memoryStream);
                }
            }
        }

        #region Private Methods


        private static void Initialise(this Node node, Random rand, Func<Random, int, int, double> initialisationFunction, int nodeCount)
        {
            if (node == null)
            {
                return;
            }

            var feedingNodes = node.Weights.Count;
            foreach (var prevNode in node.Weights.Keys.ToList())
            {
                node.Weights[prevNode].Adjust(initialisationFunction.Invoke(rand, feedingNodes, nodeCount));
            }
            var biasWeightKeys = new List<Layer>(node.BiasWeights.Keys.ToList());
            foreach (var biasWeightKey in biasWeightKeys)
            {
                node.BiasWeights[biasWeightKey].Adjust(initialisationFunction.Invoke(rand, feedingNodes, nodeCount));
            }
        }

        #endregion
    }
}
