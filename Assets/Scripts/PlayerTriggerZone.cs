using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerTriggerZone : MonoBehaviour
{
    public enum MusicAction { None, PlayOverride, ReturnToPlaylist }
    public enum ToggleAction { None, Enable, Disable }

    [Header("Detection")]
    [SerializeField, Min(0f)] private float m_radius = 5f;
    [SerializeField] private string m_playerTag = "Player";

    [Header("Gizmos")]
    [SerializeField] private Color m_gizmoColor = new(0.2f, 0.8f, 1f, 0.25f);

    [Header("Triggering")]
    [Tooltip("If true, the actions only fire the first time the player enters.")]
    [SerializeField] private bool m_fireOnce = false;

    [Header("Skybox Mode")]
    [SerializeField] private bool m_changeSkyboxMode = false;
    [SerializeField] private SkyboxController.SkyMode m_skyboxMode = SkyboxController.SkyMode.DayNight;

    [Header("Time Of Day")]
    [SerializeField] private bool m_changeTimeOfDay = false;
    [SerializeField, Range(0f, 1f)] private float m_timeOfDay = 0f;
    [Tooltip("If true, stops the day/night cycle from auto-advancing after the time is set.")]
    [SerializeField] private bool m_pauseAutoAdvance = false;

    [Header("Music")]
    [SerializeField] private MusicAction m_musicAction = MusicAction.None;
    [Tooltip("Only used when Music Action is PlayOverride.")]
    [SerializeField] private AudioClip m_overrideClip;

    [Header("Rolling Log")]
    [SerializeField] private bool m_changeRollingLog = false;
    [SerializeField, Range(-0.05f, 0.05f)] private float m_rollingLogAmount = 0.005f;
    [SerializeField, Min(0f)] private float m_rollingLogTransitionDuration = 1.5f;

    [Header("Post Processing")]
    [SerializeField] private ToggleAction m_postProcessAction = ToggleAction.None;
    [Tooltip("Leave null to auto-find a PostProcessController in the scene.")]
    [SerializeField] private PostProcessController m_postProcessController;

    private Transform m_player;
    private bool m_playerInside;
    private bool m_hasFired;
    private Coroutine m_rollingLogRoutine;

    private void Update()
    {
        if (!TryGetPlayer(out Transform player)) return;

        float sqr = (player.position - transform.position).sqrMagnitude;
        bool inside = sqr <= m_radius * m_radius;

        if (inside && !m_playerInside)
            OnPlayerEnter();

        m_playerInside = inside;
    }

    private bool TryGetPlayer(out Transform player)
    {
        if (!m_player)
        {
            GameObject go = GameObject.FindGameObjectWithTag(m_playerTag);
            if (go) m_player = go.transform;
        }
        player = m_player;
        return player;
    }

    private void OnPlayerEnter()
    {
        if (m_fireOnce && m_hasFired) return;
        m_hasFired = true;

        ApplySkybox();
        ApplyMusic();
        ApplyRollingLog();
        ApplyPostProcess();
    }

    private void ApplySkybox()
    {
        if (!m_changeSkyboxMode && !m_changeTimeOfDay) return;

        SkyboxController sky = FindFirstObjectByType<SkyboxController>();
        if (!sky) return;

        if (m_changeSkyboxMode) sky.mode = m_skyboxMode;
        if (m_changeTimeOfDay)
        {
            sky.timeOfDay = m_timeOfDay;
            if (m_pauseAutoAdvance) sky.autoAdvanceTime = false;
        }
    }

    private void ApplyMusic()
    {
        if (m_musicAction == MusicAction.None) return;
        if (!AudioPlaylist.Instance) return;

        switch (m_musicAction)
        {
            case MusicAction.PlayOverride:
                if (m_overrideClip) AudioPlaylist.Instance.PlayOverride(m_overrideClip, 10);
                break;
            case MusicAction.ReturnToPlaylist:
                AudioPlaylist.Instance.ReturnToPlaylist();
                break;
        }
    }

    private void ApplyRollingLog()
    {
        if (!m_changeRollingLog) return;

        RollingLogManager rl = FindFirstObjectByType<RollingLogManager>();
        if (!rl) return;

        if (m_rollingLogRoutine != null) StopCoroutine(m_rollingLogRoutine);
        m_rollingLogRoutine = StartCoroutine(LerpRollingLog(rl, m_rollingLogAmount, m_rollingLogTransitionDuration));
    }

    private IEnumerator LerpRollingLog(RollingLogManager rl, float target, float duration)
    {
        float start = rl.Amount;
        if (duration <= 0f)
        {
            rl.Amount = target;
            m_rollingLogRoutine = null;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            k = k * k * (3f - 2f * k);
            rl.Amount = Mathf.Lerp(start, target, k);
            yield return null;
        }
        rl.Amount = target;
        m_rollingLogRoutine = null;
    }

    private void ApplyPostProcess()
    {
        if (m_postProcessAction == ToggleAction.None) return;

        PostProcessController pp = m_postProcessController
            ? m_postProcessController
            : FindFirstObjectByType<PostProcessController>(FindObjectsInactive.Include);
        if (!pp) return;

        pp.enabled = m_postProcessAction == ToggleAction.Enable;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = m_gizmoColor;
        Gizmos.DrawSphere(transform.position, m_radius);
        Gizmos.color = new Color(m_gizmoColor.r, m_gizmoColor.g, m_gizmoColor.b, 1f);
        Gizmos.DrawWireSphere(transform.position, m_radius);
    }
#endif
}
