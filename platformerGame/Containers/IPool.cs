﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace platformerGame.Containers
{
    interface IPool
    {
        List<IDrawable> ListVisibles(AABB visible_region);
    }
}
