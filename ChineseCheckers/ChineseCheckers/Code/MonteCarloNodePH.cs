using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChineseCheckers
{
    // using Progressive History
    class MonteCarloNodePH
    {
        private class Pair
        {
            public int wins, total;
        }
        private static Dictionary<Action, Pair> progHist; // progressive history
        // constanta de explorare
        protected static double C = 2;
        protected static double W = 5; // contributia istoriei progresive 10 is best so far for 15s (8-2)
        // for 5s 5 is best, with 7-3
        protected static Random rand = new Random();

        public static int AIPlayerIndex;// the index of the AI player on whose behalf we are running this scheme
        public static AI ai;

        private Action act; // the action that spawned this node
        internal int playerIndex;
        internal MonteCarloNodePH parent;
        internal Board board; // starea jocului la momentul reprezentat de acest nod
        internal LinkedList<MonteCarloNodePH> children;
        internal List<Action> unexploredActions;
        internal int victories = 0; // numarul de jocuri castigate pornind din acest nod
        internal int totalGames = 0; // numarul de jocuri jucate porinind din acest nod

        public MonteCarloNodePH(Board _board, MonteCarloNodePH _parent, int parentPlayerIndex, Action action)
        {
            act = action;
            board = _board;
            parent = _parent;
            // nodul fiu va reprezenta mutarile jucatorului urmator
            playerIndex = (parentPlayerIndex + 1) % Game1.numPlayers;
            children = new LinkedList<MonteCarloNodePH>();
            // Daca jucatorul curent a castigat jocul, nodul este terminal (n-avem ce explora mai departe)
            // altfel, detaliem actiunile posibile pt jucatorul curent => noduri copil posibile
            // luam in calcul doar cele mai bune beta actiuni, pt a elimina din noduri
            // (factor de ramificare imens + viteza mica de explorare == dezastru)
            if (!board.hasWon(playerIndex))
                unexploredActions = Action.getActionsPruned(board, playerIndex, ai);
            else {
                unexploredActions = new List<Action>();
                if (playerIndex == AIPlayerIndex) {
                    // This node represents a victory by me; implicitly assign to it a high score
                    // in order to give more weight to the chain of actions leading here
                    victories = 1000;
                    totalGames = 1000;
                    MonteCarloNodePH node = this; // backpropagate
                    while (node.parent != null) {
                        node.parent.totalGames += 1000;
                        node.parent.victories += 1000;
                        node = node.parent;
                    }
                }
            }
        }

        // special constructor for root; no pruning because:
        // a) we can explore all the relevant options
        // b) this is paramount for the first move, where there are many moves of equal value
        // while the difference between the two constructors could be summed up with just an if
        // this option is faster (because that if would be needlessly check thousands of times)
        public MonteCarloNodePH(Board _board, MonteCarloNodePH _parent, int parentPlayerIndex,
            Action action, bool root)
        {
            progHist = new Dictionary<Action, Pair>(new Action.Comparator()); // created upon root creation
            act = action;
            board = _board;
            parent = _parent;
            playerIndex = (parentPlayerIndex + 1) % Game1.numPlayers;
            children = new LinkedList<MonteCarloNodePH>();
            if (!board.hasWon(playerIndex))
                unexploredActions = Action.getActionsPrunedRoot(board, playerIndex, ai);
            else {
                unexploredActions = new List<Action>();
                if (playerIndex == AIPlayerIndex) {
                    victories = 1000;
                    totalGames = 1000;
                    MonteCarloNodePH node = this;
                    while (node.parent != null) {
                        node.parent.totalGames += 1000;
                        node.parent.victories += 1000;
                        node = node.parent;
                    }
                }
            }
            // debug
            foreach(Action a in unexploredActions)
                Console.WriteLine(a);
        }

        // this function must be called on the root of the Monte Carlo Tree
        // it returns the most promising node to be explored
        public MonteCarloNodePH select()
        {
            MonteCarloNodePH node = this;
            // intoarcem primul nod care are copii neexplorati
            // daca dam peste un nod terminal, functia intoarce null
            // in idea ca in acel moment incheiem parcurgerea arborelui
            // (deoarece am ajuns la finalul caii cea mai promitatoare)
            while(node != null && node.unexploredActions.Count == 0) {
                // selectam cel mai promitator copil
                double maxScore = -1;
                MonteCarloNodePH mostPromising = null;
                foreach(MonteCarloNodePH child in node.children) {
                    double score = child.victories;
                    score /= child.totalGames;
                    score += C * Math.Sqrt(Math.Log(node.totalGames)/child.totalGames);
                    Pair history;
                    progHist.TryGetValue(child.act, out history);
                    if (history != null)
                    {
                        double sana = history.wins;
                        sana /= history.total;
                        sana *= W;
                        sana /= (child.totalGames - child.victories + 1);
                        score += sana;
                    }
                    if (score > maxScore) {
                        maxScore = score;
                        mostPromising = child;
                    }
                }
                node = mostPromising;
            }
            return node;
        }

        // expand the first action in unexploredActions into a child node
        public MonteCarloNodePH expand()
        {
            Action a = unexploredActions[0];
            unexploredActions.RemoveAt(0);
            Board newBoard = new Board(board); // the new node represents a new board
            newBoard.movePiece(a.fromI, a.fromJ, a.toI, a.toJ, playerIndex); // with an action taken
            MonteCarloNodePH child = new MonteCarloNodePH(newBoard, this, playerIndex, a);
            children.AddLast(child);
            return child;
        }

        // play the game starting from this node, and return true if the game was won
        // keep all actions in playoutActions; these will then be added to dictionary
        // so that we can keep our progressive history - what actions won us the game
        private static List<Action> playoutActions = new List<Action>();
        public int playout()
        {
            Board testBoard = new Board(board);
            int pi = playerIndex;
            while(!testBoard.hasWon(pi)) {
                List<Action> moves = Action.getActions(testBoard, pi, ai);
                if (moves.Count == 0)
                    return 0; // loss
                Action bestMove = null;
                int r = rand.Next(101); // there's a chance to choose a random action
                if (r < ai.eps) // we do this to spice things up and avoid local optima
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
                playoutActions.Add(bestMove);
                testBoard.movePiece(bestMove.fromI, bestMove.fromJ, bestMove.toI, bestMove.toJ, pi);
                pi = (pi + 1) % Game1.numPlayers;// each player moves in turn
            }
            int victory = Convert.ToInt32(testBoard.hasWon(AIPlayerIndex));
            foreach (Action a in playoutActions) { // update progressive history
                Pair p;
                progHist.TryGetValue(a, out p);
                if (p == null) {
                    p = new Pair();
                }
                p.wins += victory;
                p.total++;
                progHist[a] = p;
            }
            playoutActions.Clear();
            return victory;
        }

        // called on the node on which we made the playout
        // we will increment the nr of total games and victories for parent nodes
        public void backpropagation(int victory)
        {
            MonteCarloNodePH node = this;
            totalGames = 1;
            victories = victory;
            while (node.parent != null)
            {
                node.parent.totalGames++;
                node.parent.victories += victory;
                node = node.parent;
            }
        }

        // called on the root of the tree; it will return the board of the most
        // promising child
        public virtual Board getBestResult()
        {
            double maxScore = -1;
            MonteCarloNodePH mostPromising = null;
            foreach(MonteCarloNodePH child in children) {
                double score = child.victories;
                score /= child.totalGames;
                Console.WriteLine(child.victories+" "+child.totalGames+" "+score);
                if (score > maxScore) {
                    maxScore = score;
                    mostPromising = child;
                }
            }
            Console.WriteLine();
            return mostPromising.board;
        }
    }
}
