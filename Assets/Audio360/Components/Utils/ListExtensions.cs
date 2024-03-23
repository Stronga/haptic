// Copyright (c) 2018-present, Facebook, Inc. 


using System.Collections.Generic;

namespace Facebook.Audio
{
    public static class ListExtensions
    {
        /// <summary>
        /// Faster remove for when you don't need to keep the list in order.
        /// </summary>
        /// <typeparam name="T">The type contained in the list (inferred)</typeparam>
        /// <param name="list">The list (inferred)</param>
        /// <param name="index">The index of the element to remove</param>
        public static void FastRemoveAt<T>(this List<T> list, int index)
        {
            int lastIdx = list.Count - 1;
            if (index < lastIdx)
            {
                list[index] = list[lastIdx];
            }

            list.RemoveAt(lastIdx);
        }
    }
}
