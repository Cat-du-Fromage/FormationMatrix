using System;
using System.Collections;
using System.Collections.Generic;
using Kaizerwald.FormationModule;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kaizerwald
{
    public enum EFormationTestState
    {
        None,
        Dead,
        Selected
    }
    
    public class FormationElement : FormationElementBehaviour
    {
        public int InitialIndex;
        [field:SerializeField] public int PreviousIndexInRegiment { get; set; }
        [field:SerializeField] public int CurrentIndexInRegiment { get; set; }
        
        //[field:SerializeField] public int IndexInFormation { get; private set; }
        //[field:SerializeField] public bool IsInactive { get; private set; }
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                               ◆◆◆◆◆◆ FIELD ◆◆◆◆◆◆                                                  ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        public Button button;
        private TMP_Text textComponent;
        
        private ColorBlock baseColorBlock;

        [SerializeField]private int ObjectUniqueID;
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                             ◆◆◆◆◆◆ PROPERTIES ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        public int IndexInFormation { get; set; }

        [field:SerializeField] public EFormationTestState CurrentState { get; private set; }
        
        private ColorBlock deadColorBlock = new ColorBlock
        {
            normalColor = Color.red,
            highlightedColor = new Color(0.7f, 0, 0, 0.9f),
            pressedColor = new Color(0.5f, 0, 0, 0.5f),
            selectedColor = Color.red,
            disabledColor = Color.red,
            colorMultiplier = 1.0f,
            fadeDuration = 0.1f,
        };

        private ColorBlock selectColor = new ColorBlock
        {
            normalColor = Color.yellow,
            highlightedColor = new Color(1f,0.7f,0.05f,1f),
            pressedColor = Color.yellow,
            selectedColor = Color.yellow,
            disabledColor = Color.yellow,
            colorMultiplier = 1.0f,
            fadeDuration = 0.1f,
        };
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                            ◆◆◆◆◆◆ UNITY EVENTS ◆◆◆◆◆◆                                              ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        private void Awake()
        {
            button = GetComponent<Button>();
            textComponent = GetComponentInChildren<TMP_Text>();
            CurrentState = EFormationTestState.None;
            baseColorBlock = button.colors;
            ObjectUniqueID = GetInstanceID();
        }

        private void OnEnable()
        {
            button.onClick.AddListener(OnKilled);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(OnKilled);
        }
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                          ◆◆◆◆◆◆ CLASS METHODS ◆◆◆◆◆◆                                               ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        public void Initialize(int index)
        {
            IndexInFormation = index;
            InitialIndex = PreviousIndexInRegiment = CurrentIndexInRegiment = index;
            SetElementNumber(index);
        }
        
        public override void TriggerInactiveElement()
        {
            //IsInactive = true;
            FormationMatrix.RegisterInactiveElement(this);
        }

        public override void BeforeRemoval()
        {
            IsInactive = true;
        }

        public override void AfterRemoval()
        {
            Destroy(this.gameObject);
        }

        public string GetStringInfo()
        {
            return $"now: {CurrentIndexInRegiment}(before: {PreviousIndexInRegiment}), Initial: {InitialIndex}";
        }

        public override void OnRearrangement(int newIndexInFormation)
        {
            SetElementNumber(newIndexInFormation);
        }

        public void SetElementNumber(int index)
        {
            (PreviousIndexInRegiment, CurrentIndexInRegiment) = (CurrentIndexInRegiment, index);
            textComponent.text = $"{CurrentIndexInRegiment}({InitialIndex})";
        }

        public void UpdateColorInfo()
        {
            if (CurrentState == EFormationTestState.None)
            {
                button.colors = baseColorBlock;
            }
            else if (CurrentState == EFormationTestState.Dead)
            {
                SetDead();
            }
            else if (CurrentState == EFormationTestState.Selected)
            {
                button.colors = selectColor;
            }
        }

        private void OnKilled()
        {
            if (CurrentState == EFormationTestState.None)
            {
                CurrentState = EFormationTestState.Dead;
            }
            else if (CurrentState == EFormationTestState.Dead)
            {
                CurrentState = EFormationTestState.None;
            }
            UpdateColorInfo();
        }

        public void OnSelected()
        {
            if (CurrentState == EFormationTestState.None)
            {
                CurrentState = EFormationTestState.Selected;
            }
            else if (CurrentState == EFormationTestState.Selected)
            {
                CurrentState = EFormationTestState.None;
            }
            UpdateColorInfo();
        }

        public void SetSelected()
        {
            CurrentState = EFormationTestState.Selected;
            button.colors = selectColor;
        }

        public void SetDead()
        {
            button.colors = deadColorBlock;
            CurrentState = EFormationTestState.Dead;
        }
    }
}
