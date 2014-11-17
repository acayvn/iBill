﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyncBillingBase.DAL
{
    public static class MatchingExtention
    {
        public static bool In<T>(this T x, params T[] values)
        {
            foreach (T value in values)
            {
                if (x.Equals(value))
                    return true;
            }
            return false;
        }
    }
}
