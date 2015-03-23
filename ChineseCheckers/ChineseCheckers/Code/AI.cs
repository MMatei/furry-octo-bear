using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ChineseCheckers
{
    class AI
    {
        // 1 second thinking time
        private const long THINK_TIME = 3000;
        private static Thread thread;
        private bool running = true;

        public int thinking = 0; // 0 - not thinking, 1 - thinking, 2 - done thinking
        // when 0 is set, the input is stored in board
        // when 2 is set, the result is stored in board
        public Board board;
        public int playerIndex;

        public static AI startAIThread()
        {
            if (thread != null) // prevent multiple call
                return null;
            AI ai = new AI();
            thread = new Thread(new ThreadStart(ai.execute));
            thread.Start();
            return ai;
        }

        private static long currentTimeMillis()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }
    
        private Board getAIMove()
        {
            // initialize Monte Carlo Tree
            // the root must be the previous player, so that its children
            //  represent the actions of the current player
            playerIndex = playerIndex == 0 ? (Game1.numPlayers - 1) : (playerIndex - 1);
            MonteCarloNode tree = new MonteCarloNode(board, null, playerIndex);
            // we are going to run the MCTS algorithm until it either stops
            //  (it has reached a final state)
            // or until a time limit has expired
            long stopTime = currentTimeMillis() + THINK_TIME;
            long nodesExplored = 0;
            while(currentTimeMillis() < stopTime) {
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

        public void think(Board board, int playerIndex)
        {
            this.board = board;
            this.playerIndex = playerIndex;
            thinking = 1;
            lock (this) {
                Monitor.Pulse(this);// wake up the thread
            }
        }

        private void execute()
        {
            while (running)
            {
                lock (this) {
                    Monitor.Wait(this);// sleep until work is available
                }
                board = getAIMove();
                thinking = 2;
            }
        }

        public void stop()
        {
            running = false;
            thread.Join();
        }
    }
}
