using UnityEngine;

public class CloudCore : MonoBehaviour
{
    public float falloffDstHorizontal = 3;
    public float falloffVertical = 1.5f;
    public float maxScale = 1;

    public Vector2 rotSpeedMinMax = new(10, 20);

    private float m_rotSpeed;

    [HideInInspector] public Transform myTransform;

    private void Start()
    {
        m_rotSpeed = Random.Range(rotSpeedMinMax.x, rotSpeedMinMax.y);
        myTransform = transform;
    }

    private void Update()
    {
        myTransform.RotateAround(transform.parent.position, Vector3.up, Time.deltaTime * m_rotSpeed);
    }
}
