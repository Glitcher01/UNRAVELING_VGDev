using UnityEngine;
using System.Collections;

public class TeacherState : MonoBehaviour
{
    public Animator animator;
    public bool isFacingBoard = true;

    [Header("Turning")]
    public float turnDuration = 1.5f;

    [Header("Timing")]
    public float minBoardTime = 3f;
    public float maxBoardTime = 10f;
    public float minClassTime = 2f;
    public float maxClassTime = 6f;

    [Header("Staredown walk")]
    public Transform player;
    public float facePlayerAngle = 90f;
    public float walkSpeed = 1.5f;
    public float stopDistance = 2f;

    private bool levelComplete = false;

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        LevelClock.onStageChange += OnStageChange;
    }

    void OnDisable()
    {
        LevelClock.onStageChange -= OnStageChange;
    }

    void Start()
    {
        StartCoroutine(TurnAround());
    }

    void OnStageChange(int newStage)
    {
        if (newStage == 4)
        {
            levelComplete = true;
            StopAllCoroutines();
            transform.rotation = Quaternion.Euler(0f, facePlayerAngle, 0f);
            animator.SetInteger("State", 4);
            StartCoroutine(WalkToPlayer());
        }
    }

    IEnumerator WalkToPlayer()
    {
        while (true)
        {
            Vector3 target = new Vector3(player.position.x, transform.position.y, player.position.z);
            float dist = Vector3.Distance(transform.position, target);

            if (dist <= stopDistance) break;

            float speed = dist < 11f && dist > 3f ? walkSpeed * 4f : walkSpeed;

            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
        animator.SetInteger("State", 5);
    }

    IEnumerator TurnAround()
    {
        while (!levelComplete)
        {
            animator.SetInteger("State", 0);
            isFacingBoard = true;

            yield return new WaitForSeconds(
                Random.Range(minBoardTime, maxBoardTime)
            );

            if (levelComplete)
                yield break;

            isFacingBoard = false;
            animator.SetInteger("State", 1);

            yield return RotateOver(
                Quaternion.Euler(0f, 270f, 0f)
            );

            if (levelComplete)
                yield break;

            animator.SetInteger("State", 2);

            yield return new WaitForSeconds(
                Random.Range(minClassTime, maxClassTime)
            );

            if (levelComplete)
                yield break;

            animator.SetInteger("State", 3);

            yield return RotateOver(
                Quaternion.Euler(0f, 90f, 0f)
            );
        }
    }

    IEnumerator RotateOver(Quaternion target)
    {
        Quaternion start = transform.rotation;
        float t = 0f;

        while (t < turnDuration)
        {
            t += Time.deltaTime;

            transform.rotation = Quaternion.Slerp(
                start,
                target,
                t / turnDuration
            );

            yield return null;
        }

        transform.rotation = target;
    }
}