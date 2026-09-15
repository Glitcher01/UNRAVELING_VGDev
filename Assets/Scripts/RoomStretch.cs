using UnityEngine;


// As the level clock advances into stages 2 and 3, the objects in
// `objectsToStretch` move backward, making the room feel unnaturally long
public class RoomStretch : MonoBehaviour
{
    public Transform[] objectsToStretch;  
    public float stretchPerStage = 4f;    
    public float stretchSpeed = 0.5f;      

    private float[] baseZ; 
    private float targetOffset;   

    // vars needed for walls
    public Transform[] wallsToStretch;
    private float[] wallsBaseZ;
    private float[] wallsBaseScale;
    private float[] wallsBaseLength;          

    void Start()
    {
        // remember where each object started
        baseZ = new float[objectsToStretch.Length];
        for (int i = 0; i < objectsToStretch.Length; i++)
            baseZ[i] = objectsToStretch[i].position.z;
        
        // remember walls start, scaling, and length
        
        wallsBaseZ = new float[wallsToStretch.Length];
        wallsBaseScale = new float[wallsToStretch.Length];
        wallsBaseLength = new float[wallsToStretch.Length];
        for (int i = 0; i < wallsToStretch.Length; i++) {
            Transform walls = wallsToStretch[i];
            wallsBaseZ[i] = walls.position.z;
            wallsBaseScale[i] = walls.localScale.y;
            Renderer r = walls.GetComponent<Renderer>();
            wallsBaseLength[i] = r.bounds.size.z;     
        }

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

        // for stretching wall
        for (int i = 0; i < wallsToStretch.Length; i++) {
            Transform walls = wallsToStretch[i];
            float wallsGoalLength = wallsBaseLength[i] + targetOffset;
            float wallsGoalScale = wallsBaseScale[i] * (wallsGoalLength / wallsBaseLength[i]);
            float wallsGoalPos = wallsBaseZ[i] + targetOffset * 0.5f;

            Vector3 s = walls.localScale;
            s.y = Mathf.Lerp(s.y, wallsGoalScale, Time.deltaTime * stretchSpeed);
            walls.localScale = s;

            Vector3 p_walls = walls.position;
            p_walls.z = Mathf.Lerp(p_walls.z, wallsGoalPos, Time.deltaTime * stretchSpeed);
            walls.position = p_walls;
        }
    }
}