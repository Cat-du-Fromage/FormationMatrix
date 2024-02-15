using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kaizerwald.FormationModule
{
    public class UnorderedFormationBehaviour<T> : BaseFormationBehaviour<T>
    where T : Component, IFormationElement
    {
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                               ◆◆◆◆◆◆ FIELD ◆◆◆◆◆◆                                                  ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝
        
        protected HashSet<T> CachedInactiveElements = new HashSet<T>();
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                             ◆◆◆◆◆◆ PROPERTIES ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝
        
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Events ◈◈◈◈◈◈                                                                                           ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        public override event Action<int> OnFormationResized;
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                          ◆◆◆◆◆◆ CLASS METHODS ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        public override void UpdateFormation()
        {
            if (InactiveElements.Count == 0) return;
            CachedInactiveElements = new HashSet<T>(InactiveElements);
            Rearrangement();
        }
        
        public override void RegisterInactiveElement(T element)
        {
            InactiveElements.Add(element);
        }
        
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Add | Remove ◈◈◈◈◈◈                                                                                     ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        protected override void InternalRemove(T element, int indexToRemove)
        {
            element.BeforeRemoval();
            TargetFormation.Decrement();
            
            base.InternalRemove(element, indexToRemove);
            ElementKeyTransformIndex[Elements[^1]] = ElementKeyTransformIndex[element];
            ElementKeyTransformIndex.Remove(element);
            Elements.RemoveAtSwapBack(indexToRemove);
            
            CurrentFormation.Decrement();
            element.AfterRemoval();
            HandleFormationResized(CurrentFormation.NumUnitsAlive);
            //OnFormationResized?.Invoke(CurrentFormation.NumUnitsAlive);
        }

        protected override bool RegisterInactiveElements(out int numDead)
        {
            numDead = InactiveElements.Count;
            if (numDead == 0) return false;
            foreach (T deadElement in InactiveElements)
            {
                deadElement.BeforeRemoval();
            }
            return true;
        }
        
        protected void RemoveInactiveElements()
        {
            foreach (T deadElement in CachedInactiveElements)
            {
                //Remove(deadElement);
                int transformIndex = ElementKeyTransformIndex[deadElement];
                FormationTransformAccessArray.RemoveAtSwapBack(transformIndex);
                Transforms.RemoveAtSwapBack(transformIndex);
                ElementKeyTransformIndex[Elements[^1]] = ElementKeyTransformIndex[deadElement];
                ElementKeyTransformIndex.Remove(deadElement);
                Elements.RemoveAtSwapBack(transformIndex);
                deadElement.AfterRemoval();
            }
            InactiveElements.ExceptWith(CachedInactiveElements);
        }

        protected override void Rearrangement()
        {
            if (!RegisterInactiveElements(out int cacheNumDead)) return;
            TargetFormation.Remove(cacheNumDead); //was needed before rearrange, make more sense to let it here anyway
            if (cacheNumDead >= Elements.Count)
            {
                Clear();
            }
            else
            {
                RemoveInactiveElements();
            }
            CurrentFormation.Remove(cacheNumDead);
            HandleFormationResized(CurrentFormation.NumUnitsAlive);
            //OnFormationResized?.Invoke(CurrentFormation.NumUnitsAlive);
        }
    }
}
