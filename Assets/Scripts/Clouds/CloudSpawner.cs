using UnityEngine;

public class CloudSpawner : MonoBehaviour
{
    public int numViewDirections = 100;
    public int numClouds = 10;
    public int cloudSpawnSeed;
    public bool randomizeCloudSeed;

    public float spawnRadius = 10;
    [Range(0, 1)] public float startHeight;
    public GameObject cloudPrefab;
    public GameObject cloudCorePrefab;

    private void Start()
    {
        float goldenRatio = (1 + Mathf.Sqrt(5)) / 2;
        float angleIncrement = Mathf.PI * 2 * goldenRatio;

        for (int i = 0; i < numViewDirections; i++)
        {
            float t = (float)i / numViewDirections;
            float inclination = Mathf.Acos(1 - (1 - startHeight) * t);
            float azimuth = angleIncrement * i;

            float x = Mathf.Sin(inclination) * Mathf.Sin(azimuth);
            float y = Mathf.Cos(inclination);
            float z = Mathf.Sin(inclination) * Mathf.Cos(azimuth);

            Instantiate(cloudPrefab, transform.position + new Vector3(x, y, z) * spawnRadius,
                Quaternion.identity, transform);
        }

        if (randomizeCloudSeed) cloudSpawnSeed = Random.Range(-10000, 10000);
        System.Random prng = new(cloudSpawnSeed);

        for (int i = 0; i < numClouds; i++)
        {
            float t = (float)prng.NextDouble();
            float inclination = Mathf.Acos(1 - (1 - startHeight) * t);
            float azimuth = angleIncrement * i;

            float x = Mathf.Sin(inclination) * Mathf.Sin(azimuth);
            float y = Mathf.Cos(inclination);
            float z = Mathf.Sin(inclination) * Mathf.Cos(azimuth);

            Instantiate(cloudCorePrefab, transform.position + new Vector3(x, y, z) * spawnRadius,
                Quaternion.identity, transform);
        }
    }
}
