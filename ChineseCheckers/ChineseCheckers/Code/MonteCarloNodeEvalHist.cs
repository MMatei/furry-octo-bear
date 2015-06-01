using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChineseCheckers
{
    class MonteCarloNodeEvalHist : MonteCarloNode
    {
        internal Action act;

        public MonteCarloNodeEvalHist(Action a, Board _board, MonteCarloNode _parent, int parentPlayerIndex, bool debug) :
            base(_board, _parent, parentPlayerIndex, debug)
        {
            eps = 0;
            act = a;
        }

        // this function must be called on the root of the Monte Carlo Tree
        // it returns the most promising node to be explored
        public override MonteCarloNode select()
        {
            MonteCarloNodeEvalHist node = this;
            // intoarcem primul nod care are copii neexplorati
            // daca dam peste un nod terminal, functia intoarce null
            // in idea ca in acel moment incheiem parcurgerea arborelui
            // (deoarece am ajuns la finalul caii cea mai promitatoare)
            int pi = AIPlayerIndex;
            while (node != null && node.unexploredActions.Count == 0)
            {
                // selectam cel mai promitator copil
                double maxScore = -1;
                MonteCarloNodeEvalHist mostPromising = null;
                foreach (MonteCarloNodeEvalHist child in node.children)
                {
                    int actionScore;
                    Game1.aiHistory[pi].TryGetValue(child.act, out actionScore);
                    double score = child.victories;
                    score /= child.totalGames;
                    score += C * Math.Sqrt(Math.Log(node.timesVisited) / child.timesVisited);
                    score += W * actionScore;
                    if (score > maxScore)
                    {
                        maxScore = score;
                        mostPromising = child;
                    }
                }
                node.timesVisited++;
                node = mostPromising;
            }
            return node;
        }

        // expand the first action in unexploredActions into a child node
        public override MonteCarloNode expand()
        {
            Action a = unexploredActions[0];
            unexploredActions.RemoveAt(0);
            Board newBoard = new Board(board); // the new node represents a new board
            newBoard.movePiece(a.fromI, a.fromJ, a.toI, a.toJ, playerIndex); // with an action taken
            MonteCarloNodeEvalHist child = new MonteCarloNodeEvalHist(a, newBoard, this, playerIndex, false);
            children.AddLast(child);
            return child;
        }

        private int[] accScore = new int[Game1.numPlayers]; // static alloc to save some time in playout
        public override int playout()
        {
            Board testBoard = new Board(board);
            int pi = playerIndex;
            int turns = Game1.numPlayers * 5; // 5 turns per player
            for (int i = 0; i < Game1.numPlayers; i++)
                accScore[i] = 0;
            while (!testBoard.hasWon(pi))
            {
                // play only for a limited time; when that time expires, make a quick evaluation
                // of the board to determine the winner
                if (turns == 0)
                {
                    int max = 0; // estimate the winner to be the player with max score accumulated
                    for (int i = 1; i < Game1.numPlayers; i++)
                        if (accScore[max] < accScore[i])
                            max = i;
                    return Convert.ToInt32(max == AIPlayerIndex);
                }
                List<Action> moves = Action.getActions(testBoard, pi, ai);
                if (moves.Count == 0)
                    return 0; // loss
                Action bestMove = null;
                int r = rand.Next(101); // there's a chance to choose a random action
                if (r < eps) // we do this to spice things up and avoid local optima
                    bestMove = moves[rand.Next(moves.Count)];
                else
                { // choose the move with the longest path
                    int score = -100;
                    foreach (Action a in moves)
                    {
                        if (a.score > score)
                        {
                            score = a.score;
                            bestMove = a;
                        }
                    }
                }
                testBoard.movePiece(bestMove.fromI, bestMove.fromJ, bestMove.toI, bestMove.toJ, pi);
                accScore[pi] += bestMove.score; // keep track of the score each player racks
                pi = (pi + 1) % Game1.numPlayers;// each player moves in turn
                turns--;
            }
            return Convert.ToInt32(testBoard.hasWon(AIPlayerIndex));
        }

        // called on the root of the tree; it will return the board of the most
        // promising child
        public MonteCarloNodeEvalHist getBestResult()
        {
            double maxScore = -1;
            MonteCarloNodeEvalHist mostPromising = null;
            foreach (MonteCarloNodeEvalHist child in children) {
                double score = child.victories;
                score /= child.totalGames;
                Console.WriteLine(child.victories + " " + child.totalGames + " " + score);
                if (score > maxScore) {
                    maxScore = score;
                    mostPromising = child;
                }
            }
            Console.WriteLine();
            return mostPromising;
        }
    }
}
