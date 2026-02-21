# BackgammonAI: Grandmaster-Level Engine in C#

BackgammonAI is a high-performance backgammon engine written in C# and optimized for integration into Unity projects. It utilizes a custom-built Neural Network evaluator that implements the official GNU Backgammon (GNUbg) weights to provide professional-grade board evaluations and move selection.

---

## 🚀 Features

* **Neural Network Evaluator**: Implements a Multi-Layer Perceptron (MLP) using GNUbg's `contact250` weights.
* **Move Generation**: A flawless recursive algorithm that generates every physically legal sequence of moves for any given dice roll.
* **Performance**: Executes a full 1-ply search (evaluating ~30-50 legal moves) in approximately **20–25ms**.
* **Standard Compatibility**: Fully supports GNUbg Position IDs (Base64) for seamless board importing and exporting.
* **Stones and Dice Visualizer**: A beautiful ASCII board visualizer that replicates the layout of the `board.py` utility.

---

## 📂 Project Structure

* **EngineCore**: The heart of the engine.
* `NeuralNet.cs`: Matrix multiplication and activation functions ( scaling).
* `GameState.cs`: Represents the physical board, pips, and match state.
* `PositionIdParser.cs`: Handles 80-bit GNUbg key encoding/decoding.
* `FeatureEncoder.cs`: Translates board states into the 250-node input vector required by the network.


* **EngineCLI**: A command-line interface for testing the engine and running 1-ply searches.
* **EngineTests**: An xUnit suite verifying pip counts, bitstream accuracy, and move logic.

---

## 🛠️ Installation & Setup

1. **Clone the Repository**:
```bash
git clone https://github.com/StonesAndDice/BackgammonAI.git

```


2. **Add GNUbg Weights**:
   The engine requires the `gnubg.weights` file. Place it in your `data/gnubg-nn/` directory.
3. **Run the CLI**:
```bash
dotnet run --project EngineCLI/EngineCLI.csproj

```



---

## 🧩 Technical Deep Dive

### The 80-Bit Position ID

The engine communicates using the standard GNUbg Position ID. This ID is an 80-bit key packed into 14 Base64 characters. It represents the checkers on each of the 24 points, followed by the checkers on the bar, using a unary bitstream separated by zeros.

### Neural Network Architecture

The brain of the engine is a 2-layer MLP:

* **Inputs (250)**: Physical board positions (0-199) and advanced heuristics (200-249).
* **Hidden Layer (128)**: Utilizes Sigmoid activation with a  of 0.1.
* **Output Layer (5)**: Produces probabilities for Win, Win Gammon, Win Backgammon, Lose Gammon, and Lose Backgammon.

---

## 🧪 Running Tests

To ensure the engine is properly calibrated (especially Pip Counts and Position ID accuracy), run the xUnit suite:

```bash
dotnet test

```

Standard starting board verification:

* **Position ID**: `4HPwATDgc/ABMA`
* **Pip Count**: 167

---

## 📜 Licensing

This project is intended for educational and game development purposes. Please note that the use of GNUbg weights is subject to the **GNU General Public License (GPL)**. If you are integrating this into a commercial Unity project, ensure you are compliant with GPL requirements regarding derivative works.
