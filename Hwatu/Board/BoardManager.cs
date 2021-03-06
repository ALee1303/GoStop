﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using MG_Library;
using Hwatu.Card;
using Hwatu.Minhwatu;
using Hwatu.MonoGameComponents;
using Hwatu.MonoGameComponents.Drawables;
using Hwatu.Collection;
using Microsoft.Xna.Framework.Graphics;

namespace Hwatu
{
    // TODO: Change to CardManager and move boardInitialization to board
    public class BoardManager : DrawableGameComponent
    {
        // PLAYER TURN CONTROL

        protected List<IHanafudaPlayer> waitList;
        public List<IHanafudaPlayer> WaitList { get => waitList; }
        protected Queue<IHanafudaPlayer> turnQueue;
        public Queue<IHanafudaPlayer> TurnQueue { get => turnQueue; }

        private IHanafudaPlayer currentPlayer;
        public IHanafudaPlayer CurrentPlayer
        {
            get => currentPlayer;
            set
            {
                if (currentPlayer == value)
                    return;
                currentPlayer = value;
                if (currentPlayer != null)
                    OnNewPlayerTurn();
            }
        }

        private IBoard _board;
        private MainPlayer mainPlayer;

        private CardFactory cardFactory;
        private Dictionary<Month, List<DrawableCard>> fieldCards;
        private Dictionary<IHanafudaPlayer, List<DrawableCard>> handCards;
        private Dictionary<IHanafudaPlayer, List<DrawableCard>> collectedCards;
        //currently not implemented, for future visual effect
        private Dictionary<Type, List<DrawableCard>> specialCollected;


        private Dictionary<IHanafudaPlayer, List<SpecialCards>> specialStatus;

        protected Dictionary<IHanafudaPlayer, int> specialPoints;
        public Dictionary<IHanafudaPlayer, int> SpecialPoints { get => specialPoints; }

        protected Dictionary<IHanafudaPlayer, int> scoreBoard;
        public Dictionary<IHanafudaPlayer, int> ScoreBoard { get => scoreBoard; }

        // Temporary
        private Sprite2D scoreView;
        private Vector2 outlineScale;

        //TODO: change later - temporary hack for easy update check
        private bool isPostGame;
        private SpriteFont result;
        private string resultText;
        private Vector2 resultOrigin;
        private Color resultColor;
        private float resultScale;

        public MainPlayer MainPlayer { get => mainPlayer; }
        public HanafudaController Controller { get => mainPlayer.Controller; }
        public Dictionary<Month, List<DrawableCard>> Field { get => fieldCards; }

        private Sprite2D loadedOutline;
        private SpriteBatch spriteBatch;

        public BoardManager(Game game) : base(game)
        {
            Game.Services.AddService<BoardManager>(this);

            waitList = new List<IHanafudaPlayer>();
            turnQueue = new Queue<IHanafudaPlayer>();

            cardFactory = new CardFactory(Game);
            fieldCards = new Dictionary<Month, List<DrawableCard>>();
            SetupField();
            handCards = new Dictionary<IHanafudaPlayer, List<DrawableCard>>();
            collectedCards = new Dictionary<IHanafudaPlayer, List<DrawableCard>>();
            specialCollected = new Dictionary<Type, List<DrawableCard>>();
            specialStatus = new Dictionary<IHanafudaPlayer, List<SpecialCards>>();
            specialPoints = new Dictionary<IHanafudaPlayer, int>();
            scoreBoard = new Dictionary<IHanafudaPlayer, int>();

            outlineScale = Vector2.One;
        }

        private void SetupField()
        {
            for (int i = 0; i < 12; i++)
            {
                Month key = (Month)i;
                fieldCards.Add(key, new List<DrawableCard>());
            }
        }
        // TODO: move to board currently not being used
        private void SetupMinhwatuSpecialVisual()
        {
            specialCollected.Add(typeof(HongDanCollection), new List<DrawableCard>());
            specialCollected.Add(typeof(ChungDanCollection), new List<DrawableCard>());
            specialCollected.Add(typeof(ChoDanCollection), new List<DrawableCard>());
            specialCollected.Add(typeof(ChoYak), new List<DrawableCard>());
            specialCollected.Add(typeof(PoongYak), new List<DrawableCard>());
            specialCollected.Add(typeof(BiYak), new List<DrawableCard>());
        }

