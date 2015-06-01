using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChineseCheckers
{
    class OtherAI : AI
    {
        private const long THINK_TIME = 10000;
        public OtherAI()
        {
            //k = 100;
        }

        protected override Board getAIMove()
        {
            MonteCarloNodeScore.AIPlayerIndex = playerIndex;
            MonteCarloNodeScore.ai = this;
            // initialize Monte Carlo Tree
            // the root must be the previous player, so that its children
            //  represent the actions of the current player
            playerIndex = playerIndex == 0 ? (Game1.numPlayers - 1) : (playerIndex - 1);
            MonteCarloNodeScore tree = new MonteCarloNodeScore(board, null, playerIndex, true);
            // we are going to run the MCTS algorithm until it either stops
            //  (it has reached a final state)
            // or until a time limit has expired
            long stopTime = currentTimeMillis() + THINK_TIME;
            long nodesExplored = 0;
            while (currentTimeMillis() < stopTime)
            {
                MonteCarloNodeScore nodeToExpand = tree.select();
                if (nodeToExpand == null)
                {
                    Console.WriteLine("node to expand is null");
                    break;
                }
                nodeToExpand = nodeToExpand.expand();
                nodeToExpand.playout();
                nodeToExpand.backpropagation();
                //nodeToExpand.backpropagation(nodeToExpand.playout()); // second playout does more harm
                nodesExplored++;
            }
            Console.WriteLine("explored " + nodesExplored + " nodes ");
            //Console.WriteLine(currentTimeMillis() - stopTime);
            return tree.getBestResult();
        }

        /*protected override Board getAIMove()
        {
            MonteCarloNodeEval.AIPlayerIndex = playerIndex;
            MonteCarloNodeEval.ai = this;
            // initialize Monte Carlo Tree
            // the root must be the previous player, so that its children
            //  represent the actions of the current player
            playerIndex = playerIndex == 0 ? (Game1.numPlayers - 1) : (playerIndex - 1);
            MonteCarloNodeEval tree = new MonteCarloNodeEval(board, null, playerIndex, true);
            // we are going to run the MCTS algorithm until it either stops
            //  (it has reached a final state)
            // or until a time limit has expired
            long stopTime = currentTimeMillis() +THINK_TIME;
            long nodesExplored = 0;
            while (currentTimeMillis() < stopTime)
            {
                MonteCarloNode nodeToExpand = tree.select();
                if (nodeToExpand == null)
                {
                    Console.WriteLine("node to expand is null");
                    break;
                }
                nodeToExpand = nodeToExpand.expand();
                nodeToExpand.backpropagation(nodeToExpand.playout());
                //nodeToExpand.backpropagation(nodeToExpand.playout()); // second playout does more harm
                nodesExplored++;
            }
            Console.WriteLine("explored " + nodesExplored + " nodes ");
            //Console.WriteLine(currentTimeMillis() - stopTime);
            return tree.getBestResult();
        }*/

        /*protected override Board getAIMove()
        {
            MonteCarloNode2ply.AIPlayerIndex = playerIndex;
            MonteCarloNode2ply.ai = this;
            // initialize Monte Carlo Tree
            // the root must be the previous player, so that its children
            //  represent the actions of the current player
            playerIndex = playerIndex == 0 ? (Game1.numPlayers - 1) : (playerIndex - 1);
            MonteCarloNode2ply tree = new MonteCarloNode2ply(board, null, playerIndex, true);
            // we are going to run the MCTS algorithm until it either stops
            //  (it has reached a final state)
            // or until a time limit has expired
            long stopTime = currentTimeMillis() + THINK_TIME;
            long nodesExplored = 0;
            while (currentTimeMillis() < stopTime)
            {
                MonteCarloNode2ply nodeToExpand = tree.select();
                if (nodeToExpand == null)
                {
                    Console.WriteLine("node to expand is null");
                    break;
                }
                nodeToExpand = nodeToExpand.expand();
                nodeToExpand.backpropagation(nodeToExpand.playout());
                nodeToExpand.backpropagation(nodeToExpand.playout());
                nodesExplored++;
            }
            Console.WriteLine("explored " + nodesExplored + " nodes ");
            return tree.getBestResult();
        }*/
    }
}
