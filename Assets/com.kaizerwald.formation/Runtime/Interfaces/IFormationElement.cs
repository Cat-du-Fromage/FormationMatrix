using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kaizerwald.FormationModule
{
    public interface IFormationElement
    {
        public bool IsInactive { get; }

        public void BeforeRemoval();
        
        public void AfterRemoval();
        
        public void OnRearrangement(int newIndex);
    }
}
