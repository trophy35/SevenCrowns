using UnityEngine;

namespace SevenCrowns.SceneFlow
{
    [CreateAssetMenu(fileName = "SceneFlowConfig", menuName = "SevenCrowns/SceneFlow/Config")]
    public sealed class SceneFlowConfig : ScriptableObject
    {
        [Header("Timings (seconds)")]
        [SerializeField, Min(0f)] public float fadeOutSeconds = 0.35f;
        [SerializeField, Min(0f)] public float fadeInSeconds = 0.35f;

        [Header("Visuals")]
        [SerializeField] public Color fadeColor = Color.black;
        [SerializeField] public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Behavior")]
        [SerializeField] public bool blockRaycastsDuringFade = true;
        [SerializeField] public int canvasSortOrder = 32767;
    }
}

