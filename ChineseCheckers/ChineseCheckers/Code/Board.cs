using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Priority_Queue;

namespace ChineseCheckers
{
    /// <summary>
    /// This is the representation of a game state, containing all the neccessary
    /// information to draw this board to the screen and compute further moves.
    /// </summary>
    class Board
    {
        // a matrix detailing the board;
        // 255 means invalid position
        // 128 means empty position
        // 2 ^ n means player piece, where n = 0..5
        private byte[][] board;
        private byte[][] piecePos;// the positions of pieces
        // each line l contains the pieces of player l, the (i,j) coordinates written one after the other
        private static byte[][] tempBoard; // used in calculating jumps
        // since there's no reason no dynamically create/free it, we make it static
        // use the below to initialize it at the beggining of the game
        public static void initTempBoard()
        {
            tempBoard = new byte[19][];
            for (int k = 0; k < 19; k++)
            {
                tempBoard[k] = new byte[15];
                if (k == 0 || k == 18)
                    for (int l = 0; l < 15; l++)
                        tempBoard[k][l] = 255;
                else
                {
                    tempBoard[k][0] = 255;
                    tempBoard[k][14] = 255;
                }
            }
        }

        /**  0 1
         *  5 x 2  The directions around a hex; the following array lists the diff
         *   4 3    to be added to the (i,j) coord of x in order to travel that dir
         *  The first line is for odd-numbered lines
         *  The second line is for even-numbered lines (because of the nature of hex)
         */
        private static int[][] dirDiff = {
                                         new int[] { -1,0, -1,+1, 0,+1, +1,+1, +1,0, 0,-1},
                                         new int[] { -1,-1, -1,0, 0,+1, +1,0, +1,-1, 0,-1}
                                         };

