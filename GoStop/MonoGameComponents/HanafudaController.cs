﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

using MG_Library;
using GoStop.Card;

namespace GoStop.MonoGameComponents
{
    public class HanafudaController : GameComponent
    {
        private IInputManager inputManager;

        public HanafudaController(Game game) : base(game)
        {
            inputManager = Game.Services.GetService<IInputManager>();
        }

        
    }
}
