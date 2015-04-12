using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChineseCheckers
{
    /// <summary>
    /// This class contains all the information required to draw the pieces to the board,
    /// in effect translating the information found in Board into a more useful format.
    /// Furthermore, this class controls piece animations.
    /// </summary>
    class PiecesDraw
    {
        private static LinkedList<Int32> moveList;
        // x, y are the target coordinates of the moving rectangle
        // mp_i, mp_j identify the moving piece in pieceRect[][]
        // (for some reason, we can't simply obtain a reference to a rectangle,
        //  it just makes a copy)
        private static int x, y, mp_i, mp_j;
        // the rectangle's coordinates are measured in integer values
        // but we want to move them using a fractionary value (_dx, _dy)
        // therefore, _x and _y are the real values of the rectangle's position
        private static double _x, _y, _dx, _dy;

        internal static Rectangle[][] pieceRect;

        public static void createPieceRectangles(byte[][] piecePos, int numPlayers)
        {
            pieceRect = new Rectangle[numPlayers][];
            for (int i = 0; i < numPlayers; i++)
            {
                pieceRect[i] = new Rectangle[10];
                for (int j = 0; j < 10; j++)
                {
                    int pi = piecePos[i][j+j];
                    int pj = piecePos[i][j+j+1];
                    int x = 22 + pj * 60 + (pi % 2) * 30;
                    int y = 15 + pi * 52;
                    pieceRect[i][j] = new Rectangle(x, y, 52, 52);
                }
            }
        }

        public static void createAnimation(LinkedList<Int32> moveList)
        {
            PiecesDraw.moveList = moveList;
            int lin = moveList.First.Value;
            moveList.RemoveFirst();
            int col = moveList.First.Value;
            moveList.RemoveFirst();
            _x = x = 22 + col * 60 + (lin % 2) * 30;
            _y = y = 15 + lin * 52;
            for (int i = 0; i < pieceRect.Length; i++)
                for (int j = 0; j < 10; j++)
                    if (pieceRect[i][j].X == x && pieceRect[i][j].Y == y)
                    {
                        mp_i = i;
                        mp_j = j;
                        return;
                    }
        }

        public static bool isAnimationDone()
        {
            return moveList == null;
        }

        public static void update(Game1 game)
        {
            if (pieceRect[mp_i][mp_j].X == x && pieceRect[mp_i][mp_j].Y == y)
            {
                if (moveList.First == null)
                {
                    moveList = null;
                    game.nextPlayer(); // activate next player when animation done
                    return;
                }
                int lin = moveList.First.Value;
                moveList.RemoveFirst();
                int col = moveList.First.Value;
                moveList.RemoveFirst();
                x = 22 + col * 60 + (lin % 2) * 30;
                y = 15 + lin * 52;
                _dx = (x - _x) / 60;
                _dy = (y - _y) / 60;
            }
            // inch the rectangle closer to its destination
            _x += _dx;
            _y += _dy;
            pieceRect[mp_i][mp_j].X = (int)_x;
            pieceRect[mp_i][mp_j].Y = (int)Math.Round(_y);
        }

        /// <summary>
        /// This function draws the moving piece; why? because we'd like it to be drawn over
        /// all other balls. The simplest way to do this is to draw it last.
        /// </summary>
        /// <param name="sb">The spriteBatch used to draw</param>
        /// <param name="balls">The array containing the ball textures</param>
        public static void drawMovingPiece(SpriteBatch sb, Texture2D[] balls)
        {
            // remember that mp_i <=> player
            if (moveList != null)
                sb.Draw(balls[mp_i], pieceRect[mp_i][mp_j], Color.White);
        }
    }
}
