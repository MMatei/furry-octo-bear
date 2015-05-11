﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChineseCheckers
{
    class MonteCarloNodeEval : MonteCarloNode
    {
        public MonteCarloNodeEval(Board _board, MonteCarloNode _parent, int parentPlayerIndex, bool debug) :
            base(_board, _parent, parentPlayerIndex, debug)
        {
            eps = 10;
        }

        // this function must be called on the root of the Monte Carlo Tree
        // it returns the most promising node to be explored
        public MonteCarloNodeEval select()
        {
            MonteCarloNodeEval node = this;
            // intoarcem primul nod care are copii neexplorati
            // daca dam peste un nod terminal, functia intoarce null
            // in idea ca in acel moment incheiem parcurgerea arborelui
            // (deoarece am ajuns la finalul caii cea mai promitatoare)
            while (node != null && node.unexploredActions.Count == 0)
            {
                // selectam cel mai promitator copil
                double maxScore = -1;
                MonteCarloNodeEval mostPromising = null;
                foreach (MonteCarloNodeEval child in node.children)
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
        public MonteCarloNodeEval expand()
        {
            Action a = unexploredActions[0];
            unexploredActions.RemoveAt(0);
            Board newBoard = new Board(board); // the new node represents a new board
            newBoard.movePiece(a.fromI, a.fromJ, a.toI, a.toJ, playerIndex); // with an action taken
            MonteCarloNodeEval child = new MonteCarloNodeEval(newBoard, this, playerIndex, false);
            children.AddLast(child);
            return child;
        }

        private int[] accScore = new int[Game1.numPlayers]; // static alloc to save some time in playout
        public override bool playout()
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
                    /*byte[][] piecePos = testBoard.getPiecePos();
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
                    return winner == playerIndex;*/
                    //Console.WriteLine("!"+accScore[0]+" "+accScore[1]);
                    int max = 0; // estimate the winner to be the player with max score accumulated
                    for (int i = 1; i < Game1.numPlayers; i++)
                        if (accScore[max] < accScore[i])
                            max = i;
                    return max == playerIndex;
                }
                List<Action> moves = Action.getActions(testBoard, pi);
                Action bestMove = null;
                int r = rand.Next(101); // there's a chance to choose a random action
                if (r < eps) // we do this to spice things up and avoid local optima
                    bestMove = moves[rand.Next(moves.Count)];
                else
                { // choose the move with the longest path
                    int score = -100;
                    foreach (Action a in moves)
                    {
                        int h = ai.score(a, pi);
                        if (h > score)
                        {
                            score = h;
                            bestMove = a;
                        }
                    }
                }
                testBoard.movePiece(bestMove.fromI, bestMove.fromJ, bestMove.toI, bestMove.toJ, pi);
                accScore[pi] += ai.score(bestMove, pi); // keep track of the score each player racks
                pi = (pi + 1) % Game1.numPlayers;// each player moves in turn
                turns--;
            }
            return testBoard.hasWon(playerIndex);
        }
    }
}
