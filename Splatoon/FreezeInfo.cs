﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon
{
    internal class FreezeInfo
    {
        internal HashSet<DisplayObject> Objects;
        internal long ShowUntil;

        internal bool IsActive()
        {
            return ShowUntil > Environment.TickCount64;
        }
    }
}
