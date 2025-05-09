using UnityEngine;
using BCIEssentials.Controllers;
using BCIEssentials.StimulusEffects;


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

        protected override void Start()
        {
            SetStimType();
            base.ExecuteSelfRegistration(); //this is to keep the same behavior as BCIControllerBehavior

            // Move all SPOs in view of the camera at the start
            MoveAllSPOsInViewOfCamera();
        }

       public void MoveAllSPOsInViewOfCamera()
        {
            // Get the main camera
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("Main camera not found.");
                return;
            }

            // Define hardcoded positions for each SPO
            Vector3[] hardcodedPositions = new Vector3[]
            {
                new Vector3(1617, 157, -998),
                new Vector3(775, 1000, -998),
                new Vector3(-893, -662, -998),
                new Vector3(-57, -1495, -998),
            };

            Quaternion sharedRotation = Quaternion.Euler(0, 0, 45); // Same rotation for all

            int count = Mathf.Min(_selectableSPOs.Count, hardcodedPositions.Length);

            for (int i = 0; i < count; i++)
            {
                var spo = _selectableSPOs[i];
                if (spo == null) continue;

                spo.transform.position = hardcodedPositions[i];
                spo.transform.rotation = sharedRotation;

                Debug.Log($"SPO {i} moved to {spo.transform.position}");
            }
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
