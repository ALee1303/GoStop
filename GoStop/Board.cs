﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using GoStop.Card;
using GoStop.Collection;
using System.Threading.Tasks;

using GoStop.MonoGameComponents;

namespace GoStop
{
    public class Board : IBoard
    {
        private int playingCount;
        //cards
        protected Dictionary<Month, CardCollection> field;
        protected Dictionary<IHanafudaPlayer, CollectedCards> collected;
        protected Dictionary<IHanafudaPlayer, int> scoreBoard;
        //players
        protected IHanafudaPlayer currentPlayer;
        protected List<IHanafudaPlayer> playerWaitList;
        protected Queue<IHanafudaPlayer> orderedPlayers;

        protected BoardManager _manager;

        public IHanafudaPlayer CurrentPlayer
        {
            get => currentPlayer;
            private set
            {
                if (currentPlayer == value)
                    return;
                currentPlayer = value;
                if (currentPlayer != null)
                    OnNewPlayerTurn();
            }
        }
        // Adjusted in subscribe and unsubscribe
        public int PlayingCount
        {
            get => playingCount;
            private set
            {
                if (playingCount == value)
                    return;
                playingCount = value;
                if (playingCount == 0)
                    OnAllPlayerRemoved();
            }
        }

        public Board(BoardManager manager)
        {
            field = new Dictionary<Month, CardCollection>();
            InitializeField();
            scoreBoard = new Dictionary<IHanafudaPlayer, int>();
            collected = new Dictionary<IHanafudaPlayer, CollectedCards>();
            playerWaitList = new List<IHanafudaPlayer>();
            orderedPlayers = new Queue<IHanafudaPlayer>();
            _manager = manager;
        }

        private void InitializeField()
        {
            for (int i = 0; i < 12; i++)
            {
                field.Add((Month)i, new CardCollection());
            }
        }

        #region Prepare Game

        // TODO:DealCard, GameResult
        protected virtual void PrepareGame()
        {
            OrderWaitingPlayers();
            DealCard();
        }

        protected virtual void OrderWaitingPlayers()
        {
            if (currentPlayer != null || orderedPlayers.Count > 0)
                new ArgumentException("Game in progress");
            foreach (IHanafudaPlayer player in playerWaitList)
                AddPlayer(player);
            playerWaitList.Clear();
        }

        #endregion

        #region public methods for starting game on loaded board

        public virtual void StartGame()
        {
            PrepareGame();
            CurrentPlayer = orderedPlayers.Dequeue();
        }

        public virtual void EndGame()
        {
            //in case game ends early
            foreach (IHanafudaPlayer player in orderedPlayers)
            {
                player.RenewHandAndSpecial();
                RemovePlayer(player);
            }
            ResetBoard();
        }

        public virtual void ResetBoard()
        {
            DeckCollection.Instance.GatherCards();
            field.Clear();
            collected = new Dictionary<IHanafudaPlayer, CollectedCards>();
            scoreBoard = new Dictionary<IHanafudaPlayer, int>();
        }

        #endregion

        #region Deal Card
        /// <summary>
        /// Deal card to all waiting players
        /// Queueing them into the Game
        /// </summary>
        protected virtual void DealCard()
        {
            for (int i = 0; i < 2; i++)
            {
                DealCardsOnField();
                foreach (IHanafudaPlayer player in orderedPlayers)
                    DealCard(player);
            }
        }

        /// <summary>
        /// Deal card to specific player of specific amount
        /// Overload used inside DealCard()
        /// </summary>
        /// <param name="player"></param>
        /// <param name="amount">2p = 5, 3p = 4 then 3</param>
        private void DealCard(IHanafudaPlayer player, int amount = 5)
        {
            IEnumerable<Hanafuda> draws = DeckCollection.Instance.DrawCard(amount);
            DealCardEventArgs args = new DealCardEventArgs();
            args.Player = player;
            args.Cards = draws;
            OnCardsDealt(args);
            // ToDo : remove
            //foreach (Hanafuda drawn in draws)
            //{
            //    drawn.Owner = player;
            //    drawn.Location = Location.Hand;
            //}
            player.Hand.Add(draws);
        }

        /// <summary>
        /// Spread cards on dictionary of cards based on their month
        /// </summary>
        /// <param name="amount">2p = 4, 3p = 3</param>
        protected void DealCardsOnField(int amount = 4)
        {
            IEnumerable<Hanafuda> draws = DeckCollection.Instance.DrawCard(amount);
            DealCardEventArgs args = new DealCardEventArgs();
            args.Cards = draws;
            // update board manager's field
            OnCardsOnField(args);
            //update board's own field
            OrganizeField(draws);
        }

