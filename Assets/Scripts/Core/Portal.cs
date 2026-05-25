using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    [Header("Main Settings")] public Portal linkedPortal;
    public MeshRenderer screen;
    public int recursionLimit = 5;

    [Header("Advanced Settings")] public float nearClipOffset = 0.05f;
    public float nearClipLimit = 0.2f;

    // Private variables
    private RenderTexture m_viewTexture;
    private Camera m_portalCam;
    private Camera m_playerCam;
    private Material m_firstRecursionMat;
    private List<PortalTraveller> m_trackedTravellers;
    private MeshFilter m_screenMeshFilter;

    private static readonly int DisplayMask = Shader.PropertyToID("displayMask");
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    private static readonly int SliceCenter = Shader.PropertyToID("_SliceCenter");
    private static readonly int SliceNormal = Shader.PropertyToID("_SliceNormal");
    private static readonly int SliceOffsetDst = Shader.PropertyToID("_SliceOffsetDst");

    private void Awake()
    {
        m_playerCam = Camera.main;
        m_portalCam = GetComponentInChildren<Camera>();
        m_portalCam.enabled = false;
        m_trackedTravellers = new List<PortalTraveller>();
        m_screenMeshFilter = screen.GetComponent<MeshFilter>();
        screen.material.SetInt(DisplayMask, 1);
    }

    private void LateUpdate()
    {
        HandleTravellers();
    }

    private void HandleTravellers()
    {
        for (int i = 0; i < m_trackedTravellers.Count; i++)
        {
            PortalTraveller traveller = m_trackedTravellers[i];
            Transform travellerT = traveller.transform;
            Matrix4x4 m = linkedPortal.transform.localToWorldMatrix * transform.worldToLocalMatrix *
                          travellerT.localToWorldMatrix;

            Vector3 offsetFromPortal = travellerT.position - transform.position;
            int portalSide = System.Math.Sign(Vector3.Dot(offsetFromPortal, transform.forward));
            int portalSideOld = System.Math.Sign(Vector3.Dot(traveller.previousOffsetFromPortal, transform.forward));
            // Teleport the traveller if it has crossed from one side of the portal to the other
            if (portalSide != portalSideOld)
            {
                Vector3 positionOld = travellerT.position;
                Quaternion rotOld = travellerT.rotation;
                traveller.Teleport(transform, linkedPortal.transform, m.GetColumn(3), m.rotation);
                traveller.graphicsClone.transform.SetPositionAndRotation(positionOld, rotOld);
                // Can't rely on OnTriggerEnter/Exit to be called next frame since it depends on when FixedUpdate runs
                linkedPortal.OnTravellerEnterPortal(traveller);
                m_trackedTravellers.RemoveAt(i);
                i--;
            }
            else
            {
                traveller.graphicsClone.transform.SetPositionAndRotation(m.GetColumn(3), m.rotation);
                //UpdateSliceParams (traveller);
                traveller.previousOffsetFromPortal = offsetFromPortal;
            }
        }
    }

    // Called before any portal cameras are rendered for the current frame
    public void PrePortalRender()
    {
        foreach (PortalTraveller traveller in m_trackedTravellers) UpdateSliceParams(traveller);
    }

    // Manually render the camera attached to this portal
    // Called after PrePortalRender, and before PostPortalRender
    public void Render()
    {
        // Skip rendering the view from this portal if player is not looking at the linked portal
        if (!CameraUtility.VisibleFromCamera(linkedPortal.screen, m_playerCam)) return;

        CreateViewTexture();

        Matrix4x4 localToWorldMatrix = m_playerCam.transform.localToWorldMatrix;
        var renderPositions = new Vector3[recursionLimit];
        var renderRotations = new Quaternion[recursionLimit];

        int startIndex = 0;
        m_portalCam.projectionMatrix = m_playerCam.projectionMatrix;
        for (int i = 0; i < recursionLimit; i++)
        {
            if (i > 0)
                // No need for recursive rendering if linked portal is not visible through this portal
                if (!CameraUtility.BoundsOverlap(m_screenMeshFilter, linkedPortal.m_screenMeshFilter, m_portalCam))
                    break;

            localToWorldMatrix = transform.localToWorldMatrix * linkedPortal.transform.worldToLocalMatrix *
                                 localToWorldMatrix;
            int renderOrderIndex = recursionLimit - i - 1;
            renderPositions[renderOrderIndex] = localToWorldMatrix.GetColumn(3);
            renderRotations[renderOrderIndex] = localToWorldMatrix.rotation;

            m_portalCam.transform.SetPositionAndRotation(renderPositions[renderOrderIndex],
                renderRotations[renderOrderIndex]);
            startIndex = renderOrderIndex;
        }

        // Hide screen so that camera can see through portal
        screen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        linkedPortal.screen.material.SetInt(DisplayMask, 0);

        for (int i = startIndex; i < recursionLimit; i++)
        {
            m_portalCam.transform.SetPositionAndRotation(renderPositions[i], renderRotations[i]);
            SetNearClipPlane();
            HandleClipping();
            m_portalCam.Render();

            if (i == startIndex) linkedPortal.screen.material.SetInt(DisplayMask, 1);
        }

        // Unhide objects hidden at start of render
        screen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
    }

    private void HandleClipping()
    {
        // There are two main graphical issues when slicing travellers
        // 1. Tiny sliver of mesh drawn on backside of portal
        //    Ideally the oblique clip plane would sort this out, but even with 0 offset, tiny sliver still visible
        // 2. Tiny seam between the sliced mesh, and the rest of the model drawn onto the portal screen
        // This function tries to address these issues by modifying the slice parameters when rendering the view from the portal
        // Would be great if this could be fixed more elegantly, but this is the best I can figure out for now
        const float hideDst = -1000;
        const float showDst = 1000;
        float screenThickness = linkedPortal.ProtectScreenFromClipping(m_portalCam.transform.position);

        foreach (PortalTraveller traveller in m_trackedTravellers)
        {
            if (SameSideOfPortal(traveller.transform.position, PortalCamPos))
                // Addresses issue 1
                traveller.SetSliceOffsetDst(hideDst, false);
            else
                // Addresses issue 2
                traveller.SetSliceOffsetDst(showDst, false);

            // Ensure clone is properly sliced, in case it's visible through this portal:
            int cloneSideOfLinkedPortal = -SideOfPortal(traveller.transform.position);
            bool camSameSideAsClone = linkedPortal.SideOfPortal(PortalCamPos) == cloneSideOfLinkedPortal;
            if (camSameSideAsClone)
                traveller.SetSliceOffsetDst(screenThickness, true);
            else
                traveller.SetSliceOffsetDst(-screenThickness, true);
        }

        Vector3 offsetFromPortalToCam = PortalCamPos - transform.position;
        foreach (PortalTraveller linkedTraveller in linkedPortal.m_trackedTravellers)
        {
            Vector3 travellerPos = linkedTraveller.graphicsObject.transform.position;
            Vector3 clonePos = linkedTraveller.graphicsClone.transform.position;
            // Handle clone of linked portal coming through this portal:
            bool cloneOnSameSideAsCam = linkedPortal.SideOfPortal(travellerPos) != SideOfPortal(PortalCamPos);
            if (cloneOnSameSideAsCam)
                // Addresses issue 1
                linkedTraveller.SetSliceOffsetDst(hideDst, true);
            else
                // Addresses issue 2
                linkedTraveller.SetSliceOffsetDst(showDst, true);

            // Ensure traveller of linked portal is properly sliced, in case it's visible through this portal:
            bool camSameSideAsTraveller =
                linkedPortal.SameSideOfPortal(linkedTraveller.transform.position, PortalCamPos);
            if (camSameSideAsTraveller)
                linkedTraveller.SetSliceOffsetDst(screenThickness, false);
            else
                linkedTraveller.SetSliceOffsetDst(-screenThickness, false);
        }
    }

    // Called once all portals have been rendered, but before the player camera renders
    public void PostPortalRender()
    {
        foreach (PortalTraveller traveller in m_trackedTravellers) UpdateSliceParams(traveller);
        ProtectScreenFromClipping(m_playerCam.transform.position);
    }

    private void CreateViewTexture()
    {
        if (m_viewTexture == null || m_viewTexture.width != Screen.width || m_viewTexture.height != Screen.height)
        {
            if (m_viewTexture != null) m_viewTexture.Release();
            m_viewTexture = new RenderTexture(Screen.width, Screen.height, 0);
            // Render the view from the portal camera to the view texture
            m_portalCam.targetTexture = m_viewTexture;
            // Display the view texture on the screen of the linked portal
            linkedPortal.screen.material.SetTexture(MainTex, m_viewTexture);
        }
    }

    // Sets the thickness of the portal screen so as not to clip with camera near plane when player goes through
    private float ProtectScreenFromClipping(Vector3 viewPoint)
    {
        float halfHeight = m_playerCam.nearClipPlane * Mathf.Tan(m_playerCam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float halfWidth = halfHeight * m_playerCam.aspect;
        float dstToNearClipPlaneCorner = new Vector3(halfWidth, halfHeight, m_playerCam.nearClipPlane).magnitude;
        float screenThickness = dstToNearClipPlaneCorner;

        Transform screenT = screen.transform;
        bool camFacingSameDirAsPortal = Vector3.Dot(transform.forward, transform.position - viewPoint) > 0;
        screenT.localScale = new Vector3(screenT.localScale.x, screenT.localScale.y, screenThickness);
        screenT.localPosition = Vector3.forward * screenThickness * (camFacingSameDirAsPortal ? 0.5f : -0.5f);
        return screenThickness;
    }

    private void UpdateSliceParams(PortalTraveller traveller)
    {
        // Calculate slice normal
        int side = SideOfPortal(traveller.transform.position);
        Vector3 sliceNormal = transform.forward * -side;
        Vector3 cloneSliceNormal = linkedPortal.transform.forward * side;

        // Calculate slice center
        Vector3 slicePos = transform.position;
        Vector3 cloneSlicePos = linkedPortal.transform.position;

        // Adjust slice offset so that when player standing on other side of portal to the object, the slice doesn't clip through
        float sliceOffsetDst = 0;
        float cloneSliceOffsetDst = 0;
        float screenThickness = screen.transform.localScale.z;

        bool playerSameSideAsTraveller = SameSideOfPortal(m_playerCam.transform.position, traveller.transform.position);
        if (!playerSameSideAsTraveller) sliceOffsetDst = -screenThickness;
        bool playerSameSideAsCloneAppearing = side != linkedPortal.SideOfPortal(m_playerCam.transform.position);
        if (!playerSameSideAsCloneAppearing) cloneSliceOffsetDst = -screenThickness;

        // Apply parameters
        for (int i = 0; i < traveller.originalMaterials.Length; i++)
        {
            traveller.originalMaterials[i].SetVector(SliceCenter, slicePos);
            traveller.originalMaterials[i].SetVector(SliceNormal, sliceNormal);
            traveller.originalMaterials[i].SetFloat(SliceOffsetDst, sliceOffsetDst);

            traveller.cloneMaterials[i].SetVector(SliceCenter, cloneSlicePos);
            traveller.cloneMaterials[i].SetVector(SliceNormal, cloneSliceNormal);
            traveller.cloneMaterials[i].SetFloat(SliceOffsetDst, cloneSliceOffsetDst);
        }
    }

    // Use custom projection matrix to align portal camera's near clip plane with the surface of the portal
    // Note that this affects precision of the depth buffer, which can cause issues with effects like screenspace AO
    private void SetNearClipPlane()
    {
        // Learning resource:
        // http://www.terathon.com/lengyel/Lengyel-Oblique.pdf
        Transform clipPlane = transform;
        int dot = System.Math.Sign(Vector3.Dot(clipPlane.forward, transform.position - m_portalCam.transform.position));

        Vector3 camSpacePos = m_portalCam.worldToCameraMatrix.MultiplyPoint(clipPlane.position);
        Vector3 camSpaceNormal = m_portalCam.worldToCameraMatrix.MultiplyVector(clipPlane.forward) * dot;
        float camSpaceDst = -Vector3.Dot(camSpacePos, camSpaceNormal) + nearClipOffset;

        // Don't use oblique clip plane if very close to portal as it seems this can cause some visual artifacts
        if (Mathf.Abs(camSpaceDst) > nearClipLimit)
        {
            Vector4 clipPlaneCameraSpace = new(camSpaceNormal.x, camSpaceNormal.y, camSpaceNormal.z, camSpaceDst);

            // Update projection based on new clip plane
            // Calculate matrix with player cam so that player camera settings (fov, etc) are used
            m_portalCam.projectionMatrix = m_playerCam.CalculateObliqueMatrix(clipPlaneCameraSpace);
        }
        else
        {
            m_portalCam.projectionMatrix = m_playerCam.projectionMatrix;
        }
    }

    private void OnTravellerEnterPortal(PortalTraveller traveller)
    {
        if (!m_trackedTravellers.Contains(traveller))
        {
            traveller.EnterPortalThreshold();
            traveller.previousOffsetFromPortal = traveller.transform.position - transform.position;
            m_trackedTravellers.Add(traveller);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PortalTraveller traveller = other.GetComponent<PortalTraveller>();
        if (traveller) OnTravellerEnterPortal(traveller);
    }

    private void OnTriggerExit(Collider other)
    {
        PortalTraveller traveller = other.GetComponent<PortalTraveller>();
        if (traveller && m_trackedTravellers.Contains(traveller))
        {
            traveller.ExitPortalThreshold();
            m_trackedTravellers.Remove(traveller);
        }
    }

    /*
     ** Some helper/convenience stuff:
     */

    private int SideOfPortal(Vector3 pos)
    {
        return System.Math.Sign(Vector3.Dot(pos - transform.position, transform.forward));
    }

    private bool SameSideOfPortal(Vector3 posA, Vector3 posB)
    {
        return SideOfPortal(posA) == SideOfPortal(posB);
    }

    private Vector3 PortalCamPos => m_portalCam.transform.position;

    private void OnValidate()
    {
        if (linkedPortal != null) linkedPortal.linkedPortal = this;
    }
}
