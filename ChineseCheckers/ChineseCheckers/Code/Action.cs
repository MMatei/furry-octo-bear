using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChineseCheckers
{
    class Action
    {
        public int fromI, fromJ, toI, toJ;
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
                    unexploredActions.Add(new Action(fromI, fromJ, toI, toJ));
                }
            }
            return unexploredActions;
        }

        public override String ToString()
        {
            return "(" + fromI + "," + fromJ + ")->(" + toI + "," + toJ + ")";
        }
    }
}
