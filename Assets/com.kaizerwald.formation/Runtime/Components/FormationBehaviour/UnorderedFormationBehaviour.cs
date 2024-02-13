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
        /*
        public override void RemoveElementImmediate(T element)
        {
            element.BeforeRemoval();
            TargetFormation.Decrement();
            Remove(element);
            element.AfterRemoval();
            Formation.Decrement();
            OnFormationResized?.Invoke(Formation.NumUnitsAlive);
        }
        */
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Add | Remove ◈◈◈◈◈◈                                                                                     ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        /*
        protected bool Remove(T element)
        {
            if (!ElementKeyTransformIndex.TryGetValue(element, out int indexToRemove)) return false;
            FormationTransformAccessArray.RemoveAtSwapBack(indexToRemove);
            Transforms.RemoveAtSwapBack(indexToRemove);
            Elements.RemoveAtSwapBack(indexToRemove);
            //pas de décrement! car le dernier élément prend la valeur de l'index "retiré"
            ElementKeyTransformIndex[Elements[^1]] = ElementKeyTransformIndex[element];
            return ElementKeyTransformIndex.Remove(element);
        }
        */
        protected override void InternalRemove(T element, int indexToRemove)
        {
            element.BeforeRemoval();
            TargetFormation.Decrement();
            
            base.InternalRemove(element, indexToRemove);
            Elements.RemoveAtSwapBack(indexToRemove);
            
            //Dictionary's RemoveAtSwapBack we set last element's index to the one we remove first
            //to avoid need to decrement : we first Elements.RemoveAtSwapBack(indexToRemove) so Elements[^1]'s index == Count-1
            //this way order is maintain and indices are NOT out of bounds
            ElementKeyTransformIndex[Elements[^1]] = ElementKeyTransformIndex[element];
            ElementKeyTransformIndex.Remove(element);
            
            CurrentFormation.Decrement();
            element.AfterRemoval();
            OnFormationResized?.Invoke(CurrentFormation.NumUnitsAlive);
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
                Elements.RemoveAtSwapBack(transformIndex);
                ElementKeyTransformIndex[Elements[^1]] = ElementKeyTransformIndex[deadElement];
                ElementKeyTransformIndex.Remove(deadElement);
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
            OnFormationResized?.Invoke(CurrentFormation.NumUnitsAlive);
        }
    }
}
