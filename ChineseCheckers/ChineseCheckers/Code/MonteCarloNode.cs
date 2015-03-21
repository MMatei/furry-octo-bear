using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChineseCheckers
{
    class MonteCarloNode
    {
        // constanta de explorare
        private static double C = Math.Sqrt(2);
        // epsilon - the percentage chance we select a random move in playout
        // it is, in fact, indispensible in the good functioning of a playout
        private static double eps = 5;
        private static Random rand = new Random();

        private int playerIndex;
        private MonteCarloNode parent;
        private Board board; // starea jocului la momentul reprezentat de acest nod
        private LinkedList<MonteCarloNode> children;
        private List<Action> unexploredActions;
        private int victories = 0; // numarul de jocuri castigate pornind din acest nod
        private int totalGames = 0; // numarul de jocuri jucate porinind din acest nod
        private int timesVisited = 1;// numarul de vizitari ale nodului

        public MonteCarloNode(Board _board, MonteCarloNode _parent, int parentPlayerIndex)
        {
            board = _board;
            parent = _parent;
            // nodul fiu va reprezenta mutarile jucatorului urmator
            playerIndex = (parentPlayerIndex + 1) % Game1.numPlayers;
            children = new LinkedList<MonteCarloNode>();
            unexploredActions = Action.getActions(board, playerIndex);
        }

        // this function must be called on the root of the Monte Carlo Tree
        // it returns the most promising node to be explored
        public MonteCarloNode select()
        {
            MonteCarloNode node = this;
            // intoarcem primul nod care are copii neexplorati
            // daca dam peste un nod terminal, functia intoarce null
            // in idea ca in acel moment incheiem parcurgerea arborelui
            // (deoarece am ajuns la finalul caii cea mai promitatoare)
            while(node != null && node.unexploredActions.Count == 0) {
                // selectam cel mai promitator copil
                double maxScore = -1;
                MonteCarloNode mostPromising = null;
                foreach(MonteCarloNode child in node.children) {
                    double score = child.victories;
                    score /= child.totalGames;
                    score += C * Math.Sqrt(Math.Log(node.timesVisited)/child.timesVisited);
                    if (score > maxScore) {
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
        public MonteCarloNode expand()
        {
            Action a = unexploredActions[0];
            unexploredActions.RemoveAt(0);
            Board newBoard = new Board(board); // the new node represents a new board
            newBoard.movePiece(a.fromI, a.fromJ, a.toI, a.toJ, playerIndex); // with an action taken
            MonteCarloNode child = new MonteCarloNode(newBoard, this, playerIndex);
            children.AddLast(child);
            return child;
        }

        // play the game starting from this node, and return true if the game was won
        public bool playout()
        {
            Board testBoard = new Board(board);
            int pi = playerIndex;
            while(!testBoard.hasWon(pi)) {
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
                        int h = Board.score(a, pi);
                        if (h > score)
                        {
                            score = h;
                            bestMove = a;
                        }
                    }
                    //Console.WriteLine(score + " " + bestMove + " " + pi);
                }
                // for random playout : bestMove = moves.get(rand.nextInt(moves.size()));
                testBoard.movePiece(bestMove.fromI, bestMove.fromJ, bestMove.toI, bestMove.toJ, pi);
                pi = (pi + 1) % Game1.numPlayers;// each player moves in turn
            }
            return testBoard.hasWon(playerIndex);
        }

        // called on the node on which we made the playout
        // we will increment the nr of total games and victories for parent nodes
        public void backpropagation(bool victory)
        {
            MonteCarloNode node = this;
            totalGames = 1;
            if (victory)
                victories = 1;
            while (node.parent != null)
            {
                node.parent.totalGames++;
                if (victory)
                    node.parent.victories++;
                node = node.parent;
            }
        }

        // called on the root of the tree; it will return the board of the most
        // promising child
        public Board getBestResult()
        {
            double maxScore = -1;
            MonteCarloNode mostPromising = null;
            foreach(MonteCarloNode child in children) {
                double score = child.victories;
                score /= child.totalGames;
                if (score > maxScore) {
                    maxScore = score;
                    mostPromising = child;
                }
            }
            return mostPromising.board;
        }
    }
}
