using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChineseCheckers
{
    /// <summary>
    /// I called it American AI, because no one gets left behind.
    /// The heuristic is modified to keep moving the pieces behind and thus keep them all together.
    /// This is an important factor in winning at this game.
    /// </summary>
    class AmericanAI : AI
    {
        // The difference from normal score is that the distance from the original position to
        // the goal is counted twice. This is done in order to make moving pieces further behind
        // more favorable.
        public override int score(Action a, int playerIndex)
        {
            int pi_2 = playerIndex + playerIndex;
            int from = Board.h(a.fromI, a.fromJ, playerGoal[pi_2], playerGoal[pi_2 + 1]);
            return from * (from -
                Board.h(a.toI, a.toJ, playerGoal[pi_2], playerGoal[pi_2 + 1]));
        }
    }
}
