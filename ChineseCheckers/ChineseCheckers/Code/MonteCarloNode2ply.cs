using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChineseCheckers
{
    class MonteCarloNode2ply : MonteCarloNode
    {
        public MonteCarloNode2ply(Board _board, MonteCarloNode2ply _parent, int parentPlayerIndex, bool debug) :
            base(_board, _parent, parentPlayerIndex, debug)
        {
            eps = 20;
        }

        // this function must be called on the root of the Monte Carlo Tree
        // it returns the most promising node to be explored
        public MonteCarloNode2ply select()
        {
            MonteCarloNode2ply node = this;
            // intoarcem primul nod care are copii neexplorati
            // daca dam peste un nod terminal, functia intoarce null
            // in idea ca in acel moment incheiem parcurgerea arborelui
            // (deoarece am ajuns la finalul caii cea mai promitatoare)
            while (node != null && node.unexploredActions.Count == 0)
            {
                // selectam cel mai promitator copil
                double maxScore = -1;
                MonteCarloNode2ply mostPromising = null;
                foreach (MonteCarloNode2ply child in node.children)
                {
                    double score = child.victories;
                    score /= child.totalGames;
                    score += C * Math.Sqrt(Math.Log(node.timesVisited) / child.timesVisited);
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
        public MonteCarloNode2ply expand()
        {
            Action a = unexploredActions[0];
            unexploredActions.RemoveAt(0);
            Board newBoard = new Board(board); // the new node represents a new board
            newBoard.movePiece(a.fromI, a.fromJ, a.toI, a.toJ, playerIndex); // with an action taken
            MonteCarloNode2ply child = new MonteCarloNode2ply(newBoard, this, playerIndex, false);
            children.AddLast(child);
            return child;
        }

        public override bool playout()
        {
            Board testBoard = new Board(board);
            int pi = playerIndex;
            //int turns = 10;
            while (!testBoard.hasWon(pi))
            {
                // play only for a limited time; when that time expires, make a quick evaluation
                // of the board to determine the winner
                /*if (turns == 0)
                {
                    byte[][] piecePos = testBoard.getPiecePos();
                    int winner = 0, winnerScore = 9999; // smaller score is better
                    for (int i = 0; i < Game1.numPlayers; i++)
                    {
                        int score = 0;
                        int pi_2 = i + i;
                        for (int k = 0; k < 10; k++)
                            score += ai.score(piecePos[i][k + k], piecePos[i][k + k + 1], pi_2);
                        if (score < winnerScore)
                        {
                            winner = i;
                            winnerScore = score;
                        }
                    }
                    return winner == playerIndex;
                }*/
                int piP1 = (pi + 1) % Game1.numPlayers;
                List<Action> moves = Action.getActionsPruned(testBoard, pi, ai);
                Action bestMove = null;
                int r = rand.Next(101); // there's a chance to choose a random action
                if (r < eps) // we do this to spice things up and avoid local optima
                    bestMove = moves[rand.Next(moves.Count)];
                else
                { // look at the opponents possible actions
                    int maxScore = -1000;
                    foreach (Action a in moves)
                    {
                        int score = -100;
                        testBoard.movePiece(a.fromI, a.fromJ, a.toI, a.toJ, pi);
                        List<Action> moves2 = Action.getActions(testBoard, pi);
                        foreach (Action aa in moves2)
                        {
                            int h = ai.score(aa, piP1);
                            if (h > score)
                                score = h;
                        }
                        testBoard.movePiece(a.toI, a.toJ, a.fromI, a.fromJ, pi); // reverse move, to restore board
                        score = ai.score(a, pi) - score;
                        if (score > maxScore)
                        {
                            maxScore = score;
                            bestMove = a;
                        }
                    }
                }
                testBoard.movePiece(bestMove.fromI, bestMove.fromJ, bestMove.toI, bestMove.toJ, pi);
                pi = piP1;// each player moves in turn
                //turns--;
            }
            return testBoard.hasWon(playerIndex);
        }
    }
}