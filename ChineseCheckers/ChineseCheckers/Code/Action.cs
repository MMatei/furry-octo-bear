using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChineseCheckers
{
    class Action
    {
        private const int k = 5; // k-best pruning

        public int fromI, fromJ, toI, toJ;
        public int score; // optional - used for k-best pruning
        public Action(int _fromI, int _fromJ, int _toI, int _toJ)
        {
            fromI = _fromI;
            fromJ = _fromJ;
            toI = _toI;
            toJ = _toJ;
        }

        public static List<Action> getActions(Board board, int playerIndex)
        {
            List<Action> unexploredActions = new List<Action>();
            byte[] pieces = board.getPiecePos()[playerIndex];
            for (int i = 0; i < pieces.Length; i += 2)
            {
                int fromI = pieces[i];
                int fromJ = pieces[i + 1];
                LinkedList<int> validMoves = board.getValidMoves(fromI, fromJ);
                while (validMoves.Count > 0)
                {
                    int toI = validMoves.First.Value;
                    validMoves.RemoveFirst();
                    int toJ = validMoves.First.Value;
                    validMoves.RemoveFirst();
                    Action a = new Action(fromI, fromJ, toI, toJ);
                    unexploredActions.Add(a);
                }
            }
            return unexploredActions;
        }

        public static List<Action> getActionsPruned(Board board, int playerIndex, AI ai)
        {
            List<Action> unexploredActions = new List<Action>();
            byte[] pieces = board.getPiecePos()[playerIndex];
            for (int i = 0; i < pieces.Length; i += 2)
            {
                int fromI = pieces[i];
                int fromJ = pieces[i + 1];
                LinkedList<int> validMoves = board.getValidMoves(fromI, fromJ);
                while (validMoves.Count > 0)
                {
                    int toI = validMoves.First.Value;
                    validMoves.RemoveFirst();
                    int toJ = validMoves.First.Value;
                    validMoves.RemoveFirst();
                    Action a = new Action(fromI, fromJ, toI, toJ);
                    // we will sort actions based on their score, so that we
                    // can perform k-best pruning
                    a.score = ai.score(a, playerIndex);
                    unexploredActions.Add(a);
                }
            }
            unexploredActions.Sort(new Comparator());
            int count = unexploredActions.Count - k;
            if (count > 0 /*&& unexploredActions[0].score > 0*/)
                unexploredActions.RemoveRange(k, count);
            return unexploredActions;
        }

        private class Comparator : Comparer<Action>
        {
            public override int Compare(Action a1, Action a2)
            {
                return a2.score - a1.score;
            }
        }

        public override String ToString()
        {
            return "(" + fromI + "," + fromJ + ")->(" + toI + "," + toJ + ")";
        }
    }
}
