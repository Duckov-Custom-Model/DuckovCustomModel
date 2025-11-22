using System;
using DuckovCustomModel.Core.Managers;
using Newtonsoft.Json;
using UnityEngine;

namespace DuckovCustomModel.Core.MonoBehaviours.Animators
{
    /// <summary>
    ///     Drives animator parameters when animation states are entered, similar to Unity's built-in Animator Parameter
    ///     Driver.
    ///     Attach this component to animation state machine states to automatically set, add, randomize, or copy animator
    ///     parameters
    ///     when the state is entered. Useful for controlling complex animation logic and parameter changes in a modular way.
    /// </summary>
    public class ModelParameterDriver : StateMachineBehaviour, ISerializationCallbackReceiver
    {
        public enum ChangeType
        {
            Set,
            Add,
            Random,
            Copy,
        }

        [HideInInspector] [SerializeField] private string parametersData = string.Empty;

        [Tooltip(
            "Custom debug message that will be written to the client logs when the ParameterDriver is used.  Be careful to remove these before your final upload as this can spam your log files.")]
        public string debugString = string.Empty;

        [NonSerialized] private bool _dataLoaded;

        [NonSerialized] public bool Initialized;

        [NonSerialized] public bool IsEnabled;

        [NonSerialized] public Parameter[] Parameters = [];

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!_dataLoaded)
            {
                LoadParametersFromData();
                _dataLoaded = true;
            }

            if (!Initialized)
                AnimatorParameterDriverManager.InitializeDriver(this, animator);

            if (!IsEnabled)
                return;

            if (!string.IsNullOrEmpty(debugString))
                ModLogger.Log($"[AnimatorParameterDriverManager] ParameterDriver Debug: {debugString}");

            foreach (var parameter in Parameters) AnimatorParameterDriverManager.ApplyParameter(parameter, animator);
        }

        public void OnBeforeSerialize()
        {
            if (Parameters is { Length: > 0 }) parametersData = JsonConvert.SerializeObject(Parameters);
        }

        public void OnAfterDeserialize()
        {
        }

        private void LoadParametersFromData()
        {
            if (string.IsNullOrEmpty(parametersData)) return;
            var deserialized = JsonConvert.DeserializeObject<Parameter[]>(parametersData);
            if (deserialized is { Length: > 0 }) Parameters = deserialized;
        }

        [Serializable]
        public class Parameter
        {
            [Tooltip("The type of operation to be executed")]
            public ChangeType type;

            [Tooltip("Parameter that will be written to")]
            public string name = string.Empty;

            [Tooltip("Source parameter that will be read")]
            public string source = string.Empty;

            [Tooltip("The value used for this operation")]
            public float value;

            [Tooltip("Minimum value to be set")] public float valueMin;
            [Tooltip("Maximum value to be set")] public float valueMax = 1f;

            [Tooltip(
                "Chance the value will be set.  When used with a Bool type, defines the chance the value is set to 1, otherwise it's set to 0.")]
            [Range(0.0f, 1f)]
            public float chance = 1f;

            [Tooltip(
                "If true, we convert the range of the source and destination values according to the ranges given.")]
            public bool convertRange;

            public float sourceMin;
            public float sourceMax;
            public float destMin;
            public float destMax;

            /// <summary>
            ///     Runtime-only cached reference to the destination parameter.
            ///     Populated during initialization for performance optimization.
            ///     Not intended to be set manually or serialized.
            /// </summary>
            public object? DestParam;

            /// <summary>
            ///     Runtime-only cached reference to the source parameter.
            ///     Populated during initialization for performance optimization.
            ///     Not intended to be set manually or serialized.
            /// </summary>
            public object? SourceParam;
        }
    }
}
