using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static UnityEngine.Vector2;
using static Unity.Mathematics.math;

using static Unity.Jobs.LowLevel.Unsafe.JobsUtility;
using static Unity.Collections.Allocator;
using static Unity.Collections.NativeArrayOptions;

namespace Kaizerwald.FormationModule
{
    public static class FormationExtension
    {
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                             ◆◆◆◆◆◆ COMPARISON ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool EqualComposition(int rightNumUnitsAlive, int rightWidth, int leftNumUnitsAlive, int leftWidth)
        {
            return rightNumUnitsAlive == leftNumUnitsAlive && rightWidth == leftWidth;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualComposition(this in FormationData rhs, in Formation lhs)
        {
            return EqualComposition(rhs.NumUnitsAlive,rhs.Width, lhs.NumUnitsAlive, lhs.Width);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualComposition(this in FormationData rhs, in FormationData lhs)
        {
            return EqualComposition(rhs.NumUnitsAlive,rhs.Width, lhs.NumUnitsAlive, lhs.Width);
        }
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                 ◆◆◆◆◆◆ UNIT RELATIVE POSITION TO REGIMENT ◆◆◆◆◆◆                                   ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ 3D Position ◈◈◈◈◈◈                                                                                      ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetUnitRelativePositionToRegiment3D(this in FormationData formation, int unitIndex, Vector3 regimentPosition)
        {
            Vector2 dstUnitToUnit = formation.DistanceUnitToUnit;
            
            int y = unitIndex / formation.Width;
            int x = unitIndex - y * formation.Width;
            int widthRow = y == formation.Depth - 1 ? formation.NumUnitsLastLine : formation.Width;
            
            Vector2 regimentBackDirection = -(float2)formation.Direction2DForward;
            Vector2 yOffset = y * dstUnitToUnit.y * regimentBackDirection;
            
            //Attention! si Width Pair: 
            int midWidth = widthRow / 2;
            bool pair = (widthRow & 1) == 0;
            
            Vector2 xLeftDirection = -Perpendicular(regimentBackDirection);
            Vector2 xBaseOffset = (pair ? dstUnitToUnit.x / 2f : 0) * xLeftDirection;
            xBaseOffset += (pair ? midWidth - 1 : midWidth) * dstUnitToUnit.x * xLeftDirection; //space MidRow -> first Unit Left
            Vector2 xOffset = xBaseOffset + x * dstUnitToUnit.x * Perpendicular(regimentBackDirection);
            Vector2 offset = new Vector2(regimentPosition.x, regimentPosition.z) + yOffset + xOffset;
            return new Vector3(offset.x, regimentPosition.y, offset.y);
        }
        
        //┌────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
        //│  ◇◇◇◇◇◇ Single Unit ◇◇◇◇◇◇                                                                                 │
        //└────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetUnitRelativePositionToRegiment3D(this Formation formation, int unitIndex, Vector3 regimentPosition)
        {
            return ((FormationData)formation).GetUnitRelativePositionToRegiment3D(unitIndex, regimentPosition);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 GetUnitRelativePositionToRegiment3D(this in FormationData formation, int unitIndex, float3 regimentPosition)
        {
            return formation.GetUnitRelativePositionToRegiment3D(unitIndex, (Vector3)regimentPosition);
        }
        
        //┌────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
        //│  ◇◇◇◇◇◇ Array | NativeArray ◇◇◇◇◇◇                                                                         │
        //└────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3[] GetUnitsPositionRelativeToRegiment(this FormationData formation, Vector3 regimentPosition)
        {
            Vector3[] positions = new Vector3[formation.NumUnitsAlive];
            for (int i = 0; i < formation.NumUnitsAlive; i++) 
            {
                positions[i] = formation.GetUnitRelativePositionToRegiment3D(i, regimentPosition);
            }
            return positions;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3[] GetUnitsPositionRelativeToRegiment(this FormationData formation, float3 regimentPosition)
        {
            float3[] positions = new float3[formation.NumUnitsAlive];
            for (int i = 0; i < formation.NumUnitsAlive; i++)
            {
                positions[i] = formation.GetUnitRelativePositionToRegiment3D(i, regimentPosition);
            }
            return positions;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeArray<float3> GetUnitsPositionRelativeToRegiment(this FormationData formation, float3 regimentPosition, Allocator allocator)
        {
            NativeArray<float3> positions = new(formation.NumUnitsAlive, allocator, UninitializedMemory);
            for (int i = 0; i < formation.NumUnitsAlive; i++)
            {
                positions[i] = formation.GetUnitRelativePositionToRegiment3D(i, regimentPosition);
            }
            return positions;
        }
        
        //┌────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
        //│  ◇◇◇◇◇◇ Jobified ◇◇◇◇◇◇                                                                                    │
        //└────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeArray<float3> GetPositionsInFormationByRaycast(this FormationData formation, float3 regimentPosition, LayerMask terrainLayerMask)
        {
            return JPositionsInFormationWithRaycast.ProcessPositionComplete(formation, regimentPosition, terrainLayerMask);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeArray<float3> GetPositionsInFormationByRaycast(this Formation formation, float3 regimentPosition, LayerMask terrainLayerMask)
        {
            return JPositionsInFormationWithRaycast.ProcessPositionComplete(formation, regimentPosition, terrainLayerMask);
        }
        
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ 2D Position ◈◈◈◈◈◈                                                                                      ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 GetUnitRelativePositionToRegiment(this in FormationData formation, int unitIndex, Vector2 regimentPosition)
        {
            Vector2 dstUnitToUnit = formation.DistanceUnitToUnit;
            
            int y = unitIndex / formation.Width;
            int x = unitIndex - y * formation.Width;
            int widthRow = y == formation.Depth - 1 ? formation.NumUnitsLastLine : formation.Width;
            
            Vector2 regimentBackDirection = -(float2)formation.Direction2DForward;
            Vector2 yOffset = y * dstUnitToUnit.y * regimentBackDirection;
            
            //Attention! si Width Pair: 
            int midWidth = widthRow / 2;
            bool pair = (widthRow & 1) == 0;

            Vector2 xLeftDirection = -Perpendicular(regimentBackDirection);
            Vector2 xBaseOffset = (pair ? dstUnitToUnit.x / 2f : 0) * xLeftDirection;
            xBaseOffset += (pair ? midWidth - 1 : midWidth) * dstUnitToUnit.x * xLeftDirection; //space MidRow -> first Unit Left
            Vector2 xOffset = xBaseOffset + x * dstUnitToUnit.x * Perpendicular(regimentBackDirection);
            return regimentPosition + yOffset + xOffset;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 GetUnitRelativePositionToRegiment(this in FormationData formation, int unitIndex, float2 regimentPosition)
        {
            float2 dstUnitToUnit = formation.DistanceUnitToUnit;
            int y = unitIndex / formation.Width;
            int x = unitIndex - y * formation.Width;
            int widthRow = select(formation.Width, formation.NumUnitsLastLine, y == formation.Depth - 1);
            //YOffset
            //On prend simplement la direction "back" du régiment à laquelle on ajoute l'espace Y d'une unité
            float2 regimentBackDirection = formation.Direction2DBack;
            //uncomment if you want leader to be in front of regiment instead of in the middle of the first row
            float2 yOffset = y * dstUnitToUnit.y * regimentBackDirection;
            
            //XOffset
            int midWidth = widthRow / 2;
            bool pair = (widthRow & 1) == 0;
            //on cherche a atteindre l'unité (0,Y) de la ligne => il nous faut la direction gauche
            float2 xLeftDirection = float2(regimentBackDirection.y, -regimentBackDirection.x); //float2 xLeftDirection = regimentBackDirection.CrossRight();
            //avec la direction on saute le nombre nécessaire d'espace, Attention si PAIR! premier saut/2!!!
            float2 xBaseOffset = select(0, dstUnitToUnit.x / 2f, pair) * xLeftDirection;
            //restant des sauts(moitié de rangé car on commence au centre) Attention si PAIR! réduire de 1!
            xBaseOffset += select(midWidth, midWidth-1, pair) * dstUnitToUnit.x * xLeftDirection;

            //Arrivé à unité (0,Y): direction inverse * la coord X de l'unité cherchée
            float2 xOffset = xBaseOffset + x * dstUnitToUnit.x * float2(-regimentBackDirection.y, regimentBackDirection.x);
            return regimentPosition + yOffset + xOffset;
        }
        
        //┌────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
        //│  ◇◇◇◇◇◇ Single Unit ◇◇◇◇◇◇                                                                                 │
        //└────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 GetUnitRelativePositionToRegiment(this in FormationData formation, int unitIndex, float3 regimentPosition)
        {
            return formation.GetUnitRelativePositionToRegiment(unitIndex, regimentPosition.xz);
        }
        
        //┌────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
        //│  ◇◇◇◇◇◇ Array | NativeArray ◇◇◇◇◇◇                                                                         │
        //└────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2[] GetUnitsPositionRelativeToRegiment(this FormationData formation, Vector2 regimentPosition)
        {
            Vector2[] positions = new Vector2[formation.NumUnitsAlive];
            for (int i = 0; i < formation.NumUnitsAlive; i++) 
            {
                positions[i] = formation.GetUnitRelativePositionToRegiment(i, regimentPosition);
            }
            return positions;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeArray<float2> GetUnitsPositionRelativeToRegiment(this FormationData formation, float2 regimentPosition, Allocator allocator)
        {
            NativeArray<float2> positions = new(formation.NumUnitsAlive, allocator, UninitializedMemory);
            for (int i = 0; i < formation.NumUnitsAlive; i++)
            {
                positions[i] = formation.GetUnitRelativePositionToRegiment(i, regimentPosition);
            }
            return positions;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2[] GetUnitsPositionRelativeToRegiment(this FormationData formation, float2 regimentPosition)
        {
            float2[] positions = new float2[formation.NumUnitsAlive];
            for (int i = 0; i < formation.NumUnitsAlive; i++)
            {
                positions[i] = formation.GetUnitRelativePositionToRegiment(i, regimentPosition);
            }
            return positions;
        }
        
        //┌────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
        //│  ◇◇◇◇◇◇ Jobified ◇◇◇◇◇◇                                                                                    │
        //└────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JobHandle GetPositionsInFormationByJob(this FormationData formation, float3 regimentPosition, out NativeArray<float2> elementsPosition, JobHandle dependency = default)
        {
            elementsPosition = new (formation.NumUnitsAlive, TempJob, UninitializedMemory);
            return JPositionsInFormation.Process(elementsPosition, formation, regimentPosition.xz, dependency);
        }
    }
    
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                                 ◆◆◆◆◆◆ JOBS ◆◆◆◆◆◆                                                 ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝
    [BurstCompile]
    public struct JPositionsInFormation : IJobFor
    {
        [ReadOnly] public FormationData Formation;
        [ReadOnly] public float2 RegimentPosition;

        [WriteOnly, NativeDisableParallelForRestriction, NativeDisableContainerSafetyRestriction]
        public NativeArray<float2> UnitsPosition;
        
        public void Execute(int unitIndex)
        {
            float2 dstUnitToUnit = Formation.DistanceUnitToUnit;
            int y = unitIndex / Formation.Width;
            int x = unitIndex - y * Formation.Width;
            int widthRow = select(Formation.Width, Formation.NumUnitsLastLine, y == Formation.Depth - 1);
            
            float2 regimentBackDirection = Formation.Direction2DBack;
            float2 yOffset = y * dstUnitToUnit.y * regimentBackDirection;
            int midWidth = widthRow / 2;
            bool pair = (widthRow & 1) == 0;
            float2 xLeftDirection = float2(regimentBackDirection.y, -regimentBackDirection.x);
            float2 xBaseOffset = select(0, dstUnitToUnit.x / 2f, pair) * xLeftDirection;
            xBaseOffset += select(midWidth, midWidth-1, pair) * dstUnitToUnit.x * xLeftDirection;
            float2 xOffset = xBaseOffset + x * dstUnitToUnit.x * float2(-regimentBackDirection.y, regimentBackDirection.x);
            UnitsPosition[unitIndex] = RegimentPosition + yOffset + xOffset;
        }

        public static JobHandle Process(NativeArray<float2> unitsPosition, FormationData formation, float2 regimentPosition, JobHandle dependency = default)
        {
            JPositionsInFormation job = new JPositionsInFormation
            {
                Formation = formation,
                RegimentPosition = regimentPosition,
                UnitsPosition = unitsPosition
            };
            JobHandle jobHandle = job.ScheduleParallel(unitsPosition.Length, JobWorkerCount - 1, dependency);
            return jobHandle;
        }
    }
    
    [BurstCompile]
    public struct JPositionsInFormationWithRaycast : IJobFor
    {
        [ReadOnly] public FormationData Formation;
        [ReadOnly] public float2 RegimentPosition;
        
        [ReadOnly] public int OriginHeight;
        [ReadOnly] public int RayDistance;
        [ReadOnly] public QueryParameters QueryParams;
        
        [WriteOnly, NativeDisableParallelForRestriction, NativeDisableContainerSafetyRestriction]
        public NativeArray<RaycastCommand> Commands;

        public void Execute(int unitIndex)
        {
            //float2 origin = Origins[unitIndex];
            float2 dstUnitToUnit = Formation.DistanceUnitToUnit;
            int y = unitIndex / Formation.Width;
            int x = unitIndex - y * Formation.Width;
            int widthRow = select(Formation.Width, Formation.NumUnitsLastLine, y == Formation.Depth - 1);
            
            float2 regimentBackDirection = Formation.Direction2DBack;
            float2 yOffset = y * dstUnitToUnit.y * regimentBackDirection;
            
            int midWidth = widthRow / 2;
            bool pair = (widthRow & 1) == 0;
            
            float2 xLeftDirection = float2(regimentBackDirection.y, -regimentBackDirection.x);
            float2 xBaseOffset = select(0, dstUnitToUnit.x / 2f, pair) * xLeftDirection;
            xBaseOffset += select(midWidth, midWidth-1, pair) * dstUnitToUnit.x * xLeftDirection;
            float2 xOffset = xBaseOffset + x * dstUnitToUnit.x * float2(-regimentBackDirection.y, regimentBackDirection.x);
            //UnitsPosition[unitIndex] = RegimentPosition + yOffset + xOffset;
            float2 origin = RegimentPosition + yOffset + xOffset;
            
            Vector3 origin3D = new (origin.x, OriginHeight, origin.y);
            Commands[unitIndex] = new RaycastCommand(origin3D, Vector3.down, QueryParams, RayDistance);
        }

        public static JobHandle ProcessRaycast(NativeArray<RaycastHit> results, FormationData formation, float3 regimentPosition, LayerMask terrainLayer, JobHandle dependency = default)
        {
            NativeArray<RaycastCommand> commands = new (formation.NumUnitsAlive, TempJob, UninitializedMemory);
            JPositionsInFormationWithRaycast job = new JPositionsInFormationWithRaycast
            {
                Formation = formation,
                RegimentPosition = regimentPosition.xz,
                OriginHeight = 8 + (int)regimentPosition.y,
                RayDistance = 16,
                QueryParams = new QueryParameters(terrainLayer.value),
                Commands = commands
            };
            JobHandle jobHandle = job.ScheduleParallel(formation.NumUnitsAlive, JobWorkerCount - 1, dependency);
            JobHandle commandJh = RaycastCommand.ScheduleBatch(commands, results, 1, 1, jobHandle);
            commands.Dispose(commandJh);
            return commandJh;
        }
        
        public static JobHandle ProcessPosition(NativeArray<float3> positions, FormationData formation, float3 regimentPosition, LayerMask terrainLayer, JobHandle dependency = default)
        {
            NativeArray<RaycastHit> results = new (formation.NumUnitsAlive, TempJob, UninitializedMemory);
            JobHandle raycastJobHandle = ProcessRaycast(results, formation, regimentPosition, terrainLayer, dependency);
            JobHandle conversionJobHandle = JConvertRaycastHitToPosition.Process(positions, results, raycastJobHandle);
            results.Dispose(conversionJobHandle);
            return conversionJobHandle;
        }
        
        public static NativeArray<float3> ProcessPositionComplete(FormationData formation, float3 regimentPosition, LayerMask terrainLayer, JobHandle dependency = default)
        {
            NativeArray<float3> positions = new (formation.NumUnitsAlive, TempJob, UninitializedMemory);
            JobHandle jobHandle = ProcessPosition(positions, formation, regimentPosition, terrainLayer, dependency);
            jobHandle.Complete();
            return positions;
        }
    }

    [BurstCompile]
    internal struct JConvertRaycastHitToPosition : IJobFor
    {
        [ReadOnly, NativeDisableParallelForRestriction, NativeDisableContainerSafetyRestriction] 
        public NativeArray<RaycastHit> RaycastHits;
        [WriteOnly, NativeDisableParallelForRestriction, NativeDisableContainerSafetyRestriction]
        public NativeArray<float3> Positions;
        
        public void Execute(int index)
        {
            Positions[index] = RaycastHits[index].point;
        }

        public static JobHandle Process(NativeArray<float3> positions ,NativeArray<RaycastHit> results, JobHandle dependency = default)
        {
            JConvertRaycastHitToPosition job = new (){ RaycastHits = results, Positions = positions };
            JobHandle jobHandle = job.ScheduleParallel(results.Length, JobWorkerCount - 1, dependency);
            return jobHandle;
        }
    }
}