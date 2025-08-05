using System.Collections;
using UnityEngine;
using BCIEssentials.Utilities;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace BCIEssentials.StimulusEffects
{
    public class Custom_ColorFlashEffect : StimulusEffect
    {
        [SerializeField]
        [Tooltip("The renderer to assign the material color to")]
        public Renderer _renderer;

        [SerializeField]
        public Material materialList;

        [Header("Flash Settings")]
        private Color _flashOnColor = Color.blue;
        private Color _flashOffColor = Color.black;
        
        private float _flashDurationSeconds = 0.2f;

        private int _flashAmount = 3;

        public bool IsPlaying => _effectRoutine != null;

        private Coroutine _effectRoutine;

        public enum ContrastLevel
        {
            Contrast1,
            Contrast2,
            Contrast3,
            Contrast4,
            White
        }

        public enum Size
        {
            Size1,
            Size2,
            Size3,
        }

        public ContrastLevel _contrastLevel;
        public Size _size;
    
        private void Start()
        {
            if (_renderer == null && !gameObject.TryGetComponent(out _renderer))
            {
                Debug.LogWarning($"No Renderer component found for {gameObject.name}");
                return;
            }

            if (_renderer.material == null)
            {
                Debug.LogWarning($"No material assigned to renderer component on {gameObject.name}.");
            }

            _renderer.material = materialList;
            AssignMaterialColor(_flashOffColor);
        }

        public override void SetOn()
        {
            if (_renderer == null || _renderer.material == null)
                return;

            AssignMaterialColor(_flashOnColor);

            IsOn = true;
        }
        public override void SetOff()
        {
            if (_renderer == null || _renderer.material == null)
                return;
            
            AssignMaterialColor(_flashOffColor);
            IsOn = false;
        }

        public void Play()
        {
            Stop();
            _effectRoutine = StartCoroutine(RunEffect());
        }

        public void Stop()
        {
            if (!IsPlaying)
                return;

            SetOff();
            StopCoroutine(_effectRoutine);
            _effectRoutine = null;
        }

        private IEnumerator RunEffect()
        {
            if (_renderer != null && _renderer.material != null)
            {
                IsOn = true;
                
                for (var i = 0; i < _flashAmount; i++)
                {
                    AssignMaterialColor(_flashOnColor);
                    yield return new WaitForSecondsRealtime(_flashDurationSeconds);

                    AssignMaterialColor(_flashOffColor);
                    yield return new WaitForSecondsRealtime(_flashDurationSeconds);
                }
            }

            SetOff();
            _effectRoutine = null;
        }

/// <summary>
/// //////////Helper methods
/// </summary>
        private void ContrastController()
        {
            if (_contrastLevel == ContrastLevel.White)
            {
                _flashOnColor = Color.white;
                _flashOffColor = Color.black;
                return;
            }
            
            Custom_ColorContrast colorContrast = GetComponent<Custom_ColorContrast>();
            int contrastIntValue = ConvertContrastLevel(_contrastLevel);
            colorContrast.SetContrast(contrastIntValue);
            _flashOnColor = colorContrast.Grey();
        }

        private void SizeController()
        {
            Vector3 newSize = new Vector3(0.001f, 0.001f, 0); // Default size is (1, 1, 1)

            switch (_size)
            {
                case Size.Size1:
                    //3.6 cm
                    newSize = new Vector3(0.0007f, 0.0007f, 0); // this scaling gives stim of 3.6cm on the screen
                    break;

                case Size.Size2:
                    // 6.1cm
                    newSize = new Vector3(0.00119f, 0.00119f, 0); // this scaling gives stim of 6.1cm on the screen
                    //newSize = new Vector3(1,1,1); 
                    break;

                case Size.Size3:
                    //8.5 cm
                    newSize = new Vector3(0.00168f, 0.00168f, 0); // this scaling gives stim of 3.6cm on the screen
                    // newSize = new Vector3(1,1,1);
                    break;
            }

            // Apply the size change
            transform.localScale = newSize;
            Debug.Log("size changes");
        }

        public int ConvertContrastLevel(ContrastLevel _contrastLevel)
        {
            if(_contrastLevel == ContrastLevel.Contrast1)
                return 100;
            else if (_contrastLevel == ContrastLevel.Contrast2)
                return 50;
            else if (_contrastLevel == ContrastLevel.Contrast3)
                return 25;
            else if (_contrastLevel == ContrastLevel.Contrast4)
                return 10;
            else return 0;
        }

        public void AssignMaterialColor(Color color)
        {
            _renderer.material.SetColor("_Color", color);
        }

        public void SetContrast(ContrastLevel x)
        {
            _contrastLevel = x;
            ContrastController();
        }

        public void SetSize(Size x)
        {
            _size = x;
            SizeController();
        }
    }
 }