using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

using static Unity.Collections.Allocator;
using static Unity.Collections.NativeArrayOptions;

namespace Kaizerwald.FormationModule
{
    public class UnorderedFormationMatrix<T>
    where T : Component, IFormationElement
    {
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                               ◆◆◆◆◆◆ FIELD ◆◆◆◆◆◆                                                  ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        private HashSet<T> destroyedElements = new HashSet<T>();
        private HashSet<T> cacheDestroyedElements = new HashSet<T>();
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                             ◆◆◆◆◆◆ PROPERTIES ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝
        // Reference
        public Formation Formation { get; private set; }
        public Formation TargetFormation { get; private set; }
        public List<T> Elements { get; private set; } 
        
        
        public TransformAccessArray FormationTransformAccessArray { get; private set; }
        public List<Transform> Transforms { get; private set; }
        public Dictionary<T, int> ElementKeyTransformIndex { get; private set; } 
        
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Accessors ◈◈◈◈◈◈                                                                                        ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        public T this[int index] => Elements[index];
        
        public event Action OnFormationEmpty;
        public event Action<int> OnFormationResized;
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                            ◆◆◆◆◆◆ CONSTRUCTOR ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        public UnorderedFormationMatrix(Formation formationReference, int capacity = 0)
        {
            Formation = formationReference;
            TargetFormation = new Formation(formationReference);
            Elements = new List<T>(capacity);
            Transforms = new List<Transform>(capacity);
            FormationTransformAccessArray = new TransformAccessArray(capacity);
            ElementKeyTransformIndex = new Dictionary<T, int>(capacity);
        }

        public UnorderedFormationMatrix(Formation formationReference, List<T> formationElements) : this(formationReference, formationElements.Count)
        {
            Elements = formationElements;
            foreach (T element in formationElements)
            {
                int index = Transforms.Count;
                Transforms.Add(element.transform);
                FormationTransformAccessArray.Add(element.transform);
                ElementKeyTransformIndex.Add(element, index);
            }
        }
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                          ◆◆◆◆◆◆ CLASS METHODS ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        public void Update()
        {
            if (destroyedElements.Count <= 0) return;
            cacheDestroyedElements = new HashSet<T>(destroyedElements);
            Rearrangement();
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
            ElementKeyTransformIndex.Add(element, index);
        }
        
        public bool Remove(T element)
        {
            bool elementExist = ElementKeyTransformIndex.TryGetValue(element, out int indexToRemove);
            if (!elementExist) return false;
            
            FormationTransformAccessArray.RemoveAtSwapBack(indexToRemove);
            Transforms.RemoveAtSwapBack(indexToRemove);
            Elements.RemoveAtSwapBack(indexToRemove);

            //pas de décrement! car le dernier élément prend la valeur de l'index "retiré"
            ElementKeyTransformIndex[Elements[^1]] = ElementKeyTransformIndex[element];
            return ElementKeyTransformIndex.Remove(element);
        }

        public void Destroy(T element)
        {
            destroyedElements.Add(element);
        }

        public void DestroyImmediate(T element)
        {
            Remove(element);
        }
        
        public void Clear()
        {
            Transforms.Clear();
            Elements.Clear();
            ElementKeyTransformIndex.Clear();
        }

    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Rearrangement ◈◈◈◈◈◈                                                                                    ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        private bool RegisterDeaths(out int numDead)
        {
            numDead = destroyedElements.Count;
            if (numDead == 0) return false;
            foreach (T deadElement in destroyedElements)
            {
                deadElement.BeforeRemoval();
            }
            return true;
        }
        
        private void ResizeFormation(int numToRemove)
        {
            Formation.Remove(numToRemove);
            TargetFormation.Remove(numToRemove);
            if (Formation.NumUnitsAlive == 0)
            {
                OnFormationEmpty?.Invoke();
            }
            else
            {
                OnFormationResized?.Invoke(Formation.NumUnitsAlive);
            }
        }

        private void ProcessDestroyedElements()
        {
            foreach (T deadElement in cacheDestroyedElements)
            {
                Remove(deadElement);
                deadElement.AfterRemoval();
            }
            destroyedElements.ExceptWith(cacheDestroyedElements);
        }
    
        private void Rearrangement()
        {
            if (!RegisterDeaths(out int cacheNumDead)) return;
            if (cacheNumDead >= Elements.Count)
            {
                Clear();
            }
            else
            {
                ProcessDestroyedElements();
            }
            ResizeFormation(cacheNumDead);
        }
    }
}
