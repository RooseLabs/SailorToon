using System.Linq;
using UnityEngine;

public class PortalTraveler : MonoBehaviour
{
    private static readonly int SliceNormal = Shader.PropertyToID("_SliceNormal");
    private static readonly int SliceOffsetDst = Shader.PropertyToID("_SliceOffsetDst");

    public GameObject graphicsObject;
    public GameObject graphicsClone { get; private set; }
    public Vector3 previousOffsetFromPortal { get; set; }

    public Material[] originalMaterials { get; private set; }
    public Material[] cloneMaterials { get; private set; }

    public virtual void Teleport(Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot)
    {
        transform.position = pos;
        transform.rotation = rot;
    }

    // Called when first touches portal
    public virtual void EnterPortalThreshold()
    {
        if (!graphicsClone)
        {
            graphicsClone = Instantiate(graphicsObject, graphicsObject.transform.parent, true);
            graphicsClone.transform.localScale = graphicsObject.transform.localScale;
            originalMaterials = GetMaterials(graphicsObject);
            cloneMaterials = GetMaterials(graphicsClone);
        }
        else
        {
            graphicsClone.SetActive(true);
        }
    }

    // Called once no longer touching portal (excluding when teleporting)
    public virtual void ExitPortalThreshold()
    {
        graphicsClone.SetActive(false);
        // Disable slicing
        foreach (Material material in originalMaterials)
            material.SetVector(SliceNormal, Vector3.zero);
    }

    public void SetSliceOffsetDst(float dst, bool clone)
    {
        for (int i = 0; i < originalMaterials.Length; i++)
            if (clone)
                cloneMaterials[i].SetFloat(SliceOffsetDst, dst);
            else
                originalMaterials[i].SetFloat(SliceOffsetDst, dst);
    }

    private Material[] GetMaterials(GameObject g)
    {
        var renderers = g.GetComponentsInChildren<MeshRenderer>();
        return renderers.SelectMany(r => r.materials).ToArray();
    }
}
