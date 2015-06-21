using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChineseCheckers
{
    class MonteCarloNodeScore2
    {
        // constanta de explorare
        protected static double C = 64;//Math.Sqrt(2);
        protected static Random rand = new Random();

        public static int AIPlayerIndex;// the index of the AI player on whose behalf we are running this scheme
        public static AI ai;

        internal Action action;
        internal int playerIndex;
        internal MonteCarloNodeScore2 parent;
        internal Board board; // starea jocului la momentul reprezentat de acest nod
        internal LinkedList<MonteCarloNodeScore2> children;
        internal List<Action> unexploredActions;
        internal int[] score;
        internal int timesVisited = 1;
        internal int turn; // tura (fata de cea a radacinii) pe care o reprez acest nod

        public MonteCarloNodeScore2(Board _board, MonteCarloNodeScore2 _parent, int tur,
            int parentPlayerIndex, Action a)
        {
            action = a;
            turn = tur + 1;
            score = new int[Game1.numPlayers];
            board = _board;
            parent = _parent;
            // nodul fiu va reprezenta mutarile jucatorului urmator
            playerIndex = (parentPlayerIndex + 1) % Game1.numPlayers;
            children = new LinkedList<MonteCarloNodeScore2>();
            // Daca jucatorul curent a castigat jocul, nodul este terminal (n-avem ce explora mai departe)
            // altfel, detaliem actiunile posibile pt jucatorul curent => noduri copil posibile
            // luam in calcul doar cele mai bune beta actiuni, pt a elimina din noduri
            // (factor de ramificare imens + viteza mica de explorare == dezastru)
            if (!board.hasWon(playerIndex))
                unexploredActions = Action.getActionsPruned(board, playerIndex, ai);
            else {
                unexploredActions = new List<Action>();
                for (int i = 0; i < Game1.numPlayers; i++)
                    score[i] = -100;
                score[playerIndex] = 200 - turn / Game1.numPlayers;
                //Console.WriteLine("won "); // maybe keep searching even after bottom reached?
                backpropagation();
            }
        }

        public MonteCarloNodeScore2(Board _board, MonteCarloNodeScore2 _parent, int tur,
            int parentPlayerIndex, bool debug)
        {
            turn = tur + 1;
            score = new int[Game1.numPlayers];
            board = _board;
            parent = _parent;
            // nodul fiu va reprezenta mutarile jucatorului urmator
            playerIndex = (parentPlayerIndex + 1) % Game1.numPlayers;
            children = new LinkedList<MonteCarloNodeScore2>();
            // Daca jucatorul curent a castigat jocul, nodul este terminal (n-avem ce explora mai departe)
            // altfel, detaliem actiunile posibile pt jucatorul curent => noduri copil posibile
            // luam in calcul doar cele mai bune beta actiuni, pt a elimina din noduri
            // (factor de ramificare imens + viteza mica de explorare == dezastru)
            if (!board.hasWon(playerIndex))
                unexploredActions = Action.getActionsPrunedRoot(board, playerIndex, ai);
            else {
                unexploredActions = new List<Action>();
                for (int i = 0; i < Game1.numPlayers; i++)
                    score[i] = -100;
                score[playerIndex] = 200 - turn / Game1.numPlayers;
                //Console.WriteLine("won "); // maybe keep searching even after bottom reached?
                backpropagation();
            }
            if (debug)
                foreach (Action a in unexploredActions)
                    Console.WriteLine(a);
        }

        // this function must be called on the root of the Monte Carlo Tree
        // it returns the most promising node to be explored
        public MonteCarloNodeScore2 select()
        {
            MonteCarloNodeScore2 node = this;
            // intoarcem primul nod care are copii neexplorati
            // daca dam peste un nod terminal, functia intoarce null
            // in idea ca in acel moment incheiem parcurgerea arborelui
            // (deoarece am ajuns la finalul caii cea mai promitatoare)
            while (node != null && node.unexploredActions.Count == 0) {
                // selectam cel mai promitator copil
                double maxScore = -9999;
                MonteCarloNodeScore2 mostPromising = null;
                int count = node.children.Count;
                int i = count;
                foreach (MonteCarloNodeScore2 child in node.children) {
                    // urmarim aceasi logica ca la backpropagation, urmarind mutarea
                    // cea mai avantajoasa pt player-ul curent
                    // (reprezentat de player index-ul copilulului;
                    // remember: nodul copacului reprezinta jucatorul precedent)
                    double score = child.score[child.playerIndex];
                    //score /= child.timesVisited;
                    int j = i / count;
                    //score += j * 10; // give more weight to 1st node
                    i--;
                    // the exploration component should perhaps be revised?
                    score += C * Math.Sqrt(Math.Log(node.timesVisited) / child.timesVisited);
                    //Console.Write(score + " ");
                    if (score > maxScore) {
                        maxScore = score;
                        mostPromising = child;
                    }
                }
                //Console.WriteLine();
                node = mostPromising;
            }
            return node;
        }

        // expand the first action in unexploredActions into a child node
        public MonteCarloNodeScore2 expand()
        {
            Action a = unexploredActions[0];
            unexploredActions.RemoveAt(0);
            Board newBoard = new Board(board); // the new node represents a new board
            newBoard.movePiece(a.fromI, a.fromJ, a.toI, a.toJ, playerIndex); // with an action taken
            MonteCarloNodeScore2 child = new MonteCarloNodeScore2(newBoard, this, turn, playerIndex, a);
            children.AddLast(child);
            return child;
        }

        public void playout()
        {
            Board testBoard = new Board(board);
            int pi = playerIndex;
            int turns = Game1.numPlayers * 10; // 5 turns per player
            while (!testBoard.hasWon(pi)) {
                if (turns == 0) {
                    byte[][] piecePos = testBoard.getPiecePos();
                    for (int i = 0; i < Game1.numPlayers; i++) {
                        int score = 0;
                        int pi_2 = i + i;
                        for (int k = 0; k < 10; k++)
                            score += ai.scoreHome(piecePos[i][k + k], piecePos[i][k + k + 1], pi_2);
                        // scorul scade pe masura ce jocul avanseaza in timp
                        // astfel incurajam victoriile rapide
                        this.score[i] = score - turn / Game1.numPlayers;
                    }
                    return;
                }
                List<Action> moves = Action.getActions(testBoard, pi, ai);
                if (moves.Count == 0) {
                    score[pi] = 0;//-100;
                    return; // loss
                }
                Action bestMove = null;
                //int r = rand.Next(101); // there's a chance to choose a random action
                //if (r < eps) // we do this to spice things up and avoid local optima
                //    bestMove = moves[rand.Next(moves.Count)];
                //else { // choose the move with the longest path
                    int bestScore = -100;
                    foreach (Action a in moves) {
                        if (a.score > bestScore) {
                            bestScore = a.score;
                            bestMove = a;
                        }
                    }
                //}
                testBoard.movePiece(bestMove.fromI, bestMove.fromJ, bestMove.toI, bestMove.toJ, pi);
                //score[pi] += bestMove.score; // keep track of the score each player racks
                pi = (pi + 1) % Game1.numPlayers;// each player moves in turn
                turns--;
            }
            // pi has won, adjust the scores accordingly
            //Console.Write("won ");
            byte[][] pp = testBoard.getPiecePos();
            for (int i = 0; i < Game1.numPlayers; i++) {
                int s = 0;
                int pi_2 = i + i;
                for (int k = 0; k < 10; k++)
                    s += ai.scoreHome(pp[i][k + k], pp[i][k + k + 1], pi_2);
                // scorul scade pe masura ce jocul avanseaza in timp
                // astfel incurajam victoriile rapide
                if (i != pi)
                    s -= 10;
                this.score[i] = s - turn / Game1.numPlayers;
            }
            //for (int i = 0; i < Game1.numPlayers; i++)
            //    score[i] = -100;
            //this.score[pi] = 200 - turn / Game1.numPlayers;
        }

        // called on the node on which we made the playout
        // we will adjust the score of our parents, to best reflect their interests
        // if my child has a better score, I copy it, to reflect the best possible move
        // that can be achieved through me
        public void backpropagation()
        {
            MonteCarloNodeScore2 node = parent;
            MonteCarloNodeScore2 nodeChild = this;
            while (node != null) {
                node.timesVisited++;
                if (node.score[node.playerIndex] < nodeChild.score[node.playerIndex])
                    for (int i = 0; i < Game1.numPlayers; i++)
                        node.score[i] = nodeChild.score[i];
                node = node.parent;
            }
        }

        // called on the root of the tree; it will return the board of the most
        // promising child
        public MonteCarloNodeScore2 getBestResult()
        {
            double maxScore = -10000;
            MonteCarloNodeScore2 mostPromising = null;
            int other = (AIPlayerIndex + 1) % Game1.numPlayers;
            int plus = 4;
            foreach (MonteCarloNodeScore2 child in children) {
                double score = child.score[AIPlayerIndex];//diferenta ruineaza algo
                int actionScore;
                Game1.aiHistory[AIPlayerIndex].TryGetValue(child.action, out actionScore);
                score += actionScore / 10;
                if (plus > 0)
                    score += plus;
                plus-=4;
                //score /= child.timesVisited;
                Console.Write(child.score[AIPlayerIndex] + "/" + child.timesVisited + "=" + score + " ");
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
