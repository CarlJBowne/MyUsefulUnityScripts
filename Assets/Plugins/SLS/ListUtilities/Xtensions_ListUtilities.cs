using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SLS.ListUtilities
{
    public static class Xtensions_ListUtilities
    {
        /// <summary>
        /// A Session safe and easier Hash Code Extension.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static int Hash(this string input) => Animator.StringToHash(input);
    }
}
