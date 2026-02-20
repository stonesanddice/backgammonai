using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace EngineCore
{
    public static class WeightParser
    {
        public static List<NeuralNet> Load(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Weight file not found: {filePath}");

            string fileContent = File.ReadAllText(filePath);
            string[] tokens = fileContent.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            int tokenIndex = 0;
            List<NeuralNet> nets = new List<NeuralNet>();

            while (tokenIndex < tokens.Length)
            {
                nets.Add(ParseSingleNet(tokens, ref tokenIndex));
            }

            return nets;
        }

        private static NeuralNet ParseSingleNet(string[] tokens, ref int index)
        {
            // Skip any textual headers/labels until we hit an integer
            while (index < tokens.Length && !int.TryParse(tokens[index], out _))
            {
                index++;
            }

            if (index >= tokens.Length)
                throw new EndOfStreamException("Reached end of tokens while searching for a network.");

            // 1. Read Header
            int inputs = int.Parse(tokens[index++]);
            int hidden = int.Parse(tokens[index++]);
            int outputs = int.Parse(tokens[index++]);
            int trained = int.Parse(tokens[index++]);
            float betaH = float.Parse(tokens[index++], CultureInfo.InvariantCulture);
            float betaO = float.Parse(tokens[index++], CultureInfo.InvariantCulture);

            NeuralNet net = new NeuralNet(inputs, hidden, outputs, trained, betaH, betaO);

            // 2. Read Hidden Weights [Grouped by Input]
            for (int i = 0; i < inputs; i++)
            {
                for (int j = 0; j < hidden; j++)
                {
                    net.HiddenWeights[i, j] = float.Parse(tokens[index++], CultureInfo.InvariantCulture);
                }
            }

            // 3. Read Output Weights [Grouped by Output]
            for (int o = 0; o < outputs; o++)
            {
                for (int h = 0; h < hidden; h++)
                {
                    net.OutputWeights[h, o] = float.Parse(tokens[index++], CultureInfo.InvariantCulture);
                }
            }

            // 4. Read Hidden Thresholds (Biases)
            for (int i = 0; i < hidden; i++)
            {
                net.HiddenBiases[i] = float.Parse(tokens[index++], CultureInfo.InvariantCulture);
            }

            // 5. Read Output Thresholds (Biases)
            for (int i = 0; i < outputs; i++)
            {
                net.OutputBiases[i] = float.Parse(tokens[index++], CultureInfo.InvariantCulture);
            }

            return net;
        }
    }
}