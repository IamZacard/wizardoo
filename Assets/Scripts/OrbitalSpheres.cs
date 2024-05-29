using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitalSpheres : MonoBehaviour
{
    public GameObject spherePrefab; // Префаб сфери
    public float radius = 0.5f; // Радіус обертання
    public float speed = 1.0f; // Швидкість обертання
    public float noiseIntensity = 0.1f; // Інтенсивність шуму в русі
    private List<GameObject> spheres;
    private int[] sortingOrders = { 2, 3, 4, 5 };
    private float orderChangeRate = 2.0f; // Швидкість зміни порядку в шарі
    private float nextOrderChangeTime = 0.0f;
    private Color[] colors = { Color.yellow, new Color(1.0f, 0.65f, 0.0f), Color.magenta }; // Жовтий, оранжевий, фіолетовий
    private float colorChangeRate = 1.0f; // Швидкість зміни кольору
    private float nextColorChangeTime = 0.0f;

    void Start()
    {
        spheres = new List<GameObject>();
        for (int i = 0; i < 3; i++)
        {
            GameObject sphere = Instantiate(spherePrefab, transform);
            sphere.transform.localPosition = new Vector3(
                Mathf.Cos(i * 2 * Mathf.PI / 3) * radius,
                Mathf.Sin(i * 2 * Mathf.PI / 3) * radius,
                0);
            sphere.GetComponent<SpriteRenderer>().color = colors[i % colors.Length];
            spheres.Add(sphere);
        }
    }

    void Update()
    {
        float angle = speed * Time.time;
        for (int i = 0; i < spheres.Count; i++)
        {
            float currentAngle = angle + i * 2 * Mathf.PI / spheres.Count;
            float noiseX = Mathf.PerlinNoise(Time.time + i, 0) * noiseIntensity;
            float noiseY = Mathf.PerlinNoise(0, Time.time + i) * noiseIntensity;
            spheres[i].transform.localPosition = new Vector3(
                Mathf.Cos(currentAngle) * radius + noiseX,
                Mathf.Sin(currentAngle) * radius + noiseY,
                0);
        }

        if (Time.time >= nextOrderChangeTime)
        {
            nextOrderChangeTime = Time.time + orderChangeRate;
            ChangeSortingOrder();
        }

        if (Time.time >= nextColorChangeTime)
        {
            nextColorChangeTime = Time.time + colorChangeRate;
            ChangeColors();
        }
    }

    void ChangeSortingOrder()
    {
        foreach (GameObject sphere in spheres)
        {
            SpriteRenderer sr = sphere.GetComponent<SpriteRenderer>();
            sr.sortingOrder = sortingOrders[Random.Range(0, sortingOrders.Length)];
        }
    }

    void ChangeColors()
    {
        foreach (GameObject sphere in spheres)
        {
            SpriteRenderer sr = sphere.GetComponent<SpriteRenderer>();
            int currentColorIndex = System.Array.IndexOf(colors, sr.color);
            int nextColorIndex = (currentColorIndex + 1) % colors.Length;
            sr.color = colors[nextColorIndex];
        }
    }
}