using UnityEngine;

public class CloudBehaviour : MonoBehaviour
{
    public Vector2 rotSpeedMinMax = new(10, 20);

    private float m_rotSpeed;

    private CloudCore[] m_cloudCenters;
    private Transform m_myTransform;

    private void Start()
    {
        m_rotSpeed = Random.Range(rotSpeedMinMax.x, rotSpeedMinMax.y);
        m_cloudCenters = FindObjectsByType<CloudCore>(FindObjectsSortMode.None);
        m_myTransform = transform;
    }

    private void Update()
    {
        m_myTransform.RotateAround(transform.parent.position, Vector3.up, Time.deltaTime * m_rotSpeed);
        float maxScale = 0;
        foreach (CloudCore cloudCenter in m_cloudCenters)
        {
            Vector3 offset = m_myTransform.position - cloudCenter.transform.position;
            float sqrDstHorizontal = offset.x * offset.x + offset.z * offset.z;
            float sqrDstVertical = offset.y * offset.y;
            float tH = 1 - Mathf.Min(1,
                sqrDstHorizontal / (cloudCenter.falloffDstHorizontal * cloudCenter.falloffDstHorizontal));
            float tV = 1 - Mathf.Min(1, sqrDstVertical / (cloudCenter.falloffVertical * cloudCenter.falloffVertical));
            //float t = 1 - Mathf.Min (1, sqrDst / (falloffDst * falloffDst));
            maxScale = Mathf.Max(maxScale, tV * tH * cloudCenter.maxScale);
        }

        m_myTransform.localScale = Vector3.one * maxScale;
    }
}