        public Board(int numPlayers)
        {
            if (numPlayers < 2 || numPlayers > 6)
            {
                Console.WriteLine("Board: invalid numPlayers received " + numPlayers);
                return;
            }
            board = new byte[17][];
            for (int i = 0; i < 17; i++)
                board[i] = new byte[13];
            // initialize board -- first, the impassable
            board[0][0] = 255; board[0][1] = 255; board[0][2] = 255; board[0][3] = 255;
            board[0][4] = 255; board[0][5] = 255; board[0][7] = 255; board[0][8] = 255;
            board[0][9] = 255; board[0][10] = 255; board[0][11] = 255; board[0][12] = 255;
            board[1][0] = 255; board[1][1] = 255; board[1][2] = 255; board[1][3] = 255;
            board[1][4] = 255; board[1][7] = 255; board[1][8] = 255; board[1][9] = 255;
            board[1][10] = 255; board[1][11] = 255; board[1][12] = 255;
            board[2][0] = 255; board[2][1] = 255; board[2][2] = 255; board[2][3] = 255;
            board[2][4] = 255; board[2][8] = 255; board[2][9] = 255;
            board[2][10] = 255; board[2][11] = 255; board[2][12] = 255;
            board[3][0] = 255; board[3][1] = 255; board[3][2] = 255;
            board[3][3] = 255; board[3][8] = 255; board[3][9] = 255;
            board[3][10] = 255; board[3][11] = 255; board[3][12] = 255;
            board[5][12] = 255; board[6][12] = 255; board[7][11] = 255; board[7][12] = 255;
            board[8][11] = 255; board[8][12] = 255; board[9][11] = 255; board[9][12] = 255;
            board[10][12] = 255; board[11][12] = 255;
            board[6][0] = 255; board[7][0] = 255; board[8][0] = 255; board[8][1] = 255;
            board[9][0] = 255; board[10][0] = 255;
            board[13][0] = 255; board[13][1] = 255; board[13][2] = 255;
            board[13][3] = 255; board[13][8] = 255; board[13][9] = 255;
            board[13][10] = 255; board[13][11] = 255; board[13][12] = 255;
            board[14][0] = 255; board[14][1] = 255; board[14][2] = 255; board[14][3] = 255;
            board[14][4] = 255; board[14][8] = 255; board[14][9] = 255;
            board[14][10] = 255; board[14][11] = 255; board[14][12] = 255;
            board[15][0] = 255; board[15][1] = 255; board[15][2] = 255; board[15][3] = 255;
            board[15][4] = 255; board[15][7] = 255; board[15][8] = 255; board[15][9] = 255;
            board[15][10] = 255; board[15][11] = 255; board[15][12] = 255;
            board[16][0] = 255; board[16][1] = 255; board[16][2] = 255; board[16][3] = 255;
            board[16][4] = 255; board[16][5] = 255; board[16][7] = 255; board[16][8] = 255;
            board[16][9] = 255; board[16][10] = 255; board[16][11] = 255; board[16][12] = 255;
            // next, the center of the board, always empty
            board[4][4] = 128; board[4][5] = 128; board[4][6] = 128; board[4][7] = 128;
            board[4][8] = 128; board[5][3] = 128; board[5][4] = 128; board[5][5] = 128;
            board[5][6] = 128; board[5][7] = 128; board[5][8] = 128;
            board[6][3] = 128; board[6][4] = 128; board[6][5] = 128;
            board[6][6] = 128; board[6][7] = 128; board[6][8] = 128; board[6][9] = 128;
            board[7][2] = 128; board[7][3] = 128; board[7][4] = 128; board[7][5] = 128;
            board[7][6] = 128; board[7][7] = 128; board[7][8] = 128; board[7][9] = 128;
            board[8][2] = 128; board[8][3] = 128; board[8][4] = 128; board[8][5] = 128;
            board[8][6] = 128; board[8][7] = 128; board[8][8] = 128; board[8][9] = 128;
            board[8][10] = 128;
            board[9][2] = 128; board[9][3] = 128; board[9][4] = 128; board[9][5] = 128;
            board[9][6] = 128; board[9][7] = 128; board[9][8] = 128; board[9][9] = 128;
            board[10][3] = 128; board[10][4] = 128; board[10][5] = 128;
            board[10][6] = 128; board[10][7] = 128; board[10][8] = 128; board[10][9] = 128;
            board[11][3] = 128; board[11][4] = 128; board[11][5] = 128;
            board[11][6] = 128; board[11][7] = 128; board[11][8] = 128; board[12][4] = 128;
            board[12][5] = 128; board[12][6] = 128; board[12][7] = 128; board[12][8] = 128;
            // finally the place players/ empty in the corners
            // players 0 and 1 are always active
            board[0][6] = 1;
            board[1][5] = 1; board[1][6] = 1;
            board[2][5] = 1; board[2][6] = 1; board[2][7] = 1;
            board[3][4] = 1; board[3][5] = 1; board[3][6] = 1; board[3][7] = 1;
            board[16][6] = 2;
            board[15][5] = 2; board[15][6] = 2;
            board[14][5] = 2; board[14][6] = 2; board[14][7] = 2;
            board[13][4] = 2; board[13][5] = 2; board[13][6] = 2; board[13][7] = 2;
            byte fill = 128;
            if (numPlayers > 2) fill = 4;
            board[7][1] = fill;
            board[6][1] = fill; board[6][2] = fill;
            board[5][0] = fill; board[5][1] = fill; board[5][2] = fill;
            board[4][0] = fill; board[4][1] = fill; board[4][2] = fill; board[4][3] = fill;
            if (numPlayers > 3) fill = 8;
            else fill = 128;
            board[9][10] = fill;
            board[10][10] = fill; board[10][11] = fill;
            board[11][9] = fill; board[11][10] = fill; board[11][11] = fill;
            board[12][9] = fill; board[12][10] = fill; board[12][11] = fill; board[12][12] = fill;
            if (numPlayers > 4) fill = 16;
            else fill = 128;
            board[7][10] = fill;
            board[6][10] = fill; board[6][11] = fill;
            board[5][9] = fill; board[5][10] = fill; board[5][11] = fill;
            board[4][9] = fill; board[4][10] = fill; board[4][11] = fill; board[4][12] = fill;
            if (numPlayers > 5) fill = 32;
            else fill = 128;
            board[9][1] = fill;
            board[10][1] = fill; board[10][2] = fill;
            board[11][0] = fill; board[11][1] = fill; board[11][2] = fill;
            board[12][0] = fill; board[12][1] = fill; board[12][2] = fill; board[12][3] = fill;

            piecePos = new byte[numPlayers][];
            // Player 1
            piecePos[0] = new byte[20];
            piecePos[0][0] = 0; piecePos[0][1] = 6;
            piecePos[0][2] = 1; piecePos[0][3] = 5;
            piecePos[0][4] = 1; piecePos[0][5] = 6;
            piecePos[0][6] = 2; piecePos[0][7] = 5;
            piecePos[0][8] = 2; piecePos[0][9] = 6;
            piecePos[0][10] = 2; piecePos[0][11] = 7;
            piecePos[0][12] = 3; piecePos[0][13] = 4;
            piecePos[0][14] = 3; piecePos[0][15] = 5;
            piecePos[0][16] = 3; piecePos[0][17] = 6;
            piecePos[0][18] = 3; piecePos[0][19] = 7;
            // Player 2
            piecePos[1] = new byte[20];
            piecePos[1][0] = 16; piecePos[1][1] = 6;
            piecePos[1][2] = 15; piecePos[1][3] = 5;
            piecePos[1][4] = 15; piecePos[1][5] = 6;
            piecePos[1][6] = 14; piecePos[1][7] = 5;
            piecePos[1][8] = 14; piecePos[1][9] = 6;
            piecePos[1][10] = 14; piecePos[1][11] = 7;
            piecePos[1][12] = 13; piecePos[1][13] = 4;
            piecePos[1][14] = 13; piecePos[1][15] = 5;
            piecePos[1][16] = 13; piecePos[1][17] = 6;
            piecePos[1][18] = 13; piecePos[1][19] = 7;
            if (numPlayers > 2)
            {
                piecePos[2] = new byte[20];
                piecePos[2][0] = 7; piecePos[2][1] = 1;
                piecePos[2][2] = 6; piecePos[2][3] = 1;
                piecePos[2][4] = 6; piecePos[2][5] = 2;
                piecePos[2][6] = 5; piecePos[2][7] = 0;
                piecePos[2][8] = 5; piecePos[2][9] = 1;
                piecePos[2][10] = 5; piecePos[2][11] = 2;
                piecePos[2][12] = 4; piecePos[2][13] = 0;
                piecePos[2][14] = 4; piecePos[2][15] = 1;
                piecePos[2][16] = 4; piecePos[2][17] = 2;
                piecePos[2][18] = 4; piecePos[2][19] = 3;
            }
            if (numPlayers > 3)
            {
                piecePos[3] = new byte[20];
                piecePos[3][0] = 9; piecePos[3][1] = 10;
                piecePos[3][2] = 10; piecePos[3][3] = 10;
                piecePos[3][4] = 10; piecePos[3][5] = 11;
                piecePos[3][6] = 11; piecePos[3][7] = 9;
                piecePos[3][8] = 11; piecePos[3][9] = 10;
                piecePos[3][10] = 11; piecePos[3][11] = 11;
                piecePos[3][12] = 12; piecePos[3][13] = 9;
                piecePos[3][14] = 12; piecePos[3][15] = 10;
                piecePos[3][16] = 12; piecePos[3][17] = 11;
                piecePos[3][18] = 12; piecePos[3][19] = 12;
            }
            if (numPlayers > 4)
            {
                piecePos[4] = new byte[20];
                piecePos[4][0] = 7; piecePos[4][1] = 10;
                piecePos[4][2] = 6; piecePos[4][3] = 10;
                piecePos[4][4] = 6; piecePos[4][5] = 11;
                piecePos[4][6] = 5; piecePos[4][7] = 9;
                piecePos[4][8] = 5; piecePos[4][9] = 10;
                piecePos[4][10] = 5; piecePos[4][11] = 11;
                piecePos[4][12] = 4; piecePos[4][13] = 9;
                piecePos[4][14] = 4; piecePos[4][15] = 10;
                piecePos[4][16] = 4; piecePos[4][17] = 11;
                piecePos[4][18] = 4; piecePos[4][19] = 12;
            }
            if (numPlayers > 5)
            {
                piecePos[5] = new byte[20];
                piecePos[5][0] = 9; piecePos[5][1] = 1;
                piecePos[5][2] = 10; piecePos[5][3] = 1;
                piecePos[5][4] = 10; piecePos[5][5] = 2;
                piecePos[5][6] = 11; piecePos[5][7] = 0;
                piecePos[5][8] = 11; piecePos[5][9] = 1;
                piecePos[5][10] = 11; piecePos[5][11] = 2;
                piecePos[5][12] = 12; piecePos[5][13] = 0;
                piecePos[5][14] = 12; piecePos[5][15] = 1;
                piecePos[5][16] = 12; piecePos[5][17] = 2;
                piecePos[5][18] = 12; piecePos[5][19] = 3;
            }
        }

