using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ChineseCheckers
{
    /// <summary>
    /// This is the base AI class. Extend this for fancier functionality.
    /// The least one ought to overwrite is the score function (== the heuristic used for actions).
    /// </summary>
    class AI
    {
        public int k = 5; // for k-best pruning
        private const long THINK_TIME = 3000;
        private static Thread thread;

        public static int thinking = 0; // 0 - not thinking, 1 - thinking, 2 - done thinking
        // when 0 is set, the input is stored in board
        // when 2 is set, the result is stored in board
        public static Board board;
        public static int playerIndex;

        public static void startAIThread()
        {
            if (thread != null) // prevent multiple call
                return;
            thread = new Thread(new ThreadStart(AI.execute));
            thread.Start();
        }

        protected static long currentTimeMillis()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }
    
        protected virtual Board getAIMove()
        {
            MonteCarloNode.AIPlayerIndex = playerIndex;
            MonteCarloNode.ai = this;
            // initialize Monte Carlo Tree
            // the root must be the previous player, so that its children
            //  represent the actions of the current player
            playerIndex = playerIndex == 0 ? (Game1.numPlayers - 1) : (playerIndex - 1);
            MonteCarloNode tree = new MonteCarloNode(board, null, playerIndex, true);
            // we are going to run the MCTS algorithm until it either stops
            //  (it has reached a final state)
            // or until a time limit has expired
            long stopTime = currentTimeMillis() + THINK_TIME;
            long nodesExplored = 0;
            while (/*currentTimeMillis() < stopTime*/ nodesExplored < 10000)
            {
                MonteCarloNode nodeToExpand = tree.select();
                if(nodeToExpand == null){
                    Console.WriteLine("node to expand is null");
                    break;
                }
                nodeToExpand = nodeToExpand.expand();
                nodeToExpand.backpropagation(nodeToExpand.playout());
                nodesExplored++;
            }
            Console.WriteLine("explored " + nodesExplored + " nodes ");
            return tree.getBestResult();
        }

        /// <summary>
        /// Computes the score of a given action. This score is the distance I've covered
        /// towards my end goal.
        /// </summary>
        public virtual int score(Action a, int playerIndex)
        {
            int pi_2 = playerIndex + playerIndex;
            return Board.h(a.fromI, a.fromJ, playerGoal[pi_2], playerGoal[pi_2 + 1]) -
                Board.h(a.toI, a.toJ, playerGoal[pi_2], playerGoal[pi_2 + 1]);
        }
        public virtual int score(int i, int j, int pi_2)
        {
            return Board.h(i, j, playerGoal[pi_2], playerGoal[pi_2 + 1]);
        }
        protected static int[] playerGoal = { 16, 6, 0, 6, 12, 12, 4, 0, 12, 0, 4, 12 };

        public void think(Board board, int playerIndex)
        {
            AI.board = board;
            AI.playerIndex = playerIndex;
            thinking = 1;
            lock (thread) {
                Monitor.Pulse(thread);// wake up the thread
            }
        }

        private static void execute()
        {
            while (true)
            {
                lock (thread) {
                    Monitor.Wait(thread);// sleep until work is available
                }
                board = Game1.isAI[playerIndex].getAIMove();
                thinking = 2;
            }
        }

        public void stop()
        {
            thread.Abort();
        }
    }
}
