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
    public abstract class BaseFormationBehaviour<T> : MonoBehaviour
    where T : Component, IFormationElement
    {
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                               ◆◆◆◆◆◆ FIELD ◆◆◆◆◆◆                                                  ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        protected HashSet<T> InactiveElements = new HashSet<T>();
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                             ◆◆◆◆◆◆ PROPERTIES ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        public float3 TargetPosition { get; protected set; }
        public Formation CurrentFormation { get; protected set; }
        public Formation TargetFormation { get; protected set; }
        
        public List<T> Elements{ get; protected set; } // Ordered by "index in formation"
        public List<Transform> Transforms { get; protected set; }
        public TransformAccessArray FormationTransformAccessArray { get; protected set; }
        public Dictionary<T, int> ElementKeyTransformIndex { get; protected set; } 
        
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Accessors ◈◈◈◈◈◈                                                                                        ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        public T this[int index] => Elements[index];
        public int Count => Elements.Count;
        
        public virtual int GetIndexInFormation(T element) => Elements.IndexOf(element);
        
        //┌────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
        //│  ◇◇◇◇◇◇ Setter ◇◇◇◇◇◇                                                                                      │
        //└────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
        public void SetCurrentFormation(in FormationData currentFormation) => CurrentFormation.SetFromFormation(currentFormation);
        public void SetTargetFormation(in FormationData targetFormation) => TargetFormation.SetFromFormation(targetFormation);
        public void SetTargetPosition(in float3 leaderTargetPosition) => TargetPosition = leaderTargetPosition;
        
        public void SetDestination(in float3 leaderTargetPosition, in FormationData targetFormation)
        {
            TargetPosition = leaderTargetPosition;
            TargetFormation.SetFromFormation(targetFormation);
        }
        
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Events ◈◈◈◈◈◈                                                                                           ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        public virtual event Action<int> OnFormationResized;
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                             ◆◆◆◆◆◆ UNITY EVENTS ◆◆◆◆◆◆                                             ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        protected virtual void OnDestroy()
        {
            Dispose();
        }
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                          ◆◆◆◆◆◆ CLASS METHODS ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝
        public abstract void UpdateFormation();

        public virtual void RegisterInactiveElement(T element)
        {
            InactiveElements.Add(element);
        }
        
        //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
        //║ ◈◈◈◈◈◈ Initialization Methods ◈◈◈◈◈◈                                                                           ║
        //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        
        public virtual void InitializeFormation(Formation formationReference, List<T> formationElements, float3 leaderPosition = default)
        {
            TargetPosition = leaderPosition;
            CurrentFormation = new Formation(formationReference);
            TargetFormation = new Formation(formationReference);
            
            Elements = new List<T>(formationElements.Count);
            Transforms = new List<Transform>(formationElements.Count);
            FormationTransformAccessArray = new TransformAccessArray(formationElements.Count);
            ElementKeyTransformIndex = new Dictionary<T, int>(formationElements.Count);
            formationElements.ForEach(Add);
        }
        
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Add | Remove ◈◈◈◈◈◈                                                                                     ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        
        public virtual void Add(T element)
        {
            int index = Transforms.Count;
            Elements.Add(element);
            Transforms.Add(element.transform);
            FormationTransformAccessArray.Add(element.transform);
            ElementKeyTransformIndex.Add(element, index);
        }
        
        public bool Remove(T element)
        {
            if (!ElementKeyTransformIndex.TryGetValue(element, out int transformIndex)) return false;
            InternalRemove(element, transformIndex);
            return true;
        }
        
        //Allow override but keep clause guard
        protected virtual void InternalRemove(T element, int transformIndex)
        {
            FormationTransformAccessArray.RemoveAtSwapBack(transformIndex);
            Transforms.RemoveAtSwapBack(transformIndex);
        }
        
        public virtual void Clear()
        {
            Transforms?.Clear();
            Elements?.Clear();
            ElementKeyTransformIndex?.Clear();
        }
        
        public virtual void Dispose()
        {
            Clear();
            if(FormationTransformAccessArray.isCreated) FormationTransformAccessArray.Dispose();
        }

    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Rearrangement ◈◈◈◈◈◈                                                                                    ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        protected abstract bool RegisterInactiveElements(out int numDead);
        protected abstract void Rearrangement();
        
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Events ◈◈◈◈◈◈                                                                                           ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        protected virtual void HandleFormationResized(int numElementAfterResize)
        {
            OnFormationResized?.Invoke(numElementAfterResize);
        }
    }
}