        // copy-constructor
        public Board(Board otherB)
        {
            board = new byte[17][];
            for (int i = 0; i < 17; i++)
            {
                board[i] = new byte[13];
                for (int j = 0; j < 13; j++)
                    board[i][j] = otherB.board[i][j];
            }
            int l = otherB.piecePos.Length;
            piecePos = new byte[l][];
            for (int i = 0; i < l; i++)
            {
                piecePos[i] = new byte[20];
                for (int j = 0; j < 20; j++)
                    piecePos[i][j] = otherB.piecePos[i][j];
            }
        }

        public byte[][] getPiecePos()
        {
            return piecePos;
        }

        public void movePiece(int fromI, int fromJ, int toI, int toJ, int player)
        {
            board[toI][toJ] = board[fromI][fromJ];
            board[fromI][fromJ] = 128;
            for(int k = 0; k < 20; k++)
                if (piecePos[player][k++] == fromI && piecePos[player][k] == fromJ)
                {
                    piecePos[player][k--] = (byte)toJ;
                    piecePos[player][k] = (byte)toI;
                    return;
                }
        }

        // The correct implemntation of modulo, that also works for negative numbers
        // Since it most likely is slower than %, use it only where negatives are involved
        private static int modulo(double a, double b)
        {
            return (int)(a - b * Math.Floor(a / b));
        }