        #region GameComponent

        public override void Initialize()
        {
            base.Initialize();
            cardFactory.Initialize();
            spriteBatch = (SpriteBatch)Game.Services.GetService(typeof(SpriteBatch));
        }
        protected override void LoadContent()
        {
            /* Temporary result setup*/
            isPostGame = false;
            result = Game.Content.Load<SpriteFont>("ScoreFont");
            resultOrigin = new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
            resultScale = 1.0f;
            /* TODO: make separate display Drawable,Sprite2D*/
        }

        public override void Update(GameTime gameTime)
        {
            if (isPostGame)
            {
                if (Controller.IsLeftMouseClicked())
                    RestartLoadedBoard();
            }
        }

        public override void Draw(GameTime gameTime)
        {
            DrawHands();
            DrawField();
            if (loadedOutline != null)
                ((OutlineImage)loadedOutline).Draw(outlineScale);
            DrawCollected();

            scoreView.Draw();
            // Temporary PostGame result view
            if (isPostGame)
                spriteBatch.DrawString(result, resultText, resultOrigin,
                    resultColor * 1.0f, 0.0f, Vector2.Zero, resultScale, SpriteEffects.None, 0.0f);
        }

        private void DrawHands()
        {
            handCards.Keys.ToList().ForEach(key =>
            {
                int idx = 0;
                for (; idx < handCards[key].Count; idx++)
                {
                    handCards[key][idx].Draw();
                }
            });
        }

        private void DrawCollected()
        {
            collectedCards.Keys.ToList().ForEach(key =>
            {
                int idx = 0;
                for (; idx < collectedCards[key].Count; idx++)
                {
                    collectedCards[key][idx].Draw();
                }
            });
        }

        private void DrawField()
        {
            fieldCards.Keys.ToList().ForEach(key =>
            {
                int idx = 0;
                for (; idx < fieldCards[key].Count; idx++)
                {
                    fieldCards[key][idx].Draw();
                }
            });
        }

        #endregion

        // Temporary
        public void StartMinhwatuGameVsCPU()
        {
            if (_board != null)
                new ArgumentException("Must Unsubscribe Board first");
            _board = new MinhwatuBoard(this);
            IHanafudaPlayer cpu = new CPU(this);
            cpu.JoinBoard(this);
            SetNewBoardForGame();
            scoreView = new ScoreBoard2p(this, Game);
            scoreView.Initialize();
            StartGame();
        }

        public virtual void StartGame()
        {
            _board.DealCard();
            CurrentPlayer = turnQueue.Dequeue();
        }

        private void SetNewBoardForGame()
        {
            SubscribeToBoard();
            OrderWaitingPlayers();
        }

        private void SubscribeToBoard()
        {
            Board board = (Board)_board;
            board.CardsDealt += board_CardsDealt;
            board.CardsOnField += board_CardsOnField;
        }


        /// <summary>
        /// Start game on board that is already instantiated
        /// TODO: multiple board start logic and case handling
        /// </summary>
        public void RestartLoadedBoard()
        {
            // TODO: Add case for detecting board's status and
            // Should not work when game is in progress or Drawables are not ready.
            if (!isPostGame)
                return;
            isPostGame = false;
            ResetBoard();
            // TODO: Add logic for refreshing special collections

            StartGame();
        }

        /// <summary>
        /// Reorder deck for new game
        /// </summary>
        /// TODO: sort without creating new Drawable
        private void ResetBoard()
        {
            DiscardDrawables();
            DeckCollection.Instance.GatherCards();
            OrderWaitingPlayers();
        }

        public void OnJoinBoard(IHanafudaPlayer player)
        {
            if (waitList.Contains(player) || !IsNotPlaying(player))
                return;
            if (player is MainPlayer)
            {
                mainPlayer = (MainPlayer)player;
                ((Player)player).MouseOverCard += player_MouseOverCard;
            }
            waitList.Add(player);
        }

