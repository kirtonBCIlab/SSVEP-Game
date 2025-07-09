using UnityEngine;
using BCIEssentials.Controllers;
using BCIEssentials.StimulusEffects;
using System.Collections;


namespace BCIEssentials.ControllerBehaviors
{
    public class Custom_SSVEPControllerBehavior : SSVEPControllerBehavior
    {
        public override BCIBehaviorType BehaviorType => BCIBehaviorType.SSVEP;
        public enum StimulusType
        {
            BW,
            Custom
        }

        public enum ContrastLevel
        {
            Contrast1,
            Contrast2,
            Contrast3,
            Contrast4,            
        }
        public enum Size
        {
            Size1,
            Size2,
            Size3,
        }

        [Header("Stimulus Parameters")]
        [SerializeField] public StimulusType _stimulusType;
        public ContrastLevel _contrastLevel;
        public Size _size;

        void Update()
        {
            if (StimulusRunning)
            {
                UpdateStimulus();
            }
        }

        protected override void Start()
        {
            SetStimType();
            base.ExecuteSelfRegistration(); //this is to keep the same behavior as BCIControllerBehavior
        }

        protected override IEnumerator RunStimulusRoutine()
        {
            Debug.Log("Starting");
            yield return new WaitForSecondsRealtime(5.0f);
            Debug.Log("Ending");
        }

            
        
        private void SetStimType()
        {
            Custom_ColorFlashEffect spoEffect;

            if (_stimulusType == StimulusType.BW)
            {
                foreach (var spo in _selectableSPOs)
                {
                    spoEffect = spo.GetComponent<Custom_ColorFlashEffect>();
                    spoEffect.SetContrast(Custom_ColorFlashEffect.ContrastLevel.White);
                    spoEffect.SetSize(Custom_ColorFlashEffect.Size.Size3);
                }
            }
            else
            {
                foreach (var spo in _selectableSPOs)
                {
                    spoEffect = spo.GetComponent<Custom_ColorFlashEffect>();
                    if (_contrastLevel == ContrastLevel.Contrast1)
                    {
                        spoEffect.SetContrast(Custom_ColorFlashEffect.ContrastLevel.Contrast1);
                    }
                    else if (_contrastLevel == ContrastLevel.Contrast2)
                    {
                        spoEffect.SetContrast(Custom_ColorFlashEffect.ContrastLevel.Contrast2);
                    }
                    else if (_contrastLevel == ContrastLevel.Contrast3)
                    {
                        spoEffect.SetContrast(Custom_ColorFlashEffect.ContrastLevel.Contrast3);
                    }
                    else if (_contrastLevel == ContrastLevel.Contrast4)
                    {
                        spoEffect.SetContrast(Custom_ColorFlashEffect.ContrastLevel.Contrast4);
                    }

                    if (_size == Size.Size1)
                    {
                        spoEffect.SetSize(Custom_ColorFlashEffect.Size.Size1);
                    }
                    else if (_size == Size.Size2)
                    {
                        spoEffect.SetSize(Custom_ColorFlashEffect.Size.Size2);
                    }
                    else if (_size == Size.Size3)
                    {
                        spoEffect.SetSize(Custom_ColorFlashEffect.Size.Size3);
                    }
                }
            }
        }
    }
}
