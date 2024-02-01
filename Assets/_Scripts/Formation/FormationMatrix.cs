using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using Object = System.Object;

using static Unity.Collections.Allocator;
using static Unity.Collections.NativeArrayOptions;

namespace Kaizerwald.FormationModule
{
    //Todo : Trouver un moyen de synchroniser les valeurs en multijoueur
    public class FormationMatrix<T>
    where T : Component, IFormationElement
    {
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                               ◆◆◆◆◆◆ FIELD ◆◆◆◆◆◆                                                  ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝
        private SortedSet<int> deadElements = new SortedSet<int>();
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                             ◆◆◆◆◆◆ PROPERTIES ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝
        public Formation Formation { get; private set; }
        public Formation TargetFormation { get; private set; }
        
        // Ordered by "index in formation"
        public List<T> Elements{ get; private set; } 
        
        public TransformAccessArray FormationTransformAccessArray { get; private set; }
        private List<Transform> transforms;
        
        public NativeList<int> IndexToRealTransformIndex { get; private set; }
        
        // Get Real index(TransformAccessArray) from Element
        public Dictionary<T, int> ElementKeyTransformIndex { get; private set; } 
        
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Accessors ◈◈◈◈◈◈                                                                                        ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        
        public T this[int index] => Elements[index];

        public event Action<int, int> OnSwapEvent;

        public event Action OnFormationEmpty;
        public event Action<int> OnFormationResized;
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                            ◆◆◆◆◆◆ CONSTRUCTOR ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        public FormationMatrix(Formation formationReference, List<T> formationElements)
        {
            Formation = formationReference;
            TargetFormation = new Formation(formationReference);
            Elements = formationElements;
            transforms = formationElements.Select(unit => unit.transform).ToList();
            FormationTransformAccessArray = new TransformAccessArray(transforms.ToArray());
            
            IndexToRealTransformIndex = new NativeList<int>(formationElements.Count, Persistent);
            ElementKeyTransformIndex = new Dictionary<T, int>(formationElements.Count);
            for (int i = 0; i < formationElements.Count; i++)
            {
                Add(i, formationElements[i]);
            }
        }
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                          ◆◆◆◆◆◆ CLASS METHODS ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        public void Update()
        {
            if (deadElements.Count <= 0) return;
            Rearrangement();
        }

        private int ToFormationIndex(T element) => Elements.IndexOf(element);
        
        public void Add(int index, T element)
        {
            IndexToRealTransformIndex.Add(index);
            ElementKeyTransformIndex.Add(element, index);
        }
        
        public bool Remove(T element)
        {
            bool elementExist = ElementKeyTransformIndex.TryGetValue(element, out int indexToRemove);
            Debug.Log($"Remove Element ({elementExist}) : {indexToRemove}");
            if (!elementExist) return false;
            FormationTransformAccessArray.RemoveAtSwapBack(indexToRemove);
            transforms.RemoveAtSwapBack(indexToRemove);
            //ISSUE HERE!!!!!!
            Elements.Remove(element);
            
            ResetTransformsIndicators();
            return true;
        }
        
        //Internal Use
        private void RemoveAtBack(int numToRemove)
        {
            CleanDeadElements(numToRemove);
            ResetTransformsIndicators();
        }

        private void ResetTransformsIndicators()
        {
            IndexToRealTransformIndex.Clear();
            ElementKeyTransformIndex.Clear();
            foreach (T aliveElement in Elements)
            {
                int indexElement = transforms.IndexOf(aliveElement.transform);
                IndexToRealTransformIndex.Add(indexElement);
                ElementKeyTransformIndex.Add(aliveElement, indexElement);
            }
        }
        
        public void Clear()
        {
            transforms.Clear();
            Elements.Clear();
            ElementKeyTransformIndex.Clear();
        }
        
        public void Dispose()
        {
            if(IndexToRealTransformIndex.IsCreated) IndexToRealTransformIndex.Dispose();
            if(FormationTransformAccessArray.isCreated) FormationTransformAccessArray.Dispose();
        }
        
        //┌────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
        //│  ◇◇◇◇◇◇ Rearrangement ◇◇◇◇◇◇                                                                               │
        //└────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
        private void Rearrangement()
        {
            if (!RegisterDeaths(out int cacheNumDead)) return;
            TargetFormation.Remove(cacheNumDead);
            Rearrange();
            if (cacheNumDead >= Elements.Count)
            {
                Clear();
            }
            else
            {
                RemoveAtBack(cacheNumDead);
            }
            ResizeFormation(cacheNumDead);
        }

        private void CleanDeadElements(int numDead)
        {
            NativeArray<int> trashItems = new (numDead, Temp, UninitializedMemory);
            for (int i = 0; i < numDead; i++)
            {
                T elementToRemove = Elements[^1];
                trashItems[i] = ElementKeyTransformIndex[elementToRemove];
                elementToRemove.AfterRemoval();
                Elements.RemoveAt(Elements.Count-1);
            }
            trashItems.Sort();
            for (int i = numDead - 1; i > -1; i--)
            {
                int index = trashItems[i];
                FormationTransformAccessArray.RemoveAtSwapBack(index);
                transforms.RemoveAtSwapBack(index);
            }
        }
        
        public void OnUnitKilled(T element)
        {
            deadElements.Add(ToFormationIndex(element));
        }
        
        //REAL index already used from here!!
        private bool RegisterDeaths(out int numDead)
        {
            numDead = deadElements.Count;
            if (numDead == 0) return false;
            foreach (int indexInRegiment in deadElements)
            {
                Elements[indexInRegiment].BeforeRemoval();
            }
            return true;
        }
        
        private void ResizeFormation(int numToRemove)
        {
            Formation.Remove(numToRemove);
            if (Formation.NumUnitsAlive == 0)
            {
                OnFormationEmpty?.Invoke();
            }
            else
            {
                OnFormationResized?.Invoke(Formation.NumUnitsAlive);
            }
        }
        
        private void SwapByIndex(int lhs, int rhs)
        {
            IndexToRealTransformIndex.Swap(lhs, rhs);
            Elements.Swap(lhs, rhs);
            
            //Order will be send from here!
            Elements[lhs].OnRearrangement(lhs);
            Elements[rhs].OnRearrangement(rhs);
            OnSwapEvent?.Invoke(lhs, rhs);
        }
        
        private void Rearrange()
        {
            //CAREFULL IF IN MOVEMENT
            for (int i = 0; i < Elements.Count; i++)
            {
                if (deadElements.Count == 0) break;
                SwapRearrange();
            }
            if(deadElements.Count > 0) deadElements.Clear();
        }
        
        private void SwapRearrange()
        {
            int deadIndex = deadElements.Min;
            int swapIndex = RearrangementUtils.GetIndexAround(deadIndex, this);
            if (swapIndex == -1)
            {
                deadElements.Remove(deadIndex);
                return;
            }
            SwapByIndex(deadIndex, swapIndex);
            SwapDead(deadIndex, swapIndex);
        }
        
        private void SwapDead(int deadIndex, int swapIndex)
        {
            deadElements.Remove(deadIndex);
            deadElements.Add(swapIndex);
        }
    }
}

/*
int safeGuard = Elements.Count;
int safeIndex = 0;
while (DeadElements.Count > 0 && safeIndex <= safeGuard)
{
    SwapRearrange(futureFormation);
    safeIndex++;
}
*/
