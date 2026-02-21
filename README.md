# BackgammonAI

A grandmaster-level backgammon engine written in C# that implements professional AI using GNU Backgammon (GNUbg) neural networks. Designed for Unity integration with a standalone CLI for testing and gameplay.

## Overview

BackgammonAI provides a complete, production-ready backgammon engine with:

- **Neural Network Evaluator**: 2-layer MLP using official GNUbg `contact250` weights (250 inputs → 128 hidden → 5 outputs)
- **Expectiminimax Search**: Configurable 1-ply or 2-ply lookahead with probability-weighted opponent responses
- **Perfect Endgame Play**: GNUbg database integration for flawless bearoff positions
- **Doubling Cube Logic**: Full support for money games and match play with cube efficiency
- **High Performance**: 20-25ms for complete 1-ply search (~30-50 legal moves)
- **Standard Compatibility**: GNUbg Position ID encoding/decoding (80-bit Base64)

## Quick Start

### CLI Gameplay

```bash
dotnet run --project EngineCLI/EngineCLI.csproj
```

Play interactively against the AI using move notation like `24/20 13/9`.

### Unity Integration

1. Add the package to your Unity project via Package Manager (Add from disk)
2. Select the package folder: `com.stonesandice.backgammonai`
3. Use the menu: `Tools > Stones and Ice > Install Backgammon Data` to copy data files to StreamingAssets
4. Reference the engine in your code:

```csharp
using EngineCore;

// Initialize the engine
var networks = WeightParser.Load(weightsPath);
var contactNet = networks.First(n => n.InputCount == 250);
var bearoffEval = new BearoffEvaluator(dataDir);
var cubeEval = new CubeEvaluator();
var ai = new SearchEngine(contactNet, null, bearoffEval, cubeEval);

// Get best move
GameState state = CreateYourGameState();
Turn bestMove = ai.GetBestTurn(state, matchState, depth: 2);
```

## Project Structure

```
BackgammonAI/
├── com.stonesandice.backgammonai/     # Unity Package
│   ├── Runtime/
│   │   ├── Scripts/EngineCore/        # Core engine (21 classes)
│   │   └── Data/                      # GNUbg weights & databases
│   ├── Editor/                        # Unity editor tools
│   └── Tests/Runtime/EngineTests/     # Comprehensive test suite
├── EngineCLI/                         # Standalone CLI application
└── README.md                          # This file
```

## Core Components

### Engine Architecture

- **SearchEngine**: Expectiminimax algorithm with transposition table caching
- **MoveGenerator**: Recursive legal move generation with proper backgammon rules
- **NeuralNet**: 2-layer MLP with sigmoid activation
- **FeatureEncoder**: Converts board states to 250-element feature vectors
- **BearoffEvaluator**: Perfect play using GNUbg endgame databases
- **CubeEvaluator**: Doubling cube decisions for money games and match play
- **BoardClassifier**: Position categorization (contact, race, bearoff, crashed)

### Game State Management

- **GameState**: Complete board representation with 25-point arrays for each player
- **MatchState**: Match context (score, cube ownership, Crawford game, rules)
- **PositionId**: GNUbg-compatible 80-bit Position ID encoding/decoding
- **Move/Turn**: Individual moves and complete turns with hit detection

## Features

### Move Generation
- Recursive backtracking algorithm generating all legal move sequences
- Proper handling of doubles (4 dice)
- Bar entry requirements
- Bearing off rules with overshoot handling
- Maximum dice utilization enforcement
- Larger die preference when only one die can be played

### AI Evaluation
- **1-ply search**: Direct position evaluation (fast)
- **2-ply search**: Considers opponent's best responses across all 21 dice rolls (stronger)
- Probability-weighted averaging at chance nodes
- Transposition table with 500K position cache
- Cube efficiency adjustments
- Match equity table integration

### Position Analysis
- Win/Gammon/Backgammon probability estimation
- Pip count calculation
- Advanced heuristics (escape counts, mobility, timing, backbone strength)
- Contact vs race vs bearoff classification
- Database lookup for endgame positions

## Testing

Run the comprehensive test suite:

```bash
dotnet test
```

Test coverage includes:
- Move generation and validation
- Position ID encoding/decoding
- Neural network evaluation
- Feature encoding accuracy
- Bearoff database lookups
- Cube logic and equity calculations
- Strategic position evaluation
- Grandmaster-level strength validation

### Standard Position Verification
- Starting position: `4HPwATDgc/ABMA`
- Pip count: 167 for each player

## Requirements

- .NET 7.0 or higher
- Unity 2021.3+ (for Unity integration)
- GNUbg data files (included in package):
  - `gnubg.weights` (neural network weights)
  - `gnubg_ts0.bd` (two-sided bearoff database)
  - `gnubg_os0.bd` (one-sided bearoff database)
  - `gnubg_os.db` (bearoff database index)

## Performance

- **1-ply search**: 20-25ms for ~30-50 legal moves
- **2-ply search**: ~500ms-2s depending on position complexity
- **Memory**: ~50MB for neural networks and databases
- **Cache**: 500K position limit prevents memory exhaustion

## License

This project is licensed under the GNU General Public License (GPL) due to the use of GNU Backgammon weights and databases. If you're integrating this into a commercial project, ensure GPL compliance for derivative works.

See [LICENSE](LICENSE) for details.

## Technical Details

### Neural Network Architecture

```
Input Layer:    250 nodes (board positions + heuristics)
                ↓
Hidden Layer:   128 nodes (sigmoid, beta=0.1)
                ↓
Output Layer:   5 nodes (Win, WinG, WinBG, LoseG, LoseBG)
```

### Position ID Format

80-bit key encoded as 14 Base64 characters representing:
- Checkers on points 0-23 (unary encoding with zero separators)
- Checkers on bar
- Turn indicator

### Expectiminimax Algorithm

```
Player Node:  MAX(all legal moves)
    ↓
Chance Node:  Σ(probability × opponent_response) for 21 dice rolls
    ↓
Opponent Node: MIN(opponent's best moves)
```

## Contributing

Contributions are welcome! Areas for enhancement:
- Race network integration (currently uses contact net as fallback)
- Opening book implementation
- Match equity table customization
- Performance optimizations
- Additional test coverage

## Acknowledgments

Built on the foundation of GNU Backgammon (GNUbg), one of the strongest backgammon programs in the world. This project implements the GNUbg evaluation methodology in C# for game development use cases.

## Support

For issues, questions, or feature requests, please open an issue on the project repository.
