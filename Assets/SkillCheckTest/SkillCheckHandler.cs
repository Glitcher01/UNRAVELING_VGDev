using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

public class SkillCheckHandler : MonoBehaviour
{

    [Header("State")]
    [SerializeField] protected Vector2 canvasPosition;
    [SerializeField] protected GameObject background;
    [SerializeField] protected GameObject zone;
    [SerializeField] protected GameObject spinner;
    [SerializeField] protected float spinnerSpeed;
    [SerializeField] protected float radiusOffSet;

    RectTransform rt;
    // Measured in radians, from 0 to 2pi.Also assumes that the radius is zoneRT's width / 2.
    float startingPosition = 0;

    private void Start()
    {
        rt = GetComponent<RectTransform>();
        RectTransform backgroundRT = background.GetComponent<RectTransform>();
        RectTransform zoneRT = zone.GetComponent<RectTransform>();
        RectTransform spinnerRT = spinner.GetComponent<RectTransform>();



        if (rt == null) {
            return;
        }

        rt.anchoredPosition =  canvasPosition;

        

        if (backgroundRT == null || zoneRT == null || spinnerRT == null) {
            return;
        }

        backgroundRT.anchoredPosition = Vector2.zero;
        zoneRT.anchoredPosition = Vector2.zero;
    }

    private void Update()
    {
        RectTransform spinnerRT = spinner.GetComponent<RectTransform>();
        RectTransform zoneRT = zone.GetComponent<RectTransform>();

        float radius = zoneRT.rect.width / 2 + radiusOffSet;
        spinnerRT.anchoredPosition = radius * new Vector2(Mathf.Cos(startingPosition), Mathf.Sin(startingPosition));
        spinnerRT.rotation = Quaternion.Euler(0, 0, (float)(startingPosition * 180 / Math.PI));
        startingPosition += spinnerSpeed * Time.deltaTime;
        startingPosition %= Mathf.PI * 2;
    }
}
