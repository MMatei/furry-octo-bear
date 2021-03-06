﻿using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.GamerServices;

namespace ChineseCheckers
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private SpriteFont font;
        private MouseState mouseStatePrev; // previous mouse state (to help id click)
        private Texture2D table;
        private Rectangle tableRect;
        private Texture2D[] ball;
        private Texture2D ring;
        private Rectangle[] highlightPositions; // an array of all highlighted positions
        private Board board;
        private Rectangle screenRect;

        private double heightRatio, widthRatio;

        private RenderTarget2D renderTarget;

        public static int numPlayers;
        private int crtPlayer = 0;
        private String[] playerText; // text representing the six players
        internal static AI[] isAI; // is the player human (null) or AI (which type)
        internal static Dictionary<Action, int>[] aiHistory; // a dictionary of the usefulness of actions
        internal static List<Action>[] aiActions; // a list of the actions the ai has taken during the game
        // used in order to complete our history
        private bool continueGame = false; // keep playing games until nrGames is 0
        private int nrGames = 10;
        private int[] victories = new int[6];

        private int state = STATE_RUNNING; // the game state, one of the following:
        private const int STATE_RUNNING = 2;
        private const int STATE_WON = 3;

        public Game1() : base()
        {
            graphics = new GraphicsDeviceManager(this);
            int screenW = graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            int screenH = graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            double ratio = 818;
            ratio /= 916;
            screenRect = new Rectangle(32, 32, (int)((screenH - 64) * ratio), screenH - 64);
            graphics.ToggleFullScreen();
            Content.RootDirectory = "Content";
            // The screen rect is bigger than the actual board rect; this is the ratio between them
            // we need it to translate mouse position from screen space to board space
            widthRatio = (818 / (double)screenRect.Width);
            heightRatio = (916 / (double)screenRect.Height);
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            IsMouseVisible = true;
            renderTarget = new RenderTarget2D(GraphicsDevice, 818, 916, false,
                            GraphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            table = Texture2D.FromStream(GraphicsDevice, new FileStream("resources/table.png", FileMode.Open));
            tableRect = new Rectangle(0,0,818,916);
            ball = new Texture2D[6];
            ball[0] = Texture2D.FromStream(GraphicsDevice, new FileStream("resources/ball_0.png", FileMode.Open));
            ball[1] = Texture2D.FromStream(GraphicsDevice, new FileStream("resources/ball_1.png", FileMode.Open));
            ball[2] = Texture2D.FromStream(GraphicsDevice, new FileStream("resources/ball_2.png", FileMode.Open));
            ball[3] = Texture2D.FromStream(GraphicsDevice, new FileStream("resources/ball_3.png", FileMode.Open));
            ball[4] = Texture2D.FromStream(GraphicsDevice, new FileStream("resources/ball_4.png", FileMode.Open));
            ball[5] = Texture2D.FromStream(GraphicsDevice, new FileStream("resources/ball_5.png", FileMode.Open));
            ring = Texture2D.FromStream(GraphicsDevice, new FileStream("resources/ring.png", FileMode.Open));
            playerText = new String[] { "Red", "Green", "Blue", "Magenta", "Yellow", "Cyan" };
            font = Content.Load<SpriteFont>("SpriteFont");
            loadConfig(); // numPlayers, isAi, constant values

            Board.initTempBoard();
            board = new Board(numPlayers);
            PiecesDraw.createPieceRectangles(board.getPiecePos(), numPlayers);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            AI.stop();
            Console.WriteLine("-------------------------------------");
            Console.WriteLine("TALLY == " + victories[0] + victories[1] + victories[2] +
                victories[3] + victories[4] + victories[5]);
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            MouseState mouseState = Mouse.GetState();
            KeyboardState keyboardState = Keyboard.GetState();
            if (keyboardState.IsKeyDown(Keys.Escape)) {
                // de-comment to take a pic of the table
                renderTarget.SaveAsPng(new FileStream("output.png", FileMode.OpenOrCreate), renderTarget.Width, renderTarget.Height);
                Exit();
            }
            if (state == STATE_WON)
            {
                if (!PiecesDraw.isAnimationDone())
                    PiecesDraw.update(this); // otherwise last move will not be shown
                return;
            }

            if (PiecesDraw.isAnimationDone())
            {
                if (isAI[crtPlayer] == null)
                {
                    // -- HUMAN CONTROLS ---
                    // adjust mouse position to board space
                    int mouseX = (int)((mouseState.X - 32) * widthRatio);
                    int mouseY = (int)((mouseState.Y - 32) * heightRatio);
                    if (mouseState.LeftButton == ButtonState.Pressed && mouseStatePrev.LeftButton == ButtonState.Released)
                    {// mouse click
                        if (highlightPositions != null)
                        {
                            // check if I've clicked on the highlighted piece => cancel highlight
                            if (highlightPositions[0].Contains(mouseX, mouseY))
                                highlightPositions = null;
                            else
                                // check if click in other highlight => move piece there
                                for (int i = 1; i < highlightPositions.Length; i++)
                                {
                                    if (highlightPositions[i].Contains(mouseX, mouseY))
                                    {
                                        // reverse-engineer the rectangles to obtain coordinates
                                        int fromI = (highlightPositions[0].Y - 15) / 52;
                                        int fromJ = (highlightPositions[0].X - 22 - (fromI % 2) * 30) / 60;
                                        int toI = (highlightPositions[i].Y - 15) / 52;
                                        int toJ = (highlightPositions[i].X - 22 - (toI % 2) * 30) / 60;
                                        // get path to that position
                                        LinkedList<int> path = board.getPath(fromI, fromJ, toI, toJ);
                                        // start animation
                                        PiecesDraw.createAnimation(path);
                                        board.movePiece(fromI, fromJ, toI, toJ, crtPlayer);
                                        highlightPositions = null;// stop highlighting
                                        // Current player moved -> go to next player
                                        checkForVictory();
                                        int nextPlayer = (crtPlayer + 1) % numPlayers;
                                        if(isAI[nextPlayer] != null) // non - human
                                            // while animation is playing, think
                                            isAI[nextPlayer].think(board, nextPlayer);
                                        // Learn from the humie
                                        aiActions[crtPlayer].Add(new Action(fromI, fromJ, toI, toJ));
                                        break;
                                    }
                                }
                        }
                        else
                        {
                            // if we've clicked on a piece, select it
                            // => compute the valid moves and highlight them all
                            for (int j = 0; j < 10; j++)
                            {
                                if (PiecesDraw.pieceRect[crtPlayer][j].Contains(mouseX, mouseY))
                                {
                                    byte[][] piecePos = board.getPiecePos();
                                    int pi = piecePos[crtPlayer][j + j];
                                    int pj = piecePos[crtPlayer][j + j + 1];
                                    LinkedList<int> validMoves = board.getValidMoves(pi, pj);
                                    int n = 1 + validMoves.Count / 2; // 2 int coords per move
                                    highlightPositions = new Rectangle[n];
                                    highlightPositions[0] = PiecesDraw.pieceRect[crtPlayer][j]; // the clicked piece
                                    for (int k = 1; k < n; k++)
                                    {
                                        pi = validMoves.First.Value;
                                        validMoves.RemoveFirst();
                                        pj = validMoves.First.Value;
                                        validMoves.RemoveFirst();
                                        int x = 22 + pj * 60 + (pi % 2) * 30;
                                        int y = 15 + pi * 52;
                                        highlightPositions[k] = new Rectangle(x, y, 52, 52);
                                    }
                                    break;
                                }
                            }
                        }
                    }

                    // deselect on right click
                    if (mouseState.RightButton == ButtonState.Pressed)
                        highlightPositions = null;
                }
                else
                {
                    if (AI.thinking == 2) // thinking is done on a separate thread
                    {
                        AI.thinking = 0;
                        Board nextBoard = AI.board;
                        Action a = nextBoard.deduceAction(board);
                        LinkedList<int> path = board.getPath(a.fromI, a.fromJ, a.toI, a.toJ);
                        // start animation
                        PiecesDraw.createAnimation(path);
                        board = nextBoard;
                        checkForVictory();
                        int nextPlayer = (crtPlayer + 1) % numPlayers;
                        if (isAI[nextPlayer] != null)
                            // while animation is playing, think
                            isAI[nextPlayer].think(board, nextPlayer);
                    }
                    else if (AI.thinking == 0)
                        isAI[crtPlayer].think(board, crtPlayer);
                }
            }
            else
                PiecesDraw.update(this);
            mouseStatePrev = mouseState;
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        private Rectangle crrtPlayerRect = new Rectangle(0, 32, 52, 52);
        private Vector2 crrtPlayerVect = new Vector2(64, 42);
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(renderTarget);
            GraphicsDevice.Clear(Color.White);

            spriteBatch.Begin();
            spriteBatch.Draw(table, tableRect, Color.White);
            // now draw the pieces
            for (int i = 0; i < numPlayers; i++)
            {
                foreach (Rectangle r in PiecesDraw.pieceRect[i])
                    spriteBatch.Draw(ball[i], r, Color.White);
            }
            PiecesDraw.drawMovingPiece(spriteBatch, ball);
            if (highlightPositions != null)
            {
                foreach(Rectangle r in highlightPositions)
                {
                    spriteBatch.Draw(ring, r, Color.White);
                }
            }
            if(state == STATE_RUNNING)
                spriteBatch.DrawString(font, "Current player:", Vector2.Zero, Color.Black);
            else
                spriteBatch.DrawString(font, "Victorious player:", Vector2.Zero, Color.Black);
            spriteBatch.Draw(ball[crtPlayer], crrtPlayerRect, Color.White);
            String playerType = isAI[crtPlayer] == null ? " - Human" : " - AI";
            spriteBatch.DrawString(font, playerText[crtPlayer] + playerType,
                crrtPlayerVect, Color.Black);
            spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.White);
            spriteBatch.Begin();
            spriteBatch.Draw(renderTarget, screenRect, null, Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void checkForVictory()
        {
            if (board.hasWon(crtPlayer)) {
                state = STATE_WON;
                Console.WriteLine("VICTORY for player " + crtPlayer);
                victories[crtPlayer]++;
                // Now that the game is won, merge the actions of the AIs into the history
                for (int i = 0; i < numPlayers; i++) {
                    //if (isAI[i] == null)
                    //    continue;
                    var dict = aiHistory[i];
                    if (crtPlayer == i) // Winner's actions have their score increased
                    foreach (Action a in aiActions[i]) {
                        int val;
                        dict.TryGetValue(a, out val); // pass by reference
                        dict[a] = val + 1;
                    }
                    else
                    foreach (Action a in aiActions[i]) {
                        int val;
                        dict.TryGetValue(a, out val); // pass by reference
                        dict[a] = val - 1;
                    }
                    DictionaryIO.write(dict, "history_" + i + ".bin");
                }
                if (continueGame) {
                    nrGames--;
                    if (nrGames == 0) {
                        Exit();
                        return;
                    }
                    PiecesDraw.cancelAnimation();
                    state = STATE_RUNNING;
                    crtPlayer = numPlayers - 1;
                    board = new Board(numPlayers);
                    PiecesDraw.createPieceRectangles(board.getPiecePos(), numPlayers);
                    // Must switch to next player after update code runs once
                    // because of the way AI update works
                    System.Timers.Timer timer = new System.Timers.Timer(500);
                    timer.Elapsed += nextPlayer;
                    timer.Enabled = true;
                    timer.AutoReset = false;
                }
            }
        }

        // Loads information regarding the number of players and whether these players are AI/human
        // from the file config.txt; first line is whether game should continue ad infinitum
        // second line is nr of players
        // third line is type of AI separated by spaces
        // fourth line is epsilon value
        private void loadConfig()
        {
            System.IO.StreamReader file = new System.IO.StreamReader("config.txt");
            continueGame = Convert.ToBoolean(file.ReadLine());
            numPlayers = Convert.ToInt32(file.ReadLine());
            String[] ints = file.ReadLine().Split(' ');
            String[] eps = file.ReadLine().Split(' ');
            isAI = new AI[numPlayers];
            aiHistory = new Dictionary<Action, int>[numPlayers];
            aiActions = new List<Action>[numPlayers];
            for (int i = 0; i < numPlayers; i++) {
                int AItype = Convert.ToInt32(ints[i]);
                // aiHistory is used regardless if player i is AI or not
                aiHistory[i] = DictionaryIO.read("history_" + numPlayers+ "_"+ i + ".bin");
                aiHistory[i][new Action(2, 7, 4, 7)] = 100;
                aiHistory[i][new Action(2, 5, 4, 5)] = 100;
                aiHistory[i][new Action(6, 11, 7, 9)] = 100;
                aiHistory[i][new Action(12, 10, 11, 8)] = 100;
                aiHistory[i][new Action(14, 5, 12, 5)] = 100;
                aiHistory[i][new Action(14, 7, 12, 7)] = 100;
                aiHistory[i][new Action(9, 1, 8, 2)] = 100;
                aiHistory[i][new Action(4, 2, 5, 3)] = 100;
                aiActions[i] = new List<Action>();
                Console.WriteLine(AItype);
                switch (AItype) {
                    case 1: isAI[i] = new AI();
                        break;
                    case 2: isAI[i] = new OtherAI();
                        break;
                    case 3: isAI[i] = new LearningAI();
                        break;
                    default: isAI[i] = null;
                        break;
                }
                if (isAI[i] != null)
                    isAI[i].eps = Convert.ToInt32(eps[i]);
            }
            file.Close();
            AI.startAIThread();
        }

        // to be used by PiecesDraw, when animation is done
        internal void nextPlayer(Object source, System.Timers.ElapsedEventArgs e)
        {
            if(state != STATE_WON)
                crtPlayer = (crtPlayer + 1) % numPlayers;
        }
    }
}
