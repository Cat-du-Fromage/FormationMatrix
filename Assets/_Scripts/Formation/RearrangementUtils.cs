using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

using static Unity.Mathematics.math;

namespace Kaizerwald.FormationModule
{
    //TODO: need a way to burst this: replace List<T> by nativelist<TData>
    //TData will be a conversion of IFormationElement into a struct containing data of the unit
    public static class RearrangementUtils
    {
        public static int GetIndexAround<T>(int index, FormationMatrix<T> formationMatrix) 
            where T : Component, IFormationElement
        {
            return GetIndexAround(index, formationMatrix.Elements, formationMatrix.Formation);
        }
        
        public static int GetIndexAround<T>(int index, List<T> units, in FormationData formation) 
            where T : Component, IFormationElement
        {
            return GetIndexAround(index, units, formation.Depth, formation.Width, formation.NumUnitsLastLine);
        }
        
        public static int GetIndexAround<T>(int index, List<T> units, int depth, int width, int widthLastLine) 
            where T : Component, IFormationElement
        {
            int indexUnit = -1;
            int lastLineIndex = depth - 1;
            
            int yInRegiment = index / width;
            int xInRegiment = index - (yInRegiment * width);
            
            //Inline if there is only ONE line
            bool nextRowValid = IsNextRowValid(units, yInRegiment, width, depth, widthLastLine);
            if (depth == 1 || yInRegiment == lastLineIndex || !nextRowValid)
            {
                return RearrangeInline(units, index);
            }
            
            for (int lineIndex = yInRegiment + 1; lineIndex < depth; lineIndex++) //Tester avec ++lineIndex (enlever le +1 à yRegiment)
            {
                int lineWidth = lineIndex == lastLineIndex ? widthLastLine : width;
                int lastXCoordCurrentLine = lineWidth - 1;
                
                //ATTENTION: LA DERNIERE LIGNE SI Composé uniquement d'entité null ne sera pas considéré comme VIDE DONC JAMAIS INLINE!
                indexUnit = GetIndexBehind(units.Count, xInRegiment, yInRegiment, width);
                if (indexUnit != -1 && IsUnitValid(units[indexUnit])) return indexUnit;
                
                bool2 leftRightClose = new (xInRegiment == 0, xInRegiment == lastXCoordCurrentLine);//-1 because we want the index
                int x = min(xInRegiment, lastXCoordCurrentLine);
                for (int i = 0; i <= lineWidth; i++) // 0 because we check unit right behind
                {
                    if (IsRightValid(i)) return indexUnit;
                    if (IsLeftValid(i)) return indexUnit;
                    if (all(leftRightClose)) break;
                }

                continue;
                //┌────────────────────────────────────────────────────────────────────────────────────────────────────┐
                //│  ◇◇◇◇◇◇ Internal Methods ◇◇◇◇◇◇                                                                    │
                //└────────────────────────────────────────────────────────────────────────────────────────────────────┘
                bool IsRightValid(int i)
                {
                    if (leftRightClose.x) return false;
                    indexUnit = mad(lineIndex, width, x-i);
                    leftRightClose.x = x - i == 0;
                    return IsUnitValid(units[indexUnit]);
                }

                bool IsLeftValid(int i)
                {
                    if (leftRightClose.y) return false;
                    indexUnit = mad(lineIndex, width, x+i);
                    leftRightClose.y = x + i >= lastXCoordCurrentLine;
                    return IsUnitValid(units[indexUnit]);
                }
            }
            return -1;
        }
        
        
        
        private static bool IsUnitValid<T>(T unit) 
            where T : Component, IFormationElement
        {
            return !unit.IsDead;
        }

        private static int GetIndexBehind(int unitsCount, int xInRegiment, int yInRegiment,int width)
        {
            int minIndex = width * (yInRegiment + 1);
            int maxIndex = minIndex + width - 1;
            int indexUnitBehind = clamp(mad(yInRegiment + 1, width, xInRegiment), minIndex, maxIndex);
            return indexUnitBehind < unitsCount ? indexUnitBehind : -1;
        }

        public static int RearrangeInline<T>(List<T> units, int index) 
            where T : Component, IFormationElement
        {
            int numElements = units.Count;
            if (index == numElements - 1) return -1;
            int maxIteration = numElements - index;
            for (int i = 1; i < maxIteration; i++) //Begin at 1, so we start at the next index
            {
                int indexToCheck = index + i;
                if (IsUnitValid(units[indexToCheck])) return indexToCheck;
            }
            return -1;
        }

        private static bool IsNextRowValid<T>(List<T> units, int yLine, int width, int depth, int widthLastLine)
            where T : Component, IFormationElement
        {
            (int nextYLineIndex, int lastLineIndex) = (yLine + 1, depth - 1);
            if (nextYLineIndex > lastLineIndex) return false;
            int numUnitOnLine = nextYLineIndex == lastLineIndex ? widthLastLine : width;
            int startIndex = nextYLineIndex * width;
            int lastIndex = min(units.Count, startIndex + numUnitOnLine);
            for (int i = startIndex; i < lastIndex; i++)
            {
                if (IsUnitValid(units[i])) return true;
            }
            return false;
        }
    }
}
