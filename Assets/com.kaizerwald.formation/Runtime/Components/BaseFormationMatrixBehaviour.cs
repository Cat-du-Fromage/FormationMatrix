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
    public abstract class BaseFormationMatrixBehaviour<T> : MonoBehaviour
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
        public Dictionary<T, int> ElementKeyTransformIndex { get; private set; } 
        
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Accessors ◈◈◈◈◈◈                                                                                        ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        public T this[int index] => Elements[index];
        public int Count => Elements.Count;
        
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
        public abstract void OnUpdate();
        
        //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
        //║ ◈◈◈◈◈◈ Initialization Methods ◈◈◈◈◈◈                                                                           ║
        //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        public virtual BaseFormationMatrixBehaviour<T> Initialize(float3 leaderPosition, Formation formationReference, List<T> formationElements)
        {
            LeaderTargetPosition = leaderPosition;
            Formation = formationReference;
            TargetFormation = new Formation(formationReference);
            
            Elements = formationElements; // we link the lists
            Transforms = new List<Transform>(formationElements.Count);
            FormationTransformAccessArray = new TransformAccessArray(formationElements.Count);
            ElementKeyTransformIndex = new Dictionary<T, int>(formationElements.Count);
            for (int i = 0; i < formationElements.Count; i++)
            {
                T element = formationElements[i];
                Transforms.Add(element.transform);
                FormationTransformAccessArray.Add(element.transform);
                ElementKeyTransformIndex.Add(element, i);
            }
            return this;
        }

    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Add | Remove ◈◈◈◈◈◈                                                                                     ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        public abstract void Add(T element);
        public abstract bool Remove(T element);
        
        public virtual void Clear()
        {
            Transforms.Clear();
            Elements.Clear();
            ElementKeyTransformIndex.Clear();
        }
        
        public virtual void Dispose()
        {
            if(FormationTransformAccessArray.isCreated) FormationTransformAccessArray.Dispose();
        }

    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Rearrangement ◈◈◈◈◈◈                                                                                    ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        protected abstract bool RegisterDeaths(out int numDead);
        protected abstract void ProcessDestroyedElements(int cacheNumDead);
    
        protected void Rearrangement()
        {
            if (!RegisterDeaths(out int cacheNumDead)) return;
            TargetFormation.Remove(cacheNumDead); //was needed before rearrange, make more sense to let it here anyway
            if (cacheNumDead >= Elements.Count)
            {
                Clear();
            }
            else
            {
                ProcessDestroyedElements(cacheNumDead);
            }
            Formation.Remove(cacheNumDead);
            OnFormationResized?.Invoke(Formation.NumUnitsAlive);
        }
    }
}
