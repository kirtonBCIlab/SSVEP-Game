using UnityEngine;
using BCIEssentials.Controllers;

namespace BCIEssentials.ControllerBehaviors
{
    public class Custom_SSVEPControllerBehavior : FrequencyStimulusControllerBehaviour
    {
        public override BCIBehaviorType BehaviorType => BCIBehaviorType.SSVEP;
        
        [StartFoldoutGroup("Stimulus Frequencies")]
        [SerializeField]
        [Tooltip("User-defined set of target stimulus frequencies [Hz]")]
        private float[] requestedFlashingFrequencies;
        [SerializeField, EndFoldoutGroup, InspectorReadOnly]
        [Tooltip("Calculated best-match achievable frequencies based on the application framerate [Hz]")]
        private float[] realFlashingFrequencies;

        protected override void Start()
        {
            base.ExecuteSelfRegistration(); //this is to keep the same behavior as BCIControllerBehavior

            // Move all SPOs in view of the camera at the start
            MoveAllSPOsInViewOfCamera();
        }

        protected override void SendTrainingMarker(int trainingIndex)
        => MarkerWriter.PushSSVEPTrainingMarker(
            SPOCount, trainingIndex, epochLength, realFlashingFrequencies
        );

        protected override void SendClassificationMarker()
        => MarkerWriter.PushSSVEPClassificationMarker(
            SPOCount, epochLength, realFlashingFrequencies
        );


        protected override void UpdateObjectListConfiguration()
        {
            realFlashingFrequencies = new float[SPOCount];
            base.UpdateObjectListConfiguration();
        }

        protected override float GetRequestedFrequency(int index)
        => requestedFlashingFrequencies[index];
        protected override void SetRealFrequency(int index, float value)
        => realFlashingFrequencies[index] = value;

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

    }
}
