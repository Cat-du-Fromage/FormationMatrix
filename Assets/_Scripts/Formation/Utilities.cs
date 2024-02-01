using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine;

namespace Kaizerwald.FormationModule
{
    public static class Utilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveAtSwapBack<T>(this IList<T> list, int index, out T swappedValue)
        {
            int lastIndex = list.Count - 1;
            if (lastIndex > 0 && lastIndex != index)
            {
                swappedValue = list[index] = list[lastIndex];
            }
            else
            {
                swappedValue = default;
            }
            list.RemoveAt(lastIndex);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveAtSwapBack<T>(this IList<T> list, int index)
        {
            int lastIndex = list.Count - 1;
            if (lastIndex > 0 && lastIndex != index)
            {
                list[index] = list[lastIndex];
            }
            list.RemoveAt(lastIndex);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap<TKey, TValue>(this Dictionary<TKey, TValue> dico, TKey lhs, TKey rhs)
        where TKey : class
        {
            if (lhs == rhs) return;
            (dico[lhs], dico[rhs]) = (dico[rhs], dico[lhs]);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap<T>(this IList<T> list, int lhs, int rhs)
        {
            (list[lhs], list[rhs]) = (list[rhs], list[lhs]);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap<T>(this NativeList<T> list, int lhs, int rhs)
        where T : unmanaged
        {
            (list[lhs], list[rhs]) = (list[rhs], list[lhs]);
        }
    }
}