        /// <summary>
        /// Returns the a list of coordinates where the piece can be legally moved.
        /// This means any free adjacent square, and also jumps.
        /// </summary>
        public LinkedList<int> getValidMoves(int i, int j)
        {
            LinkedList<int> validMoves = new LinkedList<int>();
            // in this list we'll store positions in which the piece can jump
            LinkedList<int> jumpPositions = new LinkedList<int>();
            // next, create a temporary board on which we mark positions already considered
            // we will add a padding around the tempBoard, so that we don't need to worry
            // about indexes going outside the array
            for (int k = 1; k < 18; k++)
            {
                for (int l = 1; l < 14; l++)
                    tempBoard[k][l] = board[k-1][l-1];
            }
            i++; j++; // adjust i and j to the padded board
            int imod2 = i % 2; // used to determine whether we're on an odd or even numbered line
            for (int k = 0; k < 6; k++)
            {
                int dir_x_2 = k + k;
                int x = i + dirDiff[imod2][dir_x_2];
                int y = j + dirDiff[imod2][dir_x_2 + 1];
                if (tempBoard[x][y] == 128) // empty position
                {
                    validMoves.AddLast(x-1);// remember, x and y are adjusted
                    validMoves.AddLast(y-1);// to the paded board
                    tempBoard[x][y] = 255;
                }
                else if (tempBoard[x][y] < 33) // piece over which we can jump
                {
                    int xmod2 = x % 2;
                    tempBoard[x][y] = 255;// unavailable for further jumps
                    // there are three directions in which we can jump: k-1, k, k+1
                    int x2 = x + dirDiff[xmod2][dir_x_2];
                    int y2 = y + dirDiff[xmod2][dir_x_2 + 1];
                    if (tempBoard[x2][y2] == 128)
                    {
                        validMoves.AddLast(x2-1);
                        validMoves.AddLast(y2-1);
                        jumpPositions.AddLast(x2);
                        jumpPositions.AddLast(y2);
                        tempBoard[x2][y2] = 255;
                    }
                    dir_x_2 = (dir_x_2 + 2) % 12;
                    x2 = x + dirDiff[xmod2][dir_x_2];
                    y2 = y + dirDiff[xmod2][dir_x_2 + 1];
                    if (tempBoard[x2][y2] == 128)
                    {
                        validMoves.AddLast(x2-1);
                        validMoves.AddLast(y2-1);
                        jumpPositions.AddLast(x2);
                        jumpPositions.AddLast(y2);
                        tempBoard[x2][y2] = 255;
                    }
                    dir_x_2 = modulo((dir_x_2 - 4), 12);
                    x2 = x + dirDiff[xmod2][dir_x_2];
                    y2 = y + dirDiff[xmod2][dir_x_2 + 1];
                    if (tempBoard[x2][y2] == 128)
                    {
                        validMoves.AddLast(x2-1);
                        validMoves.AddLast(y2-1);
                        jumpPositions.AddLast(x2);
                        jumpPositions.AddLast(y2);
                        tempBoard[x2][y2] = 255;
                    }
                }
            }
            while (jumpPositions.Last != null)
            {
                // for each jump position, check if we can jump again
                i = jumpPositions.First.Value;
                jumpPositions.RemoveFirst();
                j = jumpPositions.First.Value;
                jumpPositions.RemoveFirst();
                imod2 = i % 2;
                for (int k = 0; k < 6; k++)
                {
                    int dir_x_2 = k + k;
                    int x = i + dirDiff[imod2][dir_x_2];
                    int y = j + dirDiff[imod2][dir_x_2 + 1];
                    if (tempBoard[x][y] < 33) // piece over which we can jump
                    {
                        int xmod2 = x % 2;
                        tempBoard[x][y] = 255;// unavailable for further jumps
                        // there are three directions in which we can jump: k-1, k, k+1
                        int x2 = x + dirDiff[xmod2][dir_x_2];
                        int y2 = y + dirDiff[xmod2][dir_x_2 + 1];
                        if (tempBoard[x2][y2] == 128)
                        {
                            validMoves.AddLast(x2-1);
                            validMoves.AddLast(y2-1);
                            jumpPositions.AddLast(x2);
                            jumpPositions.AddLast(y2);
                            tempBoard[x2][y2] = 255;
                        }
                        dir_x_2 = (dir_x_2 + 2) % 12;
                        x2 = x + dirDiff[xmod2][dir_x_2];
                        y2 = y + dirDiff[xmod2][dir_x_2 + 1];
                        if (tempBoard[x2][y2] == 128)
                        {
                            validMoves.AddLast(x2-1);
                            validMoves.AddLast(y2-1);
                            jumpPositions.AddLast(x2);
                            jumpPositions.AddLast(y2);
                            tempBoard[x2][y2] = 255;
                        }
                        dir_x_2 = modulo((dir_x_2 - 4), 12);
                        x2 = x + dirDiff[xmod2][dir_x_2];
                        y2 = y + dirDiff[xmod2][dir_x_2 + 1];
                        if (tempBoard[x2][y2] == 128)
                        {
                            validMoves.AddLast(x2-1);
                            validMoves.AddLast(y2-1);
                            jumpPositions.AddLast(x2);
                            jumpPositions.AddLast(y2);
                            tempBoard[x2][y2] = 255;
                        }
                    }
                }
            }
            return validMoves;
        }

