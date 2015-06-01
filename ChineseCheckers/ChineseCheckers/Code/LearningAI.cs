﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChineseCheckers
{
    class LearningAI : AI
    {
        private const long THINK_TIME = 10000;

        protected override Board getAIMove()
        {
            MonteCarloNodeEvalHist.AIPlayerIndex = playerIndex;
            MonteCarloNodeEvalHist.ai = this;
            // initialize Monte Carlo Tree
            // the root must be the previous player, so that its children
            //  represent the actions of the current player
            playerIndex = playerIndex == 0 ? (Game1.numPlayers - 1) : (playerIndex - 1);
            MonteCarloNodeEvalHist tree = new MonteCarloNodeEvalHist(null, board, null, playerIndex, true);
            // we are going to run the MCTS algorithm until it either stops
            //  (it has reached a final state)
            // or until a time limit has expired
            long stopTime = currentTimeMillis() + THINK_TIME;
            long nodesExplored = 0;
            while (currentTimeMillis() < stopTime) {
                MonteCarloNode nodeToExpand = tree.select();
                if (nodeToExpand == null) {
                    Console.WriteLine("node to expand is null");
                    break;
                }
                nodeToExpand = nodeToExpand.expand();
                nodeToExpand.backpropagation(nodeToExpand.playout());
                nodesExplored++;
            }
            Console.WriteLine("explored " + nodesExplored + " nodes ");
            tree = tree.getBestResult();
            Game1.aiActions[MonteCarloNodeEvalHist.AIPlayerIndex].Add(tree.act);
            return tree.board;
        }
    }
}