        protected void OrganizeField(IEnumerable<Hanafuda> draws)
        {
            foreach (Hanafuda drawn in draws)
            {
                drawn.Location = Location.Field;
                field[drawn.Month].Add(drawn);
            }
        }
        #endregion

        #region Subscriber

        /// <summary>
        /// Add new player to waiting list
        /// </summary>
        /// <param name="player"></param>
        public virtual void SubscribePlayer(IHanafudaPlayer player)
        {
            if (!IsNewPlayer(player) || playerWaitList.Remove(player))
                new ArgumentException("Can't Add: Player Already Exist");
            playerWaitList.Add(player);
        }

        /// <summary>
        /// Remove player from the board
        /// </summary>
        /// <param name="player"></param>
        public virtual void UnsubscribePlayer(IHanafudaPlayer player)
        {
            if (IsNewPlayer(player) || !playerWaitList.Remove(player))
                new ArgumentException("Can't Remove: Player Doesn't Exist");
            RemovePlayer(player);
            playerWaitList.Remove(player);
        }

        /// <summary>
        /// Subscribe waiting player to the game
        /// </summary>
        /// <param name="player"></param>
        protected virtual void AddPlayer(IHanafudaPlayer player)
        {
            if (!IsNewPlayer(player))
                return;
            if (currentPlayer != null)
                new ArgumentException("Can't Join: Game in progress");
            //add to queue
            orderedPlayers.Enqueue(player);
            //add scoreBoard
            scoreBoard.Add(player, 0);
            collected.Add(player, new CollectedCards());
            //event
            player.PrepareSpecialCollection();
            player.SubscribeSpecialEmptyEvent(collection_SpecialEmpty);
            var p = (Player)player;
            if (p != null)
            {
                p.HandEmpty += player_HandEmpty;
            }
            PlayingCount++;
        }

        /// <summary>
        /// put player back on the waiting list
        /// </summary>
        /// <param name="player"></param>
        protected virtual void RemovePlayer(IHanafudaPlayer player)
        {
            if (IsNewPlayer(player))
                return; // if never subscribed
            if (currentPlayer == player)
                currentPlayer = null;
            else
            {
                Queue<IHanafudaPlayer> q = new Queue<IHanafudaPlayer>();
                while (orderedPlayers.Count > 0)
                {
                    IHanafudaPlayer temp = orderedPlayers.Dequeue();
                    if (temp == player)
                        continue;
                    q.Enqueue(temp);
                }
                orderedPlayers = q;
            }
            scoreBoard.Remove(player);
            collected.Remove(player);
            //event
            player.UnsubscribeSpecialEmptyEvent(collection_SpecialEmpty);
            var p = (Player)player;
            if (p != null)
            {
                p.HandEmpty -= player_HandEmpty;
            }
            playerWaitList.Add(player);
            PlayingCount--;
        }

        #endregion

        #region EventHandler

        #endregion

        protected virtual void player_HandEmpty(object sender, EventArgs args)
        { }

        /// <summary>
        /// Not called on Clear()
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arg"></param>
        protected virtual void collection_SpecialEmpty(object sender, EventArgs arg)
        { }
        

        #region Manager EventHandler

        public event Action NewPlayerTurn;
        public event Action AllPlayerRemoved;

        public event EventHandler<DealCardEventArgs> CardsDealt;
        public event EventHandler<DealCardEventArgs> CardsOnField;
        public event EventHandler<MultipleMatchEventArgs> MultipleMatch;

        protected virtual void OnNewPlayerTurn()
        {
            NewPlayerTurn?.Invoke();
        }

        protected virtual void OnAllPlayerRemoved()
        {
            AllPlayerRemoved?.Invoke();
        }

        protected virtual void OnCardsDealt(DealCardEventArgs args)
        {
            CardsDealt?.Invoke(this, args);
        }

        protected virtual void OnCardsOnField(DealCardEventArgs args)
        {
            CardsOnField?.Invoke(this, args);
        }

        protected virtual void OnMultipleMatch(MultipleMatchEventArgs args)
        {
            MultipleMatch?.Invoke(this, args);
        }


        #endregion

        public bool IsNewPlayer(IHanafudaPlayer player)
        {
            return currentPlayer != player
                && !orderedPlayers.Contains(player);
        }
    }

    public class DealCardEventArgs : EventArgs
    {
        public IHanafudaPlayer Player { get; set; }
        public IEnumerable<Hanafuda> Cards { get; set; }
    }

    public class MultipleMatchEventArgs : EventArgs
    {
        public Month Month { get; set; }
    }
}
