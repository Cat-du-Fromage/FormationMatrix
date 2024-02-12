using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kaizerwald.FormationModule
{
    public abstract class FormationElementBehaviour : MonoBehaviour, IFormationElement
    {
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                               ◆◆◆◆◆◆ FIELD ◆◆◆◆◆◆                                                  ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        protected FormationMatrixBehaviour<FormationElementBehaviour> FormationMatrix;
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                             ◆◆◆◆◆◆ PROPERTIES ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝
        
        public int IndexInFormation { get; protected set; }
        public bool IsDead { get; protected set; }
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                          ◆◆◆◆◆◆ CLASS METHODS ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        public void AttachToFormationMatrix(FormationMatrixBehaviour<FormationElementBehaviour> formationMatrix)
        {
            FormationMatrix = formationMatrix;
        }

        //previously: TriggerDeath()
        public virtual void TriggerInactiveElement()
        {
            IsDead = true;
            FormationMatrix.OnUnitKilled(this);
        }

        public virtual void BeforeRemoval() { return; }

        public virtual void AfterRemoval() { return; }

        public virtual void OnRearrangement(int newIndex) { return; }
    }
}
