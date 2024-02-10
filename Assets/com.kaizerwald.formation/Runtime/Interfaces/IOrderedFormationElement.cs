using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kaizerwald.FormationModule
{
    public interface IOrderedFormationElement : IFormationElement
    {
        public int IndexInFormation { get; }
    }
}
