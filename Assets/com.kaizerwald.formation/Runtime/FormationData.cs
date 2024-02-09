using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

using static UnityEngine.Mathf;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;
using half = Unity.Mathematics.half;

namespace Kaizerwald.FormationModule
{
    [Serializable]
    public struct FormationData : IEquatable<FormationData>
    {
        //Taille actuelle 16 Bytes/Octets
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                                ◆◆◆◆◆◆ FIELD ◆◆◆◆◆◆                                                 ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        private readonly byte   minRow; 
        private readonly byte   maxRow;
        private readonly half   spaceBetweenUnits;
        private readonly half2  unitSize;
        
        private readonly ushort numUnitsAlive;
        private readonly byte   width;
        private readonly byte   depth;
        private readonly half2  direction2DForward;
        
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Accessors ◈◈◈◈◈◈                                                                                        ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        public readonly float SpaceBetweenUnits => spaceBetweenUnits;
        public readonly float2 UnitSize => unitSize;
        public readonly int NumUnitsAlive => numUnitsAlive;
        
        public readonly int Width => width;
        public readonly int Depth => depth;
        public readonly int2 WidthDepth => new int2(width, depth);
        
        public readonly int MinRow => min((int)minRow, numUnitsAlive);
        public readonly int MaxRow => min((int)maxRow, numUnitsAlive);
        public readonly int2 MinMaxRow => new int2(MinRow, MaxRow);
        
        public readonly float2 Direction2DForward => direction2DForward;
        public readonly float2 Direction2DBack => -(float2)direction2DForward;
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                             ◆◆◆◆◆◆ CONSTRUCTOR ◆◆◆◆◆◆                                              ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        // Base Constructor
        private FormationData(ushort numUnitsAlive, byte minRow, byte maxRow, half2 unitSize, half spaceBetweenUnits, byte width, byte depth, half2 direction2DForward)
        {
            this.minRow = minRow;
            this.maxRow = maxRow;
            this.spaceBetweenUnits = spaceBetweenUnits;
            this.unitSize = unitSize;
            this.numUnitsAlive = numUnitsAlive;
            this.width = width;
            this.depth = depth;
            this.direction2DForward = direction2DForward;
        }
        
        public FormationData(int numUnits, int2 minMaxRow = default, float2 unitSize = default, float spaceBetweenUnit = 1f, float3 direction = default, int defaultWidth = 0)
        {
            int numberUnits = max(1,numUnits);
            numUnitsAlive = (ushort)numberUnits;
            
            int2 minMax = minMaxRow.Equals(default) ? new int2(1, max(1, numberUnits / 2)) : minMaxRow;
            minRow = (byte)min(byte.MaxValue, minMax.x);
            maxRow = (byte)min(byte.MaxValue, minMax.y);
            
            this.unitSize = unitSize.Equals(default) ? half2(1) : (half2)unitSize;
            spaceBetweenUnits = (half)spaceBetweenUnit;
            
            width = (byte)(numUnitsAlive < minRow ? numUnitsAlive : clamp(defaultWidth, minRow, maxRow));
            depth = (byte)ceil(numberUnits / max(1f,width));
            
            direction2DForward = direction.IsAlmostEqual(default) ? new half2(half.zero,half(1)) : half2(normalizesafe(direction).xz);
        }
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ From FormationData ◈◈◈◈◈◈                                                                               ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        private FormationData(in FormationData other)
        {
            minRow = other.minRow;
            maxRow = other.maxRow;
            spaceBetweenUnits = other.spaceBetweenUnits;
            unitSize = other.unitSize;
            numUnitsAlive = other.numUnitsAlive;
            width = other.width;
            depth = other.depth;
            direction2DForward = other.direction2DForward;
        }
        
        public FormationData(in FormationData other, int numUnits, int newWidth, float3 direction) 
        {
            minRow = other.minRow;
            maxRow = other.maxRow;
            spaceBetweenUnits = half(other.SpaceBetweenUnits);
            unitSize = half2(other.UnitSize);
            numUnitsAlive = (ushort)max(0,numUnits);
            width = (byte)(numUnitsAlive < minRow ? numUnitsAlive : clamp(newWidth, minRow, maxRow));
            depth = (byte)ceil(numUnitsAlive / max(1f,width));
            direction2DForward = half2(direction.xz);
        }

        public FormationData(in FormationData other, int numUnits) : this(other, numUnits, other.width, other.Direction3DForward) { }
        
