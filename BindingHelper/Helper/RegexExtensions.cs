using System;
using System.Text.RegularExpressions;

namespace BindingHelper
{
    public static class RegexExtensions
    {
        public static Match[] GetAllMatches(this MatchCollection matches)
        {
            Match[] matchArray = new Match[matches.Count];
            matches.CopyTo(matchArray, 0);

            return matchArray;
        }
    }
}
