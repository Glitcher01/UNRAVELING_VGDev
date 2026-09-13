using UnityEngine;

// As the level clock advances into stages 2 and 3, the objects in
// `objectsToStretch` move backward, making the room feel unnaturally long
public class RoomStretch : MonoBehaviour
{
    public Transform[] objectsToStretch;  

    public float stretchPerStage = 3f;    
    public float stretchSpeed = 0.5f;      

    private float[] baseZ; 
    private float targetOffset;             

    void Start()
    {
        // remember where each object started
        baseZ = new float[objectsToStretch.Length];
        for (int i = 0; i < objectsToStretch.Length; i++)
            baseZ[i] = objectsToStretch[i].position.z;

        targetOffset = 0f;

        LevelClock.onStageChange += OnStageChange;
    }

    void OnDisable()
    {
        LevelClock.onStageChange -= OnStageChange;
    }

    void OnStageChange(int stage)
    {
        if (stage == 2) targetOffset = stretchPerStage;
        if (stage == 3) targetOffset = stretchPerStage * 2f;
    }

    void Update()
    {
        for (int i = 0; i < objectsToStretch.Length; i++)
        {
            Vector3 p = objectsToStretch[i].position;
            float goalZ = baseZ[i] + targetOffset;   // each object's own base + the shared offset
            p.z = Mathf.Lerp(p.z, goalZ, Time.deltaTime * stretchSpeed);
            objectsToStretch[i].position = p;
        }
    }
}