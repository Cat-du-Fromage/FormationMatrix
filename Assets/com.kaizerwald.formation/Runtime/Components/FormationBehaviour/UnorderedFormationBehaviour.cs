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

        protected HashSet<T> InactiveElements = new HashSet<T>();
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

        public override void OnUpdate()
        {
            if (InactiveElements.Count == 0) return;
            CachedInactiveElements = new HashSet<T>(InactiveElements);
            Rearrangement();
        }
        
        public override void SetElementInactive(T element)
        {
            InactiveElements.Add(element);
        }

        public void RemoveElement(T element)
        {
            element.BeforeRemoval();
            TargetFormation.Decrement();
            Remove(element);
            element.AfterRemoval();
            Formation.Decrement();
            OnFormationResized?.Invoke(Formation.NumUnitsAlive);
        }
        
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Add | Remove ◈◈◈◈◈◈                                                                                     ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
    
        protected override bool Remove(T element)
        {
            if (!ElementKeyTransformIndex.TryGetValue(element, out int indexToRemove)) return false;
            FormationTransformAccessArray.RemoveAtSwapBack(indexToRemove);
            Transforms.RemoveAtSwapBack(indexToRemove);
            Elements.RemoveAtSwapBack(indexToRemove);
            //pas de décrement! car le dernier élément prend la valeur de l'index "retiré"
            ElementKeyTransformIndex[Elements[^1]] = ElementKeyTransformIndex[element];
            return ElementKeyTransformIndex.Remove(element);
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
                Remove(deadElement);
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
            Formation.Remove(cacheNumDead);
            OnFormationResized?.Invoke(Formation.NumUnitsAlive);
        }
    }
}
