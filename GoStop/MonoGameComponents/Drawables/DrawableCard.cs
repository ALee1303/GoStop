﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

using MG_Library;
using GoStop.Card;

namespace GoStop.MonoGameComponents.Drawables
{
    public class DrawableCard : DrawableGameComponent
    {
        private Hanafuda _card;
        private Sprite2D _image;

        public DrawableCard(Game game, Hanafuda card, Sprite2D image) : base(game)
        {
            _card = card;
            _card.OwnerChanged += card_OwnerChanged;
            _card.HiddenChanged += card_HiddenChanged;
            _card.LocationChanged += card_LocationChanged;
            _image = image;
        }

        #region Drawable Override

        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            base.LoadContent();
        }

        protected override void OnDrawOrderChanged(object sender, EventArgs args)
        {
            base.OnDrawOrderChanged(sender, args);
        }

        protected override void OnVisibleChanged(object sender, EventArgs args)
        {
            base.OnVisibleChanged(sender, args);
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();
        }

        #endregion

        #region Event

        protected virtual void card_OwnerChanged(object sender, HanafudaEventArgs args)
        { }
        
        protected virtual void card_HiddenChanged(object sender, HanafudaEventArgs args)
        { }

        protected virtual void card_LocationChanged(object sender, HanafudaEventArgs args)
        { }

        #endregion
    }
}
