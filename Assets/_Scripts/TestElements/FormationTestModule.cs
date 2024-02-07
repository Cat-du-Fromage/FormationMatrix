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
    public class FormationTestModule : MonoBehaviour
    {
        [SerializeField] private Button ResetButton;
        [SerializeField] private Button ExecuteButton;
        [SerializeField] private Button SelectFirstRow;
        private FormationMatrix<FormationElement> formationMatrix;
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                             ◆◆◆◆◆◆ PROPERTIES ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝
        
        public GridLayoutGroup Grid { get; private set; }
        public Formation CurrentFormation { get; private set; }
        
        public List<FormationElement> Elements { get; private set; }
        
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
            for (int i = 0; i < formationMatrix.Formation.Width; i++)
            {
                int indexTaaTarget = formationMatrix.IndexToRealTransformIndex[i];
                Transform taaTarget = formationMatrix.FormationTransformAccessArray[indexTaaTarget];
                
                if (Elements[i].transform != taaTarget) continue;
                Elements[i].OnSelected();
            }
        }

        private void OnDisable()
        {
            ResetButton.onClick.RemoveListener(OnReset);
            ExecuteButton.onClick.RemoveListener(OnExecute);
            SelectFirstRow.onClick.RemoveListener(OnSelectFirstRow);
            formationMatrix.OnSwapEvent -= OnSwap;
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
            formationMatrix.Update();
        }

        private void OnDestroy()
        {
            formationMatrix?.Dispose();
        }

//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                          ◆◆◆◆◆◆ CLASS METHODS ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

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
                if (formationMatrix[i].CurrentState != EFormationTestState.Dead) continue;
                Debug.Log($"execute at index: {i}, State = {Elements[i].CurrentState}");
                formationMatrix.OnUnitKilled(Elements[i]);
            }
        }

        public void OnFirstInitialization()
        {
            formationMatrix?.Dispose();
            CurrentFormation = new Formation(BaseFormation);
            Grid.constraintCount = BaseFormation.Width;

            GameObject prefab = FormationDataSingleton.Instance.FormationElementPrefab;
            Elements = new List<FormationElement>(CurrentFormation.NumUnitsAlive);
            Debug.Log($"OnFirstInitialization: {CurrentFormation.NumUnitsAlive}");
            for (int i = 0; i < CurrentFormation.NumUnitsAlive; i++)
            {
                //int2 coords = KzwMath.GetXY2(i, CurrentFormation.Width);
                FormationElement newElement = Instantiate(prefab, transform).GetComponent<FormationElement>();
                newElement.Initialize(i);
                //newElement.SetElementNumber(i);
                Elements.Add(newElement);
            }
            formationMatrix = new FormationMatrix<FormationElement>(CurrentFormation, Elements);
            formationMatrix.OnSwapEvent += OnSwap;
            formationMatrix.OnFormationEmpty += ClearChildren;
            //formationMatrix.OnFormationRearranged += OnFormationRearrangedEvent;
        }

        private void OnFormationRearrangedEvent()
        {
            
            for (int i = Elements.Count-1; i > -1; i--)
            {
                //Debug.Log($"OnFormationRearrangedEvent: {i} count {Elements.Count}");
                Debug.Log($"OnFormationRearrangedEvent: Dead: {formationMatrix.Elements[i].IsDead} {i} count {formationMatrix.Elements.Count}");
                if (!formationMatrix.Elements[i].IsDead) continue;
                //Debug.Log($"OnFormationRearrangedEvent: {i} count {Elements.Count}");
                Destroy(transform.GetChild(i).gameObject);
                //Elements.RemoveAt(i);
            }
        }
    }
}
