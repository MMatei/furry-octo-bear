using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChineseCheckers
{
    class MonteCarloNodeScore
    {
        // constanta de explorare
        protected static double C = Math.Sqrt(2);
        // epsilon - the percentage chance we select a random move in playout
        // it is, in fact, indispensible in the good functioning of a playout
        protected static double eps = 0;
        protected static Random rand = new Random();

        public static int AIPlayerIndex;// the index of the AI player on whose behalf we are running this scheme
        public static AI ai;

        internal int playerIndex;
        internal MonteCarloNodeScore parent;
        internal Board board; // starea jocului la momentul reprezentat de acest nod
        internal LinkedList<MonteCarloNodeScore> children;
        internal List<Action> unexploredActions;
        internal int[] score;
        internal int timesVisited = 1;

        public MonteCarloNodeScore(Board _board, MonteCarloNodeScore _parent, int parentPlayerIndex, bool debug)
        {
            score = new int[Game1.numPlayers];
            board = _board;
            parent = _parent;
            // nodul fiu va reprezenta mutarile jucatorului urmator
            playerIndex = (parentPlayerIndex + 1) % Game1.numPlayers;
            children = new LinkedList<MonteCarloNodeScore>();
            // Daca jucatorul curent a castigat jocul, nodul este terminal (n-avem ce explora mai departe)
            // altfel, detaliem actiunile posibile pt jucatorul curent => noduri copil posibile
            // luam in calcul doar cele mai bune beta actiuni, pt a elimina din noduri
            // (factor de ramificare imens + viteza mica de explorare == dezastru)
            if (!board.hasWon(playerIndex))
                unexploredActions = Action.getActionsPruned(board, playerIndex, ai);
            else
            {
                unexploredActions = new List<Action>();
                for (int i = 0; i < Game1.numPlayers; i++)
                    score[i] = -100;
                score[playerIndex] = 100;
                backpropagation();
            }
            if (debug)
                foreach (Action a in unexploredActions)
                    Console.WriteLine(a);
        }

        // this function must be called on the root of the Monte Carlo Tree
        // it returns the most promising node to be explored
        public MonteCarloNodeScore select()
        {
            MonteCarloNodeScore node = this;
            // intoarcem primul nod care are copii neexplorati
            // daca dam peste un nod terminal, functia intoarce null
            // in idea ca in acel moment incheiem parcurgerea arborelui
            // (deoarece am ajuns la finalul caii cea mai promitatoare)
            while (node != null && node.unexploredActions.Count == 0)
            {
                // selectam cel mai promitator copil
                double maxScore = -9999;
                MonteCarloNodeScore mostPromising = null;
                foreach (MonteCarloNodeScore child in node.children)
                {
                    // urmarim aceasi logica ca la backpropagation, urmarind mutarea
                    // cea mai avantajoasa pt player-ul curent
                    // (reprezentat de player index-ul copilulului;
                    // remember: nodul copacului reprezinta jucatorul precedent)
                    double score = this.score[child.playerIndex];
                    // the exploration component should perhaps be revised?
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
        public MonteCarloNodeScore expand()
        {
            Action a = unexploredActions[0];
            unexploredActions.RemoveAt(0);
            Board newBoard = new Board(board); // the new node represents a new board
            newBoard.movePiece(a.fromI, a.fromJ, a.toI, a.toJ, playerIndex); // with an action taken
            MonteCarloNodeScore child = new MonteCarloNodeScore(newBoard, this, playerIndex, false);
            children.AddLast(child);
            return child;
        }

        public void playout()
        {
            Board testBoard = new Board(board);
            int pi = playerIndex;
            int turns = Game1.numPlayers * 5; // 5 turns per player
            for (int i = 0; i < Game1.numPlayers; i++)
                score[i] = 0;
            while (!testBoard.hasWon(pi))
            {
                if (turns == 0)
                    return;
                List<Action> moves = Action.getActions(testBoard, pi, ai);
                if (moves.Count == 0)
                {
                    score[pi] = -100;
                    return; // loss
                }
                Action bestMove = null;
                int r = rand.Next(101); // there's a chance to choose a random action
                if (r < eps) // we do this to spice things up and avoid local optima
                    bestMove = moves[rand.Next(moves.Count)];
                else
                { // choose the move with the longest path
                    int bestScore = -100;
                    foreach (Action a in moves)
                    {
                        if (a.score > bestScore)
                        {
                            bestScore = a.score;
                            bestMove = a;
                        }
                    }
                }
                testBoard.movePiece(bestMove.fromI, bestMove.fromJ, bestMove.toI, bestMove.toJ, pi);
                score[pi] += bestMove.score; // keep track of the score each player racks
                pi = (pi + 1) % Game1.numPlayers;// each player moves in turn
                turns--;
            }
            // pi has won, adjust the scores accordingly
            for (int i = 0; i < Game1.numPlayers; i++)
                score[i] = -100;
            score[pi] = 100;
        }

        // called on the node on which we made the playout
        // we will adjust the score of our parents, to best reflect their interests
        public void backpropagation()
        {
            MonteCarloNodeScore node = parent;
            while (node != null)
            {
                // we select from our children the score tuple that best
                // serves our own interest
                /*int maxScore = -9999;
                MonteCarloNodeScore c = null;
                foreach (MonteCarloNodeScore child in node.children)
                {
                    if (child.score[node.playerIndex] > maxScore)
                    {
                        maxScore = child.score[node.playerIndex];
                        c = child;
                    }
                }
                if (c == null)
                    return;
                for (int i = 0; i < Game1.numPlayers; i++)
                    score[i] = c.score[i];*/
                for (int i = 0; i < Game1.numPlayers; i++)
                    node.score[i] += score[i];
                node = node.parent;
            }
        }

        // called on the root of the tree; it will return the board of the most
        // promising child
        public Board getBestResult()
        {
            double maxScore = -10000;
            MonteCarloNodeScore mostPromising = null;
            foreach (MonteCarloNodeScore child in children)
            {
                Console.Write(child.score[AIPlayerIndex]+" ");
                if (child.score[AIPlayerIndex] > maxScore)
                {
                    maxScore = child.score[AIPlayerIndex];
                    mostPromising = child;
                }
            }
            Console.WriteLine();
            return mostPromising.board;
        }
    }
}