        private static bool areInBounds(int i, int j)
        {
            return i >= 0 && i < 17 && j >= 0 && j < 13;
        }

        /// <summary>
        /// Computes the euclidean distance between two point on the table, rounded down
        /// to the nearest integer, to provide uniformity (due to the hex nature of the board
        /// sqrt(5) and 2 are actually the same distance)
        /// If strictly measuring jump distance, divide this by two (so that the
        /// heuristic is admissible, meaning optimistic)
        /// </summary>
        private static int h(int fromI, int fromJ, int toI, int toJ)
        {
            //return Math.Max(Math.Abs(toI - fromI), Math.Abs(toJ - fromJ));
            return (int)Math.Sqrt(Math.Pow(toI - fromI, 2) + Math.Pow(toJ - fromJ, 2));
        }

        /// <summary>
        /// Make an A* search to get the path of jumps between two points on the board.
        /// (Including the start point)
        /// </summary>
        public LinkedList<int> getPath(int fromI, int fromJ, int toI, int toJ)
        {
            for (int k = 1; k < 18; k++)
            {
                for (int l = 1; l < 14; l++)
                    tempBoard[k][l] = board[k - 1][l - 1];
            }
            fromI++; fromJ++; toI++; toJ++; // adjust coordinates to tempBoard
            // First, check if destination is adjacent (no jumps)
            int imod2 = fromI % 2;
            for (int k = 0; k < 6; k++)
            {
                int dir_x_2 = k + k;
                int x = fromI + dirDiff[imod2][dir_x_2];
                int y = fromJ + dirDiff[imod2][dir_x_2 + 1];
                if (x == toI && y == toJ)
                {
                    LinkedList<int> path = new LinkedList<int>();
                    path.AddLast(fromI - 1);
                    path.AddLast(fromJ - 1);
                    path.AddLast(x - 1);
                    path.AddLast(y - 1);
                    return path;
                }
            }
            // Next, begin A* search; because it is directed, there's no need for a temp board
            HeapPriorityQueue<SearchNode> queue = new HeapPriorityQueue<SearchNode>(300);
            queue.Enqueue(new SearchNode(new LinkedList<int>(), 0, fromI, fromJ));
            while (queue.Count > 0)
            {
                SearchNode n = queue.Dequeue();
                if (n.i == toI && n.j == toJ)
                    return n.path;
                // if not final state, compute all possible jumps
                imod2 = n.i % 2;
                for (int k = 0; k < 6; k++)
                {
                    int dir_x_2 = k + k;
                    int x = n.i + dirDiff[imod2][dir_x_2];
                    int y = n.j + dirDiff[imod2][dir_x_2 + 1];
                    if (tempBoard[x][y] < 33) // piece over which we can jump
                    {
                        //tempBoard[x][y] = 255;
                        int xmod2 = x % 2;
                        // there are three directions in which we can jump: k-1, k, k+1
                        int x2 = x + dirDiff[xmod2][dir_x_2];
                        int y2 = y + dirDiff[xmod2][dir_x_2 + 1];
                        if (tempBoard[x2][y2] == 128)
                        {
                            queue.Enqueue(new SearchNode(new LinkedList<int>(n.path),
                                            (int)(n.path.Count + h(x2,y2,toI,toJ)/2), x2, y2));
                            tempBoard[x2][y2] = 255;
                        }
                        dir_x_2 = (dir_x_2 + 2) % 12;
                        x2 = x + dirDiff[xmod2][dir_x_2];
                        y2 = y + dirDiff[xmod2][dir_x_2 + 1];
                        if (tempBoard[x2][y2] == 128)
                        {
                            queue.Enqueue(new SearchNode(new LinkedList<int>(n.path),
                                            (int)(n.path.Count + h(x2, y2, toI, toJ) / 2), x2, y2));
                            tempBoard[x2][y2] = 255;
                        }
                        dir_x_2 = modulo((dir_x_2 - 4), 12);
                        x2 = x + dirDiff[xmod2][dir_x_2];
                        y2 = y + dirDiff[xmod2][dir_x_2 + 1];
                        if (tempBoard[x2][y2] == 128)
                        {
                            queue.Enqueue(new SearchNode(new LinkedList<int>(n.path),
                                            (int)(n.path.Count + h(x2, y2, toI, toJ) / 2), x2, y2));
                            tempBoard[x2][y2] = 255;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Deduces the action which transformed the previous board into this board.
        /// We do this because we don't want to waste memory by remembering actions in our
        /// MonteCarloNode.
        /// </summary>
        /// <param name="prev">The previous board</param>
        /// <returns></returns>
        public Action deduceAction(Board prev)
        {
            Action a = new Action(0, 0, 0, 0);
            for (int i = 0; i < 17; i++)
            {
                for (int j = 0; j < 13; j++)
                {
                    if (prev.board[i][j] < 33 && board[i][j] == 128)
                    {
                        a.fromI = i;
                        a.fromJ = j;
                    }
                    if (prev.board[i][j] == 128 && board[i][j] < 33)
                    {
                        a.toI = i;
                        a.toJ = j;
                    }
                }
            }
            return a;
        }

        /// <summary>
        /// Computes the score of a given action. This score is the distance I've covered
        /// towards my end goal.
        /// </summary>
        public static int score(Action a, int playerIndex)
        {
            int pi_2 = playerIndex + playerIndex;
            return h(a.fromI, a.fromJ, playerGoal[pi_2], playerGoal[pi_2 + 1]) -
                h(a.toI, a.toJ, playerGoal[pi_2], playerGoal[pi_2 + 1]);
        }
        private static int[] playerGoal = { 16, 6, 0, 6, 12, 12, 4, 0, 12, 0, 4, 12};

        /// <summary>
        /// Returns true if the player sent as argument won the game. (all of his pieces are
        ///  in the opposing corner of the board)
        /// </summary>
        public bool hasWon(int player)
        {
            int sum = 0;
            switch (player)
            {
                case 1: sum = board[0][6] & board[1][5] & board[1][6];
                    sum &= board[2][5] & board[2][6] & board[2][7];
                    sum &= board[3][4] & board[3][5] & board[3][6] & board[3][7];
                    return sum == 2;
                case 0: sum = board[16][6] & board[15][5] & board[15][6];
                    sum &= board[14][5] & board[14][6] & board[14][7];
                    sum &= board[13][4] & board[13][5] & board[13][6] & board[13][7];
                    return sum == 1;
                case 3: sum = board[7][1] & board[6][1] & board[6][2];
                    sum &= board[5][0] & board[5][1] & board[5][2];
                    sum &= board[4][0] & board[4][1] & board[4][2] & board[4][3];
                    return sum == 8;
                case 2: sum = board[9][10] & board[10][10] & board[10][11];
                    sum &= board[11][9] & board[11][10] & board[11][11];
                    sum &= board[12][9] & board[12][10] & board[12][11] & board[12][12];
                    return sum == 4;
                case 5: sum = board[7][10] & board[6][10] & board[6][11];
                    sum &= board[5][9] & board[5][10] & board[5][11];
                    sum &= board[4][9] & board[4][10] & board[4][11] + board[4][12];
                    return sum == 32;
                case 4: sum = board[9][1] & board[10][1] & board[10][2];
                    sum &= board[11][0] & board[11][1] & board[11][2];
                    sum &= board[12][0] & board[12][1] & board[12][2] & board[12][3];
                    return sum == 16;
            }
            return false;
        }

        private class SearchNode : PriorityQueueNode
        {
            public LinkedList<int> path;
            public int i, j; // crrt position
            public SearchNode(LinkedList<int> _path, int g, int _i, int _j)
            {
                path = _path;
                path.AddLast(_i - 1); // remmeber, tempBoard coordinates!
                path.AddLast(_j - 1);
                Priority = g;
                i = _i;
                j = _j;
            }
        }
    }
}
