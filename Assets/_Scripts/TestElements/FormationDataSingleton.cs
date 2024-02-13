using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kaizerwald.FormationModule;
using Kaizerwald.Utilities;
using Unity.Mathematics;
using UnityEngine;

namespace Kaizerwald
{
    public class FormationDataSingleton : Singleton<FormationDataSingleton>
    {
        [field: SerializeField] public GameObject FormationElementPrefab;
        [field: SerializeField] public int NumUnits { get; private set; }
        [field: SerializeField] public int Width { get; private set; }

        
        private HashSet<FormationTestModule> FormationTestModules = new HashSet<FormationTestModule>(2);
        
        public Formation FormationTest { get; private set; }

        protected override void OnAwake()
        {
            base.OnAwake();
            FormationTest = new Formation(NumUnits, new int2(Width), new float2(1), width: Width);
        }

        private void Start()
        {
            Debug.Log($"FormationDataSingleton: Start = {FormationTestModules.Count}");
            int i = 0;
            foreach (FormationTestModule testModule in FormationTestModules)
            {
                Debug.Log($"FormationDataSingleton: {i++}");
                testModule.OnFirstInitialization();
            }
        }

        public void UpdateTestModules()
        {
            foreach (FormationTestModule testModule in FormationTestModules)
            {
                testModule.OnFirstInitialization();
            }
        }

        public void RegisterTestModule(FormationTestModule module)
        {
            FormationTestModules.Add(module);
        }
    }
}
