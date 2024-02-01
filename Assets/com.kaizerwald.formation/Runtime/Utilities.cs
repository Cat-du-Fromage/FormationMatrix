using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

namespace Kaizerwald.FormationModule
{
    internal static class Utilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RemoveAtSwapBack<T>(this IList<T> list, int index)
        {
            int lastIndex = list.Count - 1;
            if (lastIndex > 0 && lastIndex != index)
            {
                list[index] = list[lastIndex];
            }
            list.RemoveAt(lastIndex);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Swap<TKey, TValue>(this Dictionary<TKey, TValue> dico, TKey lhs, TKey rhs)
        where TKey : class
        {
            if (lhs == rhs) return;
            (dico[lhs], dico[rhs]) = (dico[rhs], dico[lhs]);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Swap<T>(this IList<T> list, int lhs, int rhs)
        {
            if (lhs == rhs) return;
            (list[lhs], list[rhs]) = (list[rhs], list[lhs]);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Swap<T>(this NativeList<T> list, int lhs, int rhs)
        where T : unmanaged
        {
            if (lhs == rhs) return;
            (list[lhs], list[rhs]) = (list[rhs], list[lhs]);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsZero(this float lhs)
        {
            return lhs.IsAlmostEqual(0);
            //return Approximately(lhs, 0);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsZero(this float2 lhs)
        {
            return lhs.x.IsZero() && lhs.y.IsZero();
            //return Approximately(lhs.x, 0) && Approximately(lhs.y, 0);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsZero(this float3 lhs)
        {
            return lhs.x.IsZero() && lhs.y.IsZero() && lhs.z.IsZero();
            //return Approximately(lhs.x, 0) && Approximately(lhs.y, 0) && Approximately(lhs.z, 0);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsAlmostEqual(this float lhs, float rhs)
        {
            return abs(rhs - lhs) < max(0.000001f * max(abs(lhs), abs(rhs)), EPSILON * 8);
            //return Approximately(lhs.x, rhs.x) && Approximately(lhs.y, rhs.y);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsAlmostEqual(this float2 lhs, float2 rhs)
        {
            return lhs.x.IsAlmostEqual(rhs.x) && lhs.y.IsAlmostEqual(rhs.y);
            //return Approximately(lhs.x, rhs.x) && Approximately(lhs.y, rhs.y);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsAlmostEqual(this float3 lhs, float3 rhs)
        {
            return lhs.x.IsAlmostEqual(rhs.x) && lhs.y.IsAlmostEqual(rhs.y) && lhs.z.IsAlmostEqual(rhs.z);
            //return Approximately(lhs.x, rhs.x) && Approximately(lhs.y, rhs.y) && Approximately(lhs.z, rhs.z);
        }
    }
}
