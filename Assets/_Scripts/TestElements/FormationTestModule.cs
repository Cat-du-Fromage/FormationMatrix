using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Kaizerwald.FormationModule;
using Kaizerwald.Utilities;
using Unity.Mathematics;
using UnityEngine.UI;

namespace Kaizerwald
{
    public sealed class FormationTestModule : OrderedFormationBehaviour<FormationElement>
    {
        [SerializeField] private Button ResetButton;
        [SerializeField] private Button ExecuteButton;
        [SerializeField] private Button SelectFirstRow;
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                             ◆◆◆◆◆◆ PROPERTIES ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝
        
        public GridLayoutGroup Grid { get; private set; }
        
    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Accessors ◈◈◈◈◈◈                                                                                        ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        private Formation BaseFormation => FormationDataSingleton.Instance.FormationTest;
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                            ◆◆◆◆◆◆ UNITY EVENTS ◆◆◆◆◆◆                                              ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        private void Awake()
        {
            Grid = GetComponent<GridLayoutGroup>();
            FormationDataSingleton.Instance.RegisterTestModule(this);
        }

        private void OnEnable()
        {
            ResetButton.onClick.AddListener(OnReset);
            ExecuteButton.onClick.AddListener(OnExecute);
            SelectFirstRow.onClick.AddListener(OnSelectFirstRow);
        }

        private void OnSelectFirstRow()
        {
            for (int i = 0; i < Formation.Width; i++)
            {
                int indexTaaTarget = ElementIndexToTransformIndex[i];
                Transform taaTarget = FormationTransformAccessArray[indexTaaTarget];
                if (Elements[i].transform != taaTarget) continue;
                Elements[i].OnSelected();
            }
        }

        private void OnDisable()
        {
            ResetButton.onClick.RemoveListener(OnReset);
            ExecuteButton.onClick.RemoveListener(OnExecute);
            SelectFirstRow.onClick.RemoveListener(OnSelectFirstRow);
            OnElementSwapped -= OnSwap;
        }
        
        private void OnSwap(int rhs, int lhs)
        {
            transform.DetachChildren();
            for (int i = 0; i < Elements.Count; i++)
            {
                Elements[i].transform.SetParent(transform);
            }
        }

        private void Update()
        {
            OnUpdate();
        }

//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                          ◆◆◆◆◆◆ CLASS METHODS ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        private void Resize(int numAlive)
        {
            if (numAlive > 0)
            {
                
            }
            else
            {
                ClearChildren();
            }
        }

        public void ClearChildren()
        {
            transform.DestroyChildren();
            Elements.Clear();
        }

        public void OnReset()
        {
            transform.DestroyChildren();
            ClearChildren();
            OnFirstInitialization();
        }

        public void OnExecute()
        {
            for (int i = 0; i < Elements.Count; i++)
            {
                if (Elements[i].CurrentState != EFormationTestState.Dead) continue;
                Debug.Log($"execute at index: {i}, State = {Elements[i].CurrentState}");
                SetElementInactive(Elements[i]);
            }
        }

        
        public void OnFirstInitialization()
        {
            //Clear();
            //Dispose();
            Formation tmpFormation = new Formation(BaseFormation);
            Grid.constraintCount = tmpFormation.Width;

            GameObject prefab = FormationDataSingleton.Instance.FormationElementPrefab;
            List<FormationElement> tmpElements = new List<FormationElement>(tmpFormation.NumUnitsAlive);
            
            Debug.Log($"OnFirstInitialization: {tmpFormation.NumUnitsAlive}");
            for (int i = 0; i < tmpFormation.NumUnitsAlive; i++)
            {
                //int2 coords = KzwMath.GetXY2(i, CurrentFormation.Width);
                GameObject newElementGameObject = Instantiate(prefab, transform);
                FormationElement newElement = newElementGameObject.GetComponent<FormationElement>();
                newElement.Initialize(i);
                //newElement.SetElementNumber(i);
                tmpElements.Add(newElement);
            }
            Initialize(tmpFormation, tmpElements);
            OnElementSwapped += OnSwap;
            OnFormationResized += Resize;
        }

        private void OnFormationRearrangedEvent()
        {
            for (int i = Elements.Count-1; i > -1; i--)
            {
                Debug.Log($"OnFormationRearrangedEvent: Dead: {Elements[i].IsInactive} {i} count {Count}");
                if (!Elements[i].IsInactive) continue;
                Destroy(transform.GetChild(i).gameObject);
            }
        }
    }
}
