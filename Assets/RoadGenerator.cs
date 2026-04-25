using System.Collections.Generic;
using UnityEngine;

public class RoadGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject roadSegmentPrefab;
    public GameObject[] obstaclePrefabs;

    [Header("Configuración")]
    public float segmentLength = 30f;
    public int segmentsAhead = 6;
    public int segmentsBehind = 2;

    [Header("Obstáculos")]
    [Range(0f, 1f)]
    public float obstacleChance = 0.3f;
    public float minObstacleDistance = 60f;
    // NUEVO: Control de altura desde el Inspector
    public float obstacleYOffset = 0f;

    private Queue<GameObject> _activeSegments = new Queue<GameObject>();

    // Cuántos segmentos hemos spawneado en total — de aquí calculamos posición
    private int _totalSegmentsSpawned = 0;
    private float _lastObstacleAtSegment = -10;

    void Start()
    {
        // Nos aseguramos de estar en Y=0
        transform.position = Vector3.zero;

        for (int i = 0; i < segmentsAhead + segmentsBehind; i++)
        {
            SpawnSegment();
        }
    }

    void Update()
    {
        // Cuánto se ha desplazado la carretera hacia atrás
        float distanceTravelled = Mathf.Abs(transform.position.z);

        // En qué "segmento" está el jugador ahora
        float playerSegment = distanceTravelled / segmentLength;

        // Generamos segmentos hasta tener suficientes adelante
        while (_totalSegmentsSpawned < playerSegment + segmentsAhead)
        {
            SpawnSegment();
        }

        // Eliminamos los que quedaron muy atrás
        while (_activeSegments.Count > segmentsAhead + segmentsBehind)
        {
            GameObject old = _activeSegments.Dequeue();
            Destroy(old);
        }
    }

    void SpawnSegment()
    {
        // La posición es siempre LOCAL al RoadSystem
        // Así aunque el parent se mueva, los hijos se posicionan bien entre sí
        float localZ = _totalSegmentsSpawned * segmentLength;
        Vector3 localPos = new Vector3(0f, 0f, localZ);

        GameObject seg = Instantiate(roadSegmentPrefab, Vector3.zero, Quaternion.identity, transform);
        seg.transform.localPosition = localPos;

        _activeSegments.Enqueue(seg);

        // Obstáculos
        bool farEnough = (_totalSegmentsSpawned - _lastObstacleAtSegment) * segmentLength > minObstacleDistance;
        bool notTooEarly = _totalSegmentsSpawned > 3;

        if (notTooEarly && farEnough && Random.value < obstacleChance && obstaclePrefabs.Length > 0)
        {
            SpawnObstacle(seg.transform);
            _lastObstacleAtSegment = _totalSegmentsSpawned;
        }

        _totalSegmentsSpawned++;
    }

    void SpawnObstacle(Transform parentSegment)
    {
        GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
        float[] lanes = { -3.5f, 0f, 3.5f };
        float laneX = lanes[Random.Range(0, lanes.Length)];

        // CORRECCIÓN: Usamos obstacleYOffset en lugar de 1.5f
        Vector3 worldPos = parentSegment.position + new Vector3(laneX, obstacleYOffset, segmentLength * 0.5f);

        // Lo instanciamos sin padre — así no hereda escala distorsionada
        GameObject obstacle = Instantiate(prefab, worldPos, Quaternion.identity);

        // Lo hacemos hijo del RoadSystem (no del segmento) para que se mueva con la carretera
        obstacle.transform.SetParent(transform);
    }
}