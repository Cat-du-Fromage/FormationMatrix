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

        protected BaseFormationBehaviour<FormationElementBehaviour> FormationMatrix;
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                             ◆◆◆◆◆◆ PROPERTIES ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝
        public bool IsInactive { get; protected set; }
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                          ◆◆◆◆◆◆ CLASS METHODS ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        public void AttachToFormationMatrix(BaseFormationBehaviour<FormationElementBehaviour> formationMatrix)
        {
            FormationMatrix = formationMatrix;
        }

        //previously: TriggerDeath()
        public virtual void TriggerInactiveElement()
        {
            IsInactive = true;
            FormationMatrix.RegisterInactiveElement(this);
        }

        public virtual void BeforeRemoval() { return; }

        public virtual void AfterRemoval() { return; }

        public virtual void OnRearrangement(int newIndex) { return; }
    }
}
