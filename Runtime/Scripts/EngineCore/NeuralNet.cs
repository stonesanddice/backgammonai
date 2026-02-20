using System;

namespace EngineCore
{
    public class NeuralNet
    {
        public int InputCount { get; private set; }
        public int HiddenCount { get; private set; }
        public int OutputCount { get; private set; }
        public int TrainedIterations { get; private set; }
        public float BetaHidden { get; private set; }
        public float BetaOutput { get; private set; }

        public float[] LastLogits { get; private set; }

        // Weights and Biases (Thresholds)
        public float[,] HiddenWeights { get; private set; }
        public float[] HiddenBiases { get; private set; }
        public float[,] OutputWeights { get; private set; }
        public float[] OutputBiases { get; private set; }

        public NeuralNet(int inputs, int hidden, int outputs, int trainedIters, float betaH, float betaO)
        {
            InputCount = inputs;
            HiddenCount = hidden;
            OutputCount = outputs;
            TrainedIterations = trainedIters;
            BetaHidden = betaH;
            BetaOutput = betaO;

            HiddenWeights = new float[inputs, hidden];
            HiddenBiases = new float[hidden];
            OutputWeights = new float[hidden, outputs];
            OutputBiases = new float[outputs];
            LastLogits = new float[outputs];
        }

        public float[] Evaluate(float[] inputs)
        {
            if (inputs.Length != InputCount)
                throw new ArgumentException($"Expected {InputCount} inputs, got {inputs.Length}");

            // 1. Calculate Hidden Layer
            float[] hiddenLayer = new float[HiddenCount];
            for (int i = 0; i < HiddenCount; i++)
            {
                // GNUbg logic: Start with the threshold
                float sum = HiddenBiases[i];
                for (int j = 0; j < InputCount; j++)
                {
                    sum += inputs[j] * HiddenWeights[j, i];
                }
                // Apply beta and sigmoid: sigmoid(-beta * sum)
                hiddenLayer[i] = Sigmoid(sum, BetaHidden);
            }

            // 2. Calculate Output Layer
            float[] outputs = new float[OutputCount];
            for (int i = 0; i < OutputCount; i++)
            {
                float sum = OutputBiases[i];
                for (int j = 0; j < HiddenCount; j++)
                {
                    sum += hiddenLayer[j] * OutputWeights[j, i];
                }

                LastLogits[i] = sum;
                outputs[i] = Sigmoid(sum, BetaOutput);
            }

            return outputs;
        }

        private static float Sigmoid(float sum, float beta)
        {
            // GNUbg uses sigmoid(-beta * sum)
            double x = -beta * sum;
            return 1.0f / (1.0f + (float)Math.Exp(x));
        }
    }
}