using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapFollowUser : MonoBehaviour
{
    [SerializeField] private Transform user;
    public float blinkSpeed = 2f;

    // Minimum and maximum alpha values
    public float minAlpha = 0f;
    public float maxAlpha = 1f;

    private Image image;
    private Color originalColor;

    void Start()
    {
        image = GetComponent<Image>();
        originalColor = image.color;
    }
    void Update()
    {
        Vector3 newPosition = user.position;
        newPosition.y = transform.position.y;
        transform.position = newPosition;
        float alpha = Mathf.PingPong(Time.time * blinkSpeed, maxAlpha - minAlpha) + minAlpha;

        Color newColor = originalColor;
        newColor.a = alpha;
        image.color = newColor;
    }
}