        public void OnExitBoard(IHanafudaPlayer player)
        {
            // TODO: for multiple players
        }

        // Initialize player when first joining
        private void InitializePlayer(IHanafudaPlayer player)
        {  
            handCards.Add(player, new List<DrawableCard>());
            collectedCards.Add(player, new List<DrawableCard>());
            //Create special collection and subscribe player to the event.
            specialStatus.Add(player, _board.GetSpecialCollection(player));
            specialStatus[player].ForEach(
                (collection) => ((CardCollection)collection).CollectionChanged += special_CollectionChanged);
            specialStatus[player].ForEach(
                (collection) => ((CardCollection)collection).CollectionEmpty += special_CollectionEmpty);
            specialPoints.Add(player, 0);
            scoreBoard.Add(player, 0);
            ((Player)player).CardPlayed += player_CardPlayed;
            player.IsInitialized = true;
        }
         // called instead of InitializePlayer() for players already existing
        private void ResetPlayer(IHanafudaPlayer player)
        {
            handCards[player] = new List<DrawableCard>();
            //Create special collection and subscribe player to the event.
            specialStatus[player] = _board.GetSpecialCollection(player);
            specialStatus[player].ForEach(
                (collection) => ((CardCollection)collection).CollectionChanged += special_CollectionChanged);
            specialStatus[player].ForEach(
                (collection) => ((CardCollection)collection).CollectionEmpty += special_CollectionEmpty);
            SpecialPoints[player] = 0;
            scoreBoard[player] = 0;
        }
        
        protected virtual void OrderWaitingPlayers()
        {
            if (currentPlayer != null || turnQueue.Count > 0)
                new ArgumentException("Game in progress");
            foreach (IHanafudaPlayer player in waitList)
            {
                if (!player.IsInitialized)
                    InitializePlayer(player);
                else
                    ResetPlayer(player);
                turnQueue.Enqueue(player);
            }
            waitList.Clear();
        }

        
        /// <summary>
        /// put player back on the waiting list
        /// </summary>
        /// <param name="player"></param>
        protected virtual void RemovePlayerFromGame(IHanafudaPlayer player)
        {
            if (IsNotPlaying(player))
                return; // if never subscribed
            if (currentPlayer == player)
                currentPlayer = null;
            // if player is on the queue
            else
            {
                Queue<IHanafudaPlayer> q = new Queue<IHanafudaPlayer>();
                while (turnQueue.Count > 0)
                {
                    IHanafudaPlayer temp = turnQueue.Dequeue();
                    if (temp == player)
                        continue;
                    q.Enqueue(temp);
                }
                turnQueue = q;
            }
            //event
            var p = (Player)player;
            waitList.Add(player);
        }

        public bool IsNotPlaying(IHanafudaPlayer player)
        {
            return currentPlayer != player
                && !turnQueue.Contains(player);
        }


        #region Setting up DrawableCards Methods


        /// <summary>
        /// Initialize drawables
        /// Called right before every game when all cards are in deck
        /// </summary>
        private IEnumerable<DrawableCard> InitializeRevealedDrawables(IEnumerable<Hanafuda> hanafudas)
        {
            List<DrawableCard> drawables = new List<DrawableCard>();
            foreach (Hanafuda card in hanafudas)
            {
                DrawableCard drawable = cardFactory.ReturnPairedDrawable(card);
                drawable.Initialize();
                drawables.Add(drawable);
            }
            return drawables;
        }

        private IEnumerable<DrawableCard> InitializeTurnedDrawables(IEnumerable<Hanafuda> hanafudas)
        {
            List<DrawableCard> drawables = new List<DrawableCard>();
            foreach (Hanafuda card in hanafudas)
            {
                DrawableCard turnedCard = cardFactory.ReturnTurnedDrawable(card);
                turnedCard.Initialize();
                drawables.Add(turnedCard);
            }
            return drawables;
        }

        private void DiscardDrawables()
        {
            fieldCards.Keys.ToList().ForEach(key => fieldCards[key].Clear());
            handCards.Keys.ToList().ForEach(key => handCards[key].Clear());
            collectedCards.Keys.ToList().ForEach(key => collectedCards[key].Clear());
            cardFactory.ResizeImages();
        }
        #endregion

        #region Board events

        public event Action HandEmpty;

        private void OnHandEmpty()
        {
            HandEmpty?.Invoke();
        }

        protected virtual void OnNewPlayerTurn()
        {
            List<DrawableCard> hand = handCards[CurrentPlayer];
            CurrentPlayer.PlayCard(hand);
        }

        protected virtual void board_CardsDealt(object sender, DealCardEventArgs args)
        {
            IEnumerable<Hanafuda> cards = args.Cards;
            IHanafudaPlayer owner = args.Player;
            IEnumerable<DrawableCard> returnedDrawables = new List<DrawableCard>();
            if (owner == MainPlayer)
                returnedDrawables = InitializeRevealedDrawables(cards);
            else
                returnedDrawables = InitializeTurnedDrawables(cards);
            foreach (DrawableCard drawable in returnedDrawables)
            {
                PlaceCardOnHand(owner, drawable);
            }
        }

        protected virtual void board_CardsOnField(object sender, DealCardEventArgs args)
        {
            IEnumerable<Hanafuda> cards = args.Cards;
            IEnumerable<DrawableCard> returnedDrawables = InitializeRevealedDrawables(cards);
            foreach (DrawableCard drawable in returnedDrawables)
            {
                PlaceCardOnField(drawable);
            }
        }

        // When _board.PlayingCount == 0
        protected virtual void board_AllPlayerRemoved()
        {
            PostGame();
        }

        #endregion

        #region Player EventHandler

        protected virtual void player_MouseOverCard(object sender, PlayerEventArgs args)
        {
            //check for safe case to see if it was player triggering the event
            var mainP = (MainPlayer)sender;
            if (mainP == null || mainP != MainPlayer)
                return;
            var toOutline = args.Selected;
            if (toOutline != null)
            {
                loadedOutline = cardFactory.RetrieveOutline();
                loadedOutline.Position = toOutline.Position;
            }
            else // destroy loaded outline
            {
                cardFactory.RemoveOutline();
                loadedOutline = null;
            }
        }

        protected virtual void player_CardPlayed(object sender, PlayerEventArgs args)
        {
            // check if Drawable is valid
            var player = (IHanafudaPlayer)sender;
            var played = args.Selected;
            if (played == null || player == null)
                new ArgumentException("Wrong call to player_CardPlayed");
            PlayResult(played).Start();
        }
        #endregion

        #region Card Played

        private async Task PlayResult(DrawableCard played, float delay = 0.0f)
        {
            RemoveCardFromHand(CurrentPlayer, played);
            PlaceCardOnField(played);
            //scale outline
            outlineScale = new Vector2(2.0f);
            // place card on field from deck
            DrawableCard drawnCard = await PlayFromDeck();
            Month playedMonth = played.Card.Month;
            Month drawnMonth = drawnCard.Card.Month;
            int stack = fieldCards[playedMonth].Count;
            //didnt poop but month had 2 on field from beginning
            if (stack == 3 && drawnMonth != playedMonth)
            {
                //put played card to collection
                RemoveCardFromField(played);
                PlaceCardOnCollection(CurrentPlayer, played);
                List<DrawableCard> choices = fieldCards[playedMonth];
                PlaceCardOnChoice(choices);
                // TODO: make enlarge variable
                Task<DrawableCard> selectTask = CurrentPlayer.ChooseCard(choices);
                DrawableCard selected = await selectTask;
                //shrink back to original size
                foreach (DrawableCard enlarged in choices)
                    enlarged.Scale = Vector2.One;

                //selected card to collection
                RemoveCardFromField(selected);
                PlaceCardOnCollection(CurrentPlayer, selected);
                //put unselected card back on field
                DrawableCard unselected = choices.Find(x => x != selected);
                RemoveCardFromField(unselected);
                PlaceCardOnField(unselected);
                stack = fieldCards[playedMonth].Count;
            }
            if (stack == 2 || stack == 4)
            {
                for (; stack > 0; stack--)
                {
                    DrawableCard toMove = fieldCards[playedMonth][stack - 1];
                    RemoveCardFromField(toMove);
                    PlaceCardOnCollection(CurrentPlayer, toMove);
                }
            }
            // TODO: make to function
            // play from deck
            // same for both played and drawn
            stack = fieldCards[drawnMonth].Count;
            if (stack == 3 && drawnMonth != playedMonth)
            {
                //put played card to collection
                RemoveCardFromField(drawnCard);
                PlaceCardOnCollection(CurrentPlayer, drawnCard);
                List<DrawableCard> choices = fieldCards[drawnMonth];
                PlaceCardOnChoice(choices);
                Task<DrawableCard> selectTask = CurrentPlayer.ChooseCard(choices);
                DrawableCard selected = await selectTask;
                //shrink back to original size
                foreach (DrawableCard enlarged in choices)
                    enlarged.Scale = Vector2.One;
                //selected card to collection
                RemoveCardFromField(selected);
                PlaceCardOnCollection(CurrentPlayer, selected);
                //position unselected card back on field
                DrawableCard unselected = choices.Find(x => x != selected);
                // for reposition
                RemoveCardFromField(unselected);
                PlaceCardOnField(unselected);
                stack = fieldCards[drawnMonth].Count;
            }
            if (stack == 2 || stack == 4 && drawnMonth != playedMonth)
            {
                for (; stack > 0; stack--)
                {
                    DrawableCard toMove = fieldCards[drawnMonth][stack - 1];
                    RemoveCardFromField(toMove);
                    PlaceCardOnCollection(CurrentPlayer, toMove);
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(delay));
            //resize outline
            outlineScale = Vector2.One;
            if (handCards[CurrentPlayer].Count == 0)
                OnHandEmpty();
            EndTurn();
        }

        private async Task<DrawableCard> PlayFromDeck(float delay = 1.0f)
        {
            await Task.Delay(TimeSpan.FromSeconds(delay));
            IEnumerable<Hanafuda> drawnCard = DeckCollection.Instance.DrawCard();
            IEnumerable<DrawableCard> drawable = InitializeRevealedDrawables(drawnCard);
            DrawableCard drawn = drawable.First();
            PlaceCardOnField(drawn);
            //display in middle
            drawn.Position = new Vector2(GraphicsDevice.Viewport.Width / 2 + 150, GraphicsDevice.Viewport.Height / 2);
            drawn.Scale = new Vector2(2.0f);
            await Task.Delay(TimeSpan.FromSeconds(delay));
            //move to field
            drawn.Position = GetFieldLocation(drawn.Card.Month);
            drawn.Scale = Vector2.One;
            await Task.Delay(TimeSpan.FromSeconds(delay));
            return drawn;
        }

        public virtual void EndTurn()
        {
            if (turnQueue.Count == 0)
            {
                PostGame();
                return;
            }
            // currentPlayer is set to null when hand is empty
            if (CurrentPlayer != null)
                turnQueue.Enqueue(CurrentPlayer);
            CurrentPlayer = turnQueue.Dequeue();
        }


        private void PostGame()
        {
            IHanafudaPlayer winner = scoreBoard.OrderByDescending(kevalue => kevalue.Value).First().Key;
            if (MainPlayer == winner)
            {
                resultText = "You Win!";
                resultColor = Color.Blue;
            }
            else
            {
                resultText = "You Lose...";
                resultColor = Color.Red;
            }
            resultText += "\nClick left to try again";
            isPostGame = true; //Temporary hack for quick update loop at end of the game
        }
        #endregion

        #region Card Location Methods

        private void PlaceCardOnHand(IHanafudaPlayer owner, DrawableCard drawable)
        {
            drawable.Position = GetHandLocation(owner);
            handCards[owner].Add(drawable);
        }
        private Vector2 GetHandLocation(IHanafudaPlayer owner)
        {
            int slot = handCards[owner].Count;
            Vector2 position = new Vector2(50.0f * slot, 45.0f);
            position.X += 2 * slot + 30;
            if (owner == MainPlayer)
                position.Y += GraphicsDevice.Viewport.Height - 90.0f;
            return position;
        }

        private void PlaceCardOnField(DrawableCard drawable)
        {
            Month month = drawable.Card.Month;
            fieldCards[month].Add(drawable);
            drawable.Position = GetFieldLocation(month);
        }
        private Vector2 GetFieldLocation(Month month)
        {
            int slot = (int)month;
            int xSlot = (slot % 6);
            int ySlot = (slot / 6);
            int yOffset = GraphicsDevice.Viewport.Height / 2 - 100;
            Vector2 position = new Vector2(40.0f + (80.0f * xSlot) + (fieldCards[month].Count * 6),
                yOffset + 200 * ySlot);
            return position;
        }

        private void PlaceCardOnChoice(List<DrawableCard> choices)
        {
            for (int i = 0; i < 2; i++)
            {
                choices[i].Scale = new Vector2(2.0f);
                choices[i].Position = GetChoiceLocation(i);
            }
        }
        private Vector2 GetChoiceLocation(int count)
        {
            float x = Game.GraphicsDevice.Viewport.Width / 2 + 40 + 120 * count;
            float y = Game.GraphicsDevice.Viewport.Height / 2;
            return new Vector2(x, y);
        }

        private void PlaceCardOnCollection(IHanafudaPlayer owner, DrawableCard drawable)
        {
            drawable.Position = GetCollectionLocation(owner, drawable);
            drawable.Scale = new Vector2(0.5f);
            collectedCards[owner].Add(drawable);
            CollectCard(owner, drawable);
        }
        private Vector2 GetCollectionLocation(IHanafudaPlayer owner, DrawableCard drawable)
        {
            int xSlot = collectedCards[owner].Count % 6;
            int ySlot = collectedCards[owner].Count / 6;
            Vector2 loc = new Vector2(Game.GraphicsDevice.Viewport.Width - 24.0f * xSlot - 20.0f, 40.0f * ySlot + 40.0f);
            if (owner == MainPlayer)
                loc.Y += GraphicsDevice.Viewport.Height - 70.0f - 80.0f * ySlot;
            return loc;
        }
        private void CollectCard(IHanafudaPlayer owner, DrawableCard wonCard)
        {
            foreach (SpecialCards collection in specialStatus[owner])
                collection.OnCardCollected(wonCard);
            // get point according to the game mode in play
            scoreBoard[owner] += _board.CalculatePoint(owner, wonCard.Card);
        }

        /* TODO: display logic for special cards.
        private void PlaceWonSpecial(DrawableCard card, Type special)
        {
        }
        private Vector2 GetSpecialLocation(Type special)
        {
            return Vector2.One;
        }
        */

        private void RemoveCardFromHand(IHanafudaPlayer player, DrawableCard drawable)
        {
            handCards[player].Remove(drawable);

            if (player != MainPlayer)
                cardFactory.FlipCard(drawable);
        }
        private void RemoveCardFromField(DrawableCard drawable)
        {
            Month month = drawable.Card.Month;
            fieldCards[month].Remove(drawable);
        }
        #endregion


        protected virtual void special_CollectionChanged(object sender, EventArgs args)
        {
            Type type = sender.GetType();
            SpecialChangedEventArgs specialArg = (SpecialChangedEventArgs)args;
            if (!(type.IsInstanceOfType(typeof(SpecialCards)) || specialArg == null))
                new ArgumentException("Not a Special Collection");
            specialArg.Match.OnSpecialCollected();
        }
        protected virtual void special_CollectionEmpty(object sender, EventArgs args)
        {
            Type type = sender.GetType();
            SpecialEmptyEventArgs specialArg = (SpecialEmptyEventArgs)args;
            if (!(type.IsInstanceOfType(typeof(SpecialCards)) || specialArg == null))
                new ArgumentException("Not a Special Collection");
            IHanafudaPlayer owner = specialArg.Owner;
            int point = specialArg.Points;
            specialPoints[owner] += point;
            scoreBoard[owner] += point;
        }
    }
}