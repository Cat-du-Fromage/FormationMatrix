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
        public float3 LeaderTargetPosition { get; private set; }
        public Formation Formation { get; private set; }
        public Formation TargetFormation { get; private set; }
        
        // Ordered by "index in formation"
        public List<T> Elements{ get; private set; } 
        
        public TransformAccessArray FormationTransformAccessArray { get; private set; }
        public List<Transform> Transforms { get; private set; }
        
        //May need the inverse too Taa index => Element corresponding index
        public NativeList<int> IndexToRealTransformIndex { get; private set; }
        
        // Get Real index(TransformAccessArray) from Element
        public Dictionary<T, int> ElementKeyTransformIndex { get; private set; } 
        
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Accessors ◈◈◈◈◈◈                                                                                        ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        //┌────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
        //│  ◇◇◇◇◇◇ Getter ◇◇◇◇◇◇                                                                                      │
        //└────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
        public T this[int index] => Elements[index];
            
        //┌────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
        //│  ◇◇◇◇◇◇ Setter ◇◇◇◇◇◇                                                                                      │
        //└────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
        public void SetCurrentFormation(in FormationData destinationFormation) => Formation.SetFromFormation(destinationFormation);
        public void SetTargetFormation(in FormationData destinationFormation) => TargetFormation.SetFromFormation(destinationFormation);
        public void SetTargetPosition(in float3 leaderTargetPosition) => LeaderTargetPosition = leaderTargetPosition;
        
        public void SetDestination(in float3 leaderTargetPosition, in FormationData destinationFormation)
        {
            LeaderTargetPosition = leaderTargetPosition;
            TargetFormation.SetFromFormation(destinationFormation);
        }
            
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Events ◈◈◈◈◈◈                                                                                           ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        public event Action<int, int> OnSwapEvent;
        public event Action<int> OnFormationResized;
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                            ◆◆◆◆◆◆ CONSTRUCTOR ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        public FormationMatrix(Formation formationReference, float3 currentPosition = default, int capacity = 0)
        {
            LeaderTargetPosition = currentPosition;
            Formation = formationReference;
            TargetFormation = new Formation(formationReference);
            Elements = new List<T>(capacity);
            Transforms = new List<Transform>(capacity);
            FormationTransformAccessArray = new TransformAccessArray(capacity);
            IndexToRealTransformIndex = new NativeList<int>(capacity, Persistent);
            ElementKeyTransformIndex = new Dictionary<T, int>(capacity);
        }
        
        public FormationMatrix(Formation formationReference, List<T> formationElements, float3 currentPosition = default) 
            : this(formationReference, currentPosition, formationElements.Count)
        {
            Elements = formationElements; // we link the lists
            foreach (T element in formationElements)
            {
                int index = Transforms.Count;
                Transforms.Add(element.transform);
                FormationTransformAccessArray.Add(element.transform);
                IndexToRealTransformIndex.Add(index);
                ElementKeyTransformIndex.Add(element, index);
            }
        }
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                          ◆◆◆◆◆◆ CLASS METHODS ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        public void OnUpdate()
        {
            if (deadElements.Count <= 0) return;
            Rearrangement();
        }

        private int GetIndexInFormation(T element)
        {
            return Elements.IndexOf(element);
        }
        
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Request Calls ◈◈◈◈◈◈                                                                                    ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        public void OnUnitKilled(T element)
        {
            deadElements.Add(GetIndexInFormation(element));
        }
        
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Add | Remove ◈◈◈◈◈◈                                                                                     ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        public void Add(T element)
        {
            int index = Elements.Count;
            Elements.Add(element);
            Transforms.Add(element.transform);
            FormationTransformAccessArray.Add(element.transform);
            IndexToRealTransformIndex.Add(index);
            ElementKeyTransformIndex.Add(element, index);
        }
        
        public bool Remove(T element)
        {
            bool elementExist = ElementKeyTransformIndex.TryGetValue(element, out int indexToRemove);
            if (!elementExist) return false;
            FormationTransformAccessArray.RemoveAtSwapBack(indexToRemove);
            Transforms.RemoveAtSwapBack(indexToRemove);
            Elements.Remove(element);
            ResetTransformsIndicators();
            return true;
        }

        private void ResetTransformsIndicators()
        {
            IndexToRealTransformIndex.Clear();
            ElementKeyTransformIndex.Clear();
            foreach (T aliveElement in Elements)
            {
                int indexElement = Transforms.IndexOf(aliveElement.transform);
                IndexToRealTransformIndex.Add(indexElement);
                ElementKeyTransformIndex.Add(aliveElement, indexElement);
            }
        }
        
        public void Clear()
        {
            Transforms.Clear();
            Elements.Clear();
            ElementKeyTransformIndex.Clear();
        }
        
        public void Dispose()
        {
            if(IndexToRealTransformIndex.IsCreated) IndexToRealTransformIndex.Dispose();
            if(FormationTransformAccessArray.isCreated) FormationTransformAccessArray.Dispose();
        }
        
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Rearrangement ◈◈◈◈◈◈                                                                                    ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
    
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
            deadElements.Clear();
            return;
            //┌▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁┐
            //▕  ◇◇◇◇◇◇ Internal Methods ◇◇◇◇◇◇                                                                        ▏
            //└▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔┘
            void SwapRearrange()
            {
                int deadIndex = deadElements.Min;
                if (!RearrangementUtils.TryGetIndexAround(deadIndex, this, out int swapIndex))
                {
                    deadElements.Remove(deadIndex);
                    return;
                }
                SwapByIndex(deadIndex, swapIndex);
                deadElements.Remove(deadIndex);
                deadElements.Add(swapIndex);
            }
        }
        
        private void Rearrangement()
        {
            if (!RegisterDeaths(out int cacheNumDead)) return;
            TargetFormation.Remove(cacheNumDead); //was needed before rearrange, make more sense to let it here anyway
            Rearrange();
            if (cacheNumDead >= Elements.Count)
            {
                Clear();
            }
            else
            {
                CleanDeadElements(cacheNumDead);
                ResetTransformsIndicators();
            }
            Formation.Remove(cacheNumDead);
            OnFormationResized?.Invoke(Formation.NumUnitsAlive);
        }

        private void CleanDeadElements(int numDead)
        {
            NativeArray<int> trashItems = new (numDead, Temp, UninitializedMemory);
            for (int i = 0; i < numDead; i++)
            {
                T elementToRemove = Elements[^1];
                trashItems[i] = ElementKeyTransformIndex[elementToRemove];
                Elements.RemoveAt(Elements.Count-1);
                elementToRemove.AfterRemoval();
            }
            
            // MUST BE DONE IN REVERSE! because of the nature of RemoveAtSwapBack
            trashItems.Sort();
            for (int i = numDead - 1; i > -1; i--)
            {
                int index = trashItems[i];
                FormationTransformAccessArray.RemoveAtSwapBack(index);
                Transforms.RemoveAtSwapBack(index);
            }
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
