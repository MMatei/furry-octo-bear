using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChineseCheckers
{
    class MonteCarloNodeScore : MonteCarloNode
    {
        public MonteCarloNodeScore(Board _board, MonteCarloNodeScore _parent, int parentPlayerIndex, bool debug) :
            base(_board, _parent, parentPlayerIndex, debug)
        {
            eps = 10;
        }

        // this function must be called on the root of the Monte Carlo Tree
        // it returns the most promising node to be explored
        public override MonteCarloNode select()
        {
            MonteCarloNodeScore node = this;
            // intoarcem primul nod care are copii neexplorati
            // daca dam peste un nod terminal, functia intoarce null
            // in idea ca in acel moment incheiem parcurgerea arborelui
            // (deoarece am ajuns la finalul caii cea mai promitatoare)
            while (node != null && node.unexploredActions.Count == 0)
            {
                // selectam cel mai promitator copil
                double maxScore = -1;
                MonteCarloNodeScore mostPromising = null;
                foreach (MonteCarloNodeScore child in node.children)
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
        public override MonteCarloNode expand()
        {
            Action a = unexploredActions[0];
            unexploredActions.RemoveAt(0);
            Board newBoard = new Board(board); // the new node represents a new board
            newBoard.movePiece(a.fromI, a.fromJ, a.toI, a.toJ, playerIndex); // with an action taken
            MonteCarloNodeScore child = new MonteCarloNodeScore(newBoard, this, playerIndex, false);
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
                    //int max = 0; // estimate the winner to be the player with max score accumulated
                    //for (int i = 1; i < Game1.numPlayers; i++)
                    //    if (accScore[max] < accScore[i])
                    //        max = i;
                    // TODO: for more than 2 players
                    return accScore[playerIndex] - accScore[(playerIndex + 1)%2];
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
            if (testBoard.hasWon(playerIndex))
            {
                //Console.WriteLine(accScore[playerIndex] - accScore[(playerIndex + 1) % 2]);
                return 100;
            }
            return -100;
        }

        // called on the node on which we made the playout
        // we will increment the nr of total games and victories for parent nodes
        public override void backpropagation(int score)
        {
            MonteCarloNode node = this;
            totalGames = score;
            while (node.parent != null)
            {
                node = node.parent;
                if(node.playerIndex != playerIndex)
                { // opponent seeks to minimise our gain
                    int min = 99999;
                    foreach (MonteCarloNode n in node.children)
                        if (min > n.totalGames)
                            min = n.totalGames;
                    node.totalGames = min;
                }
                else
                { // we seek to maximise our gain
                    int max = -99999;
                    foreach (MonteCarloNode n in node.children)
                        if (max < n.totalGames)
                            max = n.totalGames;
                    node.totalGames = max;
                }
            }
        }

        // called on the root of the tree; it will return the board of the most
        // promising child
        public override Board getBestResult()
        {
            double maxScore = -10000;
            MonteCarloNode mostPromising = null;
            foreach (MonteCarloNode child in children)
            {
                Console.WriteLine(child.totalGames);
                if (child.totalGames > maxScore)
                {
                    maxScore = child.totalGames;
                    mostPromising = child;
                }
            }
            Console.WriteLine();
            return mostPromising.board;
        }
    }
}
