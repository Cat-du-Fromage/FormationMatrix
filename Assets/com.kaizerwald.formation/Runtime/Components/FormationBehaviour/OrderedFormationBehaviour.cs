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

        protected SortedSet<int> InactiveElements = new SortedSet<int>();
        
        private NativeList<int> elementIndexToTransformIndex;
        //TODO a ajouter au besoin
        //private NativeList<int> transformIndexToElementIndex;
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                             ◆◆◆◆◆◆ PROPERTIES ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        public NativeList<int> ElementIndexToTransformIndex => elementIndexToTransformIndex;
        //public NativeList<int> TransformIndexToElementIndex => transformIndexToElementIndex;
        
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Events ◈◈◈◈◈◈                                                                                           ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        public override event Action<int> OnFormationResized;
        public virtual event Action<int, int> OnElementSwapped;
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                          ◆◆◆◆◆◆ CLASS METHODS ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        public override void OnUpdate()
        {
            if (InactiveElements.Count == 0) return;
            Rearrangement();
        }
        
        public override void Initialize(Formation formationReference, List<T> formationElements, float3 leaderPosition = default)
        {
            elementIndexToTransformIndex = new NativeList<int>(formationElements.Count, Persistent);
            base.Initialize(formationReference, formationElements, leaderPosition);
        }
        
        public override void SetElementInactive(T element)
        {
            InactiveElements.Add(GetIndexInFormation(element));
        }
        
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Add | Remove ◈◈◈◈◈◈                                                                                     ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        public override void Add(T element)
        {
            base.Add(element);
            elementIndexToTransformIndex.Add(ElementKeyTransformIndex[element]);
        }

        protected override bool Remove(T element)
        {
            if (!ElementKeyTransformIndex.TryGetValue(element, out int indexToRemove)) return false;
            FormationTransformAccessArray.RemoveAtSwapBack(indexToRemove);
            Transforms.RemoveAtSwapBack(indexToRemove);
            Elements.Remove(element);
            ResetTransformsIndicators();
            return true;
        }
        
        public override void Dispose()
        {
            if(elementIndexToTransformIndex.IsCreated) elementIndexToTransformIndex.Dispose();
            base.Dispose();
        }

        protected virtual void ResetTransformsIndicators()
        {
            elementIndexToTransformIndex.Clear();
            ElementKeyTransformIndex.Clear();
            foreach (T aliveElement in Elements)
            {
                int elementIndex = Transforms.IndexOf(aliveElement.transform);
                elementIndexToTransformIndex.Add(elementIndex);
                ElementKeyTransformIndex.Add(aliveElement, elementIndex);
            }
        }
        
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Rearrangement ◈◈◈◈◈◈                                                                                    ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        protected override bool RegisterInactiveElements(out int numDead)
        {
            numDead = InactiveElements.Count;
            if (numDead == 0) return false;
            foreach (int indexInRegiment in InactiveElements)
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
            OnElementSwapped?.Invoke(lhs, rhs);
        }
        
        private void Rearrange()
        {
            for (int i = 0; i < Elements.Count; i++)
            {
                if (InactiveElements.Count == 0) break;
                SwapRearrange();
            }
            InactiveElements.Clear();
            return;
            //┌▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁┐
            //▕  ◇◇◇◇◇◇ Internal Methods ◇◇◇◇◇◇                                                                        ▏
            //└▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔┘
            void SwapRearrange() // must be in a separate function because of "InactiveElements.Min" issue when remove in a loop
            {
                int deadIndex = InactiveElements.Min;
                if (!RearrangementUtils.TryGetIndexAround(deadIndex, Elements, Formation, out int swapIndex))
                {
                    InactiveElements.Remove(deadIndex);
                    return;
                }
                SwapElementByIndex(deadIndex, swapIndex);
                InactiveElements.Remove(deadIndex);
                InactiveElements.Add(swapIndex);
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
            Formation.Remove(cacheNumDead);
            OnFormationResized?.Invoke(Formation.NumUnitsAlive);
        }
    }
}
