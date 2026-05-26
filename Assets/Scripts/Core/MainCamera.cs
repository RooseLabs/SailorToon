using UnityEngine;

public class MainCamera : MonoBehaviour {

    private Portal[] m_portals;

    private void Awake()
    {
        m_portals = FindObjectsByType<Portal>(FindObjectsSortMode.None);
    }

    private void OnPreCull()
    {
        foreach (Portal p in m_portals)
            p.PrePortalRender();
        foreach (Portal p in m_portals)
            p.Render();
        foreach (Portal portal in m_portals)
            portal.PostPortalRender();
    }
}
