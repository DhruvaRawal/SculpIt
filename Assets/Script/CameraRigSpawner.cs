using UnityEngine;

public class CameraRigSpawner : MonoBehaviour
{
    public Transform spawnPoint; // Reference to the spawn point

    void Start()
    {
        if (spawnPoint != null)
        {
            // Move Camera Rig to Spawn Point
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
        }
        else
        {
            Debug.LogError("Spawn Point not assigned in CameraRigSpawner script!");
        }
    }
}