        public FormationData(in FormationData other, int newWidth, float3 direction) : this(other, other.numUnitsAlive, newWidth, direction) { }
        
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ From Formation ◈◈◈◈◈◈                                                                                   ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        public FormationData(Formation other)
        {
            minRow = (byte)other.MinRow;
            maxRow = (byte)other.MaxRow;
            spaceBetweenUnits = half(other.SpaceBetweenUnits);
            unitSize = half2(other.UnitSize);
            numUnitsAlive = (ushort)max(0,other.NumUnitsAlive);
            width = (byte)(numUnitsAlive < minRow ? numUnitsAlive : clamp(other.Width, minRow, maxRow));
            depth = (byte)ceil(numUnitsAlive / max(1f,width));
            direction2DForward = half2(other.Direction2DForward);
        }
        
        public FormationData(Formation other, int numUnits, int newWidth, float3 direction)
        {
            minRow = (byte)other.MinRow;
            maxRow = (byte)other.MaxRow;
            spaceBetweenUnits = half(other.SpaceBetweenUnits);
            unitSize = half2(other.UnitSize);
            numUnitsAlive = (ushort)max(0,numUnits);
            width = (byte)(numUnitsAlive < minRow ? numUnitsAlive : clamp(newWidth, minRow, maxRow));
            depth = (byte)ceil(numUnitsAlive / max(1f,width));
            direction2DForward = half2(direction.xz);
        }

        public FormationData(Formation other, int numUnits) : this(other, numUnits, other.Width, other.DirectionForward) { }
        
        public FormationData(Formation other, int newWidth, float3 direction) : this(other, other.NumUnitsAlive, newWidth, direction) { }
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                               ◆◆◆◆◆◆ METHODS ◆◆◆◆◆◆                                                ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝
        
        public readonly float2 DistanceUnitToUnit => UnitSize + SpaceBetweenUnits;
        public readonly float DistanceUnitToUnitX => DistanceUnitToUnit.x;
        public readonly float DistanceUnitToUnitY => DistanceUnitToUnit.y;
        
        public readonly int LastRowFirstIndex => NumUnitsAlive - NumUnitsLastLine;
        
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Direction ◈◈◈◈◈◈                                                                                        ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        public readonly float3 Direction3DForward => new (direction2DForward.x,0,direction2DForward.y);
        public readonly float3 Direction3DBack => -Direction3DForward;
        public readonly float3 Direction3DLine => cross(up(), Direction3DForward);
        
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Rearrangement ◈◈◈◈◈◈                                                                                    ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        private readonly int CountUnitsLastLine => numUnitsAlive - NumCompleteLine * width;
        public readonly bool IsLastLineComplete => NumCompleteLine == depth;
        public readonly int NumCompleteLine => Depth * Width == numUnitsAlive ? depth : depth - 1;
        public readonly int NumUnitsLastLine => IsLastLineComplete ? width : CountUnitsLastLine;
        
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Operators ◈◈◈◈◈◈                                                                                        ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator FormationData(Formation rhs)
        {
            return new FormationData(rhs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(FormationData lhs, FormationData rhs)
        {
            return lhs.numUnitsAlive == rhs.numUnitsAlive && 
                   lhs.width == rhs.width && lhs.depth == rhs.depth && 
                   lhs.minRow == rhs.minRow && lhs.maxRow == rhs.maxRow && 
                   lhs.SpaceBetweenUnits.IsAlmostEqual(rhs.SpaceBetweenUnits) &&
                   lhs.UnitSize.IsAlmostEqual(rhs.UnitSize) && 
                   lhs.Direction2DForward.IsAlmostEqual(rhs.Direction2DForward);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(FormationData lhs, FormationData rhs)
        {
            return !(lhs == rhs);
        }

    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ IEquatable ◈◈◈◈◈◈                                                                                       ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        public bool Equals(FormationData other)
        {
            return minRow == other.minRow && maxRow == other.maxRow &&
                   width == other.width && depth == other.depth &&
                   numUnitsAlive == other.numUnitsAlive && 
                   SpaceBetweenUnits.IsAlmostEqual(other.spaceBetweenUnits) &&
                   UnitSize.IsAlmostEqual(other.UnitSize) &&
                   Direction2DForward.IsAlmostEqual(other.Direction2DForward);
        }

        public override bool Equals(object obj)
        {
            return obj is FormationData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(minRow, maxRow, unitSize, spaceBetweenUnits, numUnitsAlive, width, depth, direction2DForward);
        }
        
        public override string ToString()
        {
            return $"Current formation:\r\n" +
                   $"minRow {minRow}\r\n" +
                   $"maxRow {maxRow}\r\n" +
                   $"unitSize {unitSize}\r\n" +
                   $"spaceBetweenUnits {spaceBetweenUnits}\r\n" +
                   $"numUnitsAlive {numUnitsAlive}\r\n" +
                   $"width {width}\r\n" +
                   $"depth {depth}\r\n" +
                   $"direction2DForward {direction2DForward}\r\n";
        }
    }
}
