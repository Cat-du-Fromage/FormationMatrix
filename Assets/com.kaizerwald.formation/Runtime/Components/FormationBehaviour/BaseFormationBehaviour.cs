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
//║                                             ◆◆◆◆◆◆ PROPERTIES ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        public float3 LeaderTargetPosition { get; protected set; }
        public Formation Formation { get; protected set; }
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
        
        public int GetIndexInFormation(T element) => Elements.IndexOf(element);
        
        //┌────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
        //│  ◇◇◇◇◇◇ Setter ◇◇◇◇◇◇                                                                                      │
        //└────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
        public void SetCurrentFormation(in FormationData currentFormation) => Formation.SetFromFormation(currentFormation);
        public void SetTargetFormation(in FormationData targetFormation) => TargetFormation.SetFromFormation(targetFormation);
        public void SetTargetPosition(in float3 leaderTargetPosition) => LeaderTargetPosition = leaderTargetPosition;
        
        public void SetDestination(in float3 leaderTargetPosition, in FormationData targetFormation)
        {
            LeaderTargetPosition = leaderTargetPosition;
            TargetFormation.SetFromFormation(targetFormation);
        }
        
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Events ◈◈◈◈◈◈                                                                                           ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        public abstract event Action<int> OnFormationResized;
        
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
        public abstract void OnUpdate();
        public abstract void SetElementInactive(T element);
        
        //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
        //║ ◈◈◈◈◈◈ Initialization Methods ◈◈◈◈◈◈                                                                           ║
        //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        
        public virtual void Initialize(Formation formationReference, List<T> formationElements, float3 leaderPosition = default)
        {
            LeaderTargetPosition = leaderPosition;
            Formation = new Formation(formationReference);
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
        
        protected abstract bool Remove(T element);
        
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
    }
}
