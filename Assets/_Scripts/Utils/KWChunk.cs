using System;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

using static Kaizerwald.Utilities.KzwMath;
using static Unity.Mathematics.math;

using Debug = UnityEngine.Debug;
using int2 = Unity.Mathematics.int2;
using float2 = Unity.Mathematics.float2;
using float3 = Unity.Mathematics.float3;

namespace Kaizerwald
{
    public enum ChunkEnterPoint
    {
        Left,
        Right,
        Top,
        Bottom,
    }
    
    public static class KWChunk
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetChunkIndexFromGridIndex(int gridIndex, int chunkSize, int numChunkX)
        {
            int mapSizeX = chunkSize * numChunkX;
            int2 cellCoord = GetXY2(gridIndex,mapSizeX);
            int2 chunkCoord = (int2)floor(cellCoord / chunkSize);
            return GetIndex(chunkCoord, numChunkX);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCellChunkIndexFromGridIndex(int gridIndex, int chunkSize, int numChunkX)
        {
            int mapSizeX = chunkSize * numChunkX;
            int2 cellCoord = GetXY2(gridIndex,mapSizeX);
            int2 chunkCoord = (int2)floor(cellCoord / chunkSize);
            int2 cellCoordInChunk = cellCoord - (chunkCoord * chunkSize);
            return GetIndex(cellCoordInChunk, chunkSize);
        }
        
        /// <summary>
        /// Cell is at the constant size of 1!
        /// chunk can only be a square, meaning : width = height
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 GetGridCellCoordFromChunkCellCoord(in int2 cellInChunkCoord, int chunkCellWidth, in int2 chunkCoord)
        {
            return (chunkCoord * chunkCellWidth) + cellInChunkCoord;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetGridCellIndexFromChunkCellIndex(int chunkIndex, in GridData gridData, int cellIndexInsideChunk)
        {
            int2 chunkCoord = GetXY2(chunkIndex,gridData.NumChunkXY.x);
            int2 cellCoordInChunk = GetXY2(cellIndexInsideChunk,gridData.NumCellInChunkX);
            int2 cellGridCoord = GetGridCellCoordFromChunkCellCoord(cellCoordInChunk, gridData.NumCellInChunkX, chunkCoord);
            return GetIndex(cellGridCoord, gridData.NumCellXY.x);//cellGridCoord.y * gridData.NumCellXY.x + cellGridCoord.x;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)] //May be useful if we dont want to create a gridData
        public static int GetGridCellIndexFromChunkCellIndex(int chunkIndex, int mapSizeX, int chunkSize, int cellIndexInsideChunk, int cellSize = 1)
        {
            int2 chunkCoord = GetXY2(chunkIndex,mapSizeX/chunkSize);
            int2 cellCoordInChunk = GetXY2(cellIndexInsideChunk,chunkSize);
            int2 cellGridCoord = GetGridCellCoordFromChunkCellCoord(cellCoordInChunk,chunkSize/cellSize, chunkCoord);
            return GetIndex(cellGridCoord, mapSizeX); //(cellGridCoord.y * mapSizeX) + cellGridCoord.x;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndexFromPosition(in float2 pointPos, in int2 mapXY, int cellSize = 1)
        {
            float2 percents = pointPos / (mapXY * cellSize);
            percents = clamp(percents, float2.zero, 1f);
            int2 xy =  clamp((int2)floor(mapXY * percents), 0, mapXY - 1);
            return mad(xy.y, mapXY.x/cellSize, xy.x);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndexFromPositionOffset(in float2 pointPos, in int2 mapXY, int cellSize = 1)
        {
            float2 offset = mapXY / new float2(2f);
            float2 percents = (pointPos + offset) / (mapXY * cellSize);
            percents = clamp(percents, float2.zero, 1f);
            int2 xy =  clamp((int2)floor(mapXY * percents), 0, mapXY - 1); // Cellsize not applied?!
            return mad(xy.y, mapXY.x/cellSize, xy.x);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndexFromPositionOffset(in float3 pointPos, in int2 mapXY, int cellSize = 1)
        {
            return GetIndexFromPositionOffset(pointPos.xz, mapXY, cellSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndexFromPosition(in float3 pointPos, in int2 mapXY, int cellSize = 1)
        {
            return GetIndexFromPosition(pointPos.xz, mapXY, cellSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndexFromPosition(in Vector3 pointPos, in int2 mapXY, int cellSize = 1)
        {
            return GetIndexFromPosition((float3)pointPos, mapXY, cellSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetCellCenterFromPosition(in Vector3 positionInWorld, in int2 mapXY, int cellSize = 1)
        {
            int index = GetIndexFromPosition(((float3)positionInWorld).xz, mapXY, cellSize);
            float2 cellCoord = GetXY2(index,mapXY.x/cellSize) * cellSize + new float2(cellSize/2f);
            return new Vector3(cellCoord.x,0,cellCoord.y);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetCellCenterFromIndex(int index, in int2 mapXY, int cellSize = 1)
        {
            float2 cellCoord = GetXY2(index,mapXY.x/cellSize) * cellSize + new float2(cellSize/2f);
            return new Vector3(cellCoord.x,0,cellCoord.y);
        }
    }

}
