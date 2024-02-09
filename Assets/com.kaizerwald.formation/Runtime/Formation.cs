using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

using static Unity.Mathematics.float2;
using float2 = Unity.Mathematics.float2;
using float3 = Unity.Mathematics.float3;

namespace Kaizerwald.FormationModule
{
    public class Formation
    {
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                                ◆◆◆◆◆◆ FIELD ◆◆◆◆◆◆                                                 ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝
        public readonly int BaseNumUnits;
        public readonly int MinRow; 
        public readonly int MaxRow;
        public readonly float2 UnitSize;
        public readonly float SpaceBetweenUnits;
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                             ◆◆◆◆◆◆ Properties ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝
        public int NumUnitsAlive { get; private set; }
        public int Width { get; private set; }
        public int Depth { get; private set; }
        public float3 DirectionForward { get; private set; }
        public int2 MinMaxRow => int2(MinRow, MaxRow);

//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                             ◆◆◆◆◆◆ CONSTRUCTOR ◆◆◆◆◆◆                                              ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        public Formation(int numUnits, int2 minMaxRow = default, float2 unitSize = default, float spaceBetweenUnit = 1f, float3 direction = default, int width = 0)
        {
            BaseNumUnits = NumUnitsAlive = max(1,numUnits);
            int2 minMax = minMaxRow.Equals(default) ? new int2(1, max(1, BaseNumUnits / 2)) : minMaxRow;
            MinRow = (byte)min(byte.MaxValue, minMax.x);
            MaxRow = (byte)min(byte.MaxValue, minMax.y);
            UnitSize = unitSize.IsAlmostEqual(default) ? float2(1) : unitSize;
            SpaceBetweenUnits = spaceBetweenUnit;
            Width = BaseNumUnits < MinRow ? BaseNumUnits : clamp(width, MinRow, MaxRow);
            Depth = (int)ceil(BaseNumUnits / max(1f,Width));
            DirectionForward = direction.IsAlmostEqual(default) ? forward() : normalizesafe(direction);
        }
        
        public Formation(in FormationData other)
        {
            BaseNumUnits = NumUnitsAlive = other.NumUnitsAlive;
            MinRow = other.MinRow;
            MaxRow = other.MaxRow;
            UnitSize = other.UnitSize;
            SpaceBetweenUnits = other.SpaceBetweenUnits;
            Width = other.Width;
            Depth = other.Depth;
            DirectionForward = other.Direction3DForward;
        }
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                               ◆◆◆◆◆◆ METHODS ◆◆◆◆◆◆                                                ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝
        public int2 WidthDepth => int2(Width, Depth);
        public float2 Direction2DForward => DirectionForward.xz;
        public float3 DirectionLine => cross(up(), DirectionForward);//cross(DirectionForward, up());
        public float2 DistanceUnitToUnit => UnitSize + SpaceBetweenUnits;
        public float DistanceUnitToUnitX => DistanceUnitToUnit.x;
        public float DistanceUnitToUnitY => DistanceUnitToUnit.y;
        public int LastRowFirstIndex => NumUnitsAlive - NumUnitsLastLine;
        
        // Needed for Rearrangement
        public int CountUnitsLastLine => NumUnitsAlive - NumCompleteLine * Width;
        public bool IsLastLineComplete => NumCompleteLine == Depth;
        public int NumCompleteLine => Depth * Width == NumUnitsAlive ? Depth : Depth - 1;
        public int NumUnitsLastLine => IsLastLineComplete ? Width : CountUnitsLastLine;
        
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Setters ◈◈◈◈◈◈                                                                                          ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜

        public void SetFromFormation(in FormationData formationData)
        {
            NumUnitsAlive = formationData.NumUnitsAlive;
            Width = min(formationData.Width, formationData.NumUnitsAlive);
            Depth = (int)ceil(formationData.NumUnitsAlive / max(1f,formationData.Width));
            DirectionForward = formationData.Direction3DForward;
        }
    
        public void Increment() => Add(1);
        public void Decrement() => Remove(1);
        
        //┌────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
        //│  ◇◇◇◇◇◇ Increase/Decrease Formation ◇◇◇◇◇◇                                                                 │
        //└────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
        public bool Add(int numAdded)
        {
            if (numAdded == 0) return false;
            NumUnitsAlive = min(BaseNumUnits, NumUnitsAlive + numAdded);
            Depth = (int)ceil(NumUnitsAlive / (float)Width);
            return true;
        }

        public bool Remove(int numRemoved)
        {
            if (numRemoved == 0) return false;
            NumUnitsAlive = max(0, NumUnitsAlive - numRemoved);
            Width = min(Width, NumUnitsAlive);
            Depth = (int)ceil(NumUnitsAlive / max(1f,Width));
            return true;
        }

        public bool SetNumUnits(int value)
        {
            if (value == NumUnitsAlive) return false;
            return value < NumUnitsAlive ? Remove(NumUnitsAlive - value) : Add(value - NumUnitsAlive);
        }

        public void SetWidth(int newWidth)
        {
            Width = min(MaxRow, newWidth);
            Depth = (int)ceil(NumUnitsAlive / max(1f,Width));
        }
        
        //┌────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
        //│  ◇◇◇◇◇◇ Set Direction ◇◇◇◇◇◇                                                                               │
        //└────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
        
        public void SetDirection(float3 newDirection)
        {
            if (newDirection.IsZero()) return;
            DirectionForward = newDirection;
        }
        
        public void SetDirection(float2 newDirection)
        {
            SetDirection(float3(newDirection.x, 0, newDirection.y));
        }

        public void SetDirection(float3 firstUnitFirstRow, float3 lastUnitFirstRow)
        {
            SetDirection(cross(down(), normalizesafe(lastUnitFirstRow - firstUnitFirstRow)));
        }
        
        //┌────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
        //│  ◇◇◇◇◇◇ Overrides ◇◇◇◇◇◇                                                                                   │
        //└────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Formation(in FormationData rhs)
        {
            return new Formation(rhs);
        }
        
        public override string ToString()
        {
            return $"Current formation:\r\n" +
                   $"BaseNumUnits:{BaseNumUnits}\r\n" +
                   $"MinRow {MinRow}\r\n" +
                   $"MaxRow {MaxRow}\r\n" +
                   $"UnitSize {UnitSize}\r\n" +
                   $"SpaceBetweenUnits {SpaceBetweenUnits}\r\n" +
                   $"NumUnitsAlive {NumUnitsAlive}\r\n" +
                   $"Width {Width}\r\n" +
                   $"Depth {Depth}\r\n" +
                   $"Direction2DForward {Direction2DForward}\r\n";
        }
    }
}
