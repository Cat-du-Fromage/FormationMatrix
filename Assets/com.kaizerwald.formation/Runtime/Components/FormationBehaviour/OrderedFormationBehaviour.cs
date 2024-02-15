using System.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

using static Unity.Collections.Allocator;
using static Unity.Collections.NativeArrayOptions;

namespace Kaizerwald.FormationModule
{
    public abstract class OrderedFormationBehaviour<T> : BaseFormationBehaviour<T>
    where T : Component, IFormationElement
    {
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                               ◆◆◆◆◆◆ FIELD ◆◆◆◆◆◆                                                  ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        protected SortedSet<int> SortedInactiveElements = new SortedSet<int>();
        
        private NativeList<int> elementIndexToTransformIndex;
        private NativeList<int> transformIndexToElementIndex;
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                             ◆◆◆◆◆◆ PROPERTIES ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        public NativeList<int> ElementIndexToTransformIndex => elementIndexToTransformIndex;
        public NativeList<int> TransformIndexToElementIndex => transformIndexToElementIndex;

        public override int GetIndexInFormation(T element)
        {
            if (!ElementKeyTransformIndex.TryGetValue(element, out int transformIndex)) return -1;
            return transformIndexToElementIndex[transformIndex];
        }

    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Events ◈◈◈◈◈◈                                                                                           ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        public override event Action<int> OnFormationResized;
        public virtual event Action<int, int> OnElementSwapped;
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                          ◆◆◆◆◆◆ CLASS METHODS ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        public override void UpdateFormation()
        {
            if (SortedInactiveElements.Count == 0) return;
            Rearrangement();
        }
        
        public override void InitializeFormation(Formation formationReference, List<T> formationElements, float3 leaderPosition = default)
        {
            elementIndexToTransformIndex = new NativeList<int>(formationElements.Count, Persistent);
            transformIndexToElementIndex = new NativeList<int>(formationElements.Count, Persistent);
            base.InitializeFormation(formationReference, formationElements, leaderPosition);
        }

        public override void RegisterInactiveElement(T element)
        {
            base.RegisterInactiveElement(element);
            SortedInactiveElements.Add(GetIndexInFormation(element));
        }
        
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Add | Remove ◈◈◈◈◈◈                                                                                     ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        public override void Add(T element)
        {
            base.Add(element);
            elementIndexToTransformIndex.Add(ElementKeyTransformIndex[element]);
            transformIndexToElementIndex.Add(Elements.Count - 1);
        }

        protected override void InternalRemove(T element, int transformIndexToRemove)
        {
            base.InternalRemove(element, transformIndexToRemove);
            SortedInactiveElements.Add(transformIndexToElementIndex[transformIndexToRemove]);
            UpdateFormation();
        }
        
        public override void Dispose()
        {
            if(elementIndexToTransformIndex.IsCreated) elementIndexToTransformIndex.Dispose();
            if(transformIndexToElementIndex.IsCreated) transformIndexToElementIndex.Dispose();
            base.Dispose();
        }

        protected virtual void ResetTransformsIndicators()
        {
            ElementKeyTransformIndex.Clear();
            elementIndexToTransformIndex.Clear();
            transformIndexToElementIndex.Clear();
            NativeArray<int> tmpTransformIndexToElementIndex = new (Elements.Count, Temp, UninitializedMemory);
            for (int i = 0; i < Elements.Count; i++)
            {
                T aliveElement = Elements[i];
                int elementIndex = Transforms.IndexOf(aliveElement.transform);
                ElementKeyTransformIndex.Add(aliveElement, elementIndex);
                elementIndexToTransformIndex.Add(elementIndex);
                tmpTransformIndexToElementIndex[elementIndex] = i;
            }
            transformIndexToElementIndex.CopyFrom(tmpTransformIndexToElementIndex);
        }
        
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Rearrangement ◈◈◈◈◈◈                                                                                    ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        protected override bool RegisterInactiveElements(out int numDead)
        {
            numDead = SortedInactiveElements.Count;
            if (numDead == 0) return false;
            foreach (int indexInRegiment in SortedInactiveElements)
            {
                Elements[indexInRegiment].BeforeRemoval();
            }
            return true;
        }
        
        protected virtual void SwapElementByIndex(int lhs, int rhs)
        {
            //IndexToRealTransformIndex.Swap(lhs, rhs);
            //Elements.Swap(lhs, rhs);
            //Since it's reset at the end is it really useful?
            (elementIndexToTransformIndex[lhs], elementIndexToTransformIndex[rhs]) = (elementIndexToTransformIndex[rhs], elementIndexToTransformIndex[lhs]);
            (Elements[lhs], Elements[rhs]) = (Elements[rhs], Elements[lhs]);
            Elements[lhs].OnRearrangement(lhs);
            Elements[rhs].OnRearrangement(rhs);
            HandleElementSwapped(lhs, rhs);
        }
        
        protected void Rearrange()
        {
            for (int i = 0; i < Elements.Count; i++)
            {
                if (SortedInactiveElements.Count == 0) break;
                SwapRearrange();
            }
            SortedInactiveElements.Clear();
            return;
            //┌▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁┐
            //▕  ◇◇◇◇◇◇ Internal Methods ◇◇◇◇◇◇                                                                        ▏
            //└▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔┘
            void SwapRearrange() // must be in a separate function because of "InactiveElements.Min" issue when remove in a loop
            {
                int deadIndex = SortedInactiveElements.Min;
                if (!RearrangementUtils.TryGetIndexAround(deadIndex, Elements, CurrentFormation, out int swapIndex))
                {
                    SortedInactiveElements.Remove(deadIndex);
                    return;
                }
                SwapElementByIndex(deadIndex, swapIndex);
                SortedInactiveElements.Remove(deadIndex);
                SortedInactiveElements.Add(swapIndex);
            }
        }
        
        protected void RemoveInactiveElements(int numDead)
        {
            NativeArray<int> trashItems = new (numDead, Temp, UninitializedMemory);
            for (int i = 0; i < numDead; i++)
            {
                T elementToRemove = Elements[^1];
                trashItems[i] = ElementKeyTransformIndex[elementToRemove];
                Elements.RemoveAt(Elements.Count-1);
                elementToRemove.AfterRemoval();
            }
            trashItems.Sort();
            for (int i = numDead - 1; i > -1; i--) // MUST BE DONE IN REVERSE! because of the nature of RemoveAtSwapBack
            {
                int index = trashItems[i];
                FormationTransformAccessArray.RemoveAtSwapBack(index);
                Transforms.RemoveAtSwapBack(index);
            }
            ResetTransformsIndicators();
        }

        protected override void Rearrangement()
        {
            if (!RegisterInactiveElements(out int cacheNumDead)) return;
            TargetFormation.Remove(cacheNumDead); //was needed before rearrange, make more sense to let it here anyway
            Rearrange();
            if (cacheNumDead >= Elements.Count)
            {
                Clear();
            }
            else
            {
                RemoveInactiveElements(cacheNumDead);
            }
            CurrentFormation.Remove(cacheNumDead);
            HandleFormationResized(CurrentFormation.NumUnitsAlive);
            //OnFormationResized?.Invoke(CurrentFormation.NumUnitsAlive);
        }

        protected virtual void HandleElementSwapped(int lhs, int rhs)
        {
            OnElementSwapped?.Invoke(lhs, rhs);
        }
    }
}
