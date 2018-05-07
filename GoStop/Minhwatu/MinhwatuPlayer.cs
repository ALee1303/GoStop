﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GoStop.Collection;

namespace GoStop.Minhwatu
{
    public class MinhwatuPlayer : Player
    {
        public MinhwatuPlayer() : base()
        {
            specials = new List<SpecialCards>
            {
                new ChoYak(this), new PoongYak(this), new BiYak(this),
                new ChoDanCollection(this), new ChungDanCollection(this), new HongDanCollection(this)
            };
        }

        public override void TakeTurn()
        {
            base.TakeTurn();
        }

        public Task TakeTurnTask()
        {

        }

        public override void PrepareSpecialCollection()
        {
            specials = new List<SpecialCards>
            {
                new ChoYak(this), new PoongYak(this), new BiYak(this),
                new ChoDanCollection(this), new ChungDanCollection(this), new HongDanCollection(this)
            };
        }
    }
}