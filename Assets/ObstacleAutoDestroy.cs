using UnityEngine;

public class ObstacleAutoDestroy : MonoBehaviour
{
    // Distancia detrás del jugador para destruirse
    public float destroyDistance = 20f;
    private Transform _player;

    void Start()
    {
        // Buscamos el OVRCameraRig como referencia del jugador
        var rig = FindObjectOfType<OVRCameraRig>();
        if (rig != null) _player = rig.transform;
    }

    void Update()
    {
        if (_player == null) return;

        // El RoadSystem se mueve hacia atrás — cuando el obstáculo
        // queda muy atrás en Z mundial, lo destruimos
        if (transform.position.z < _player.position.z - destroyDistance)
        {
            Destroy(gameObject);
        }
    }
}