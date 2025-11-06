using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    public Light flickerLight;
    public float minIntensity = 0.5f;
    public float maxIntensity = 1.5f;
    public float flickerSpeed = 0.1f;

    private float targetIntensity;

    void Start()
    {
        if (!flickerLight) flickerLight = GetComponent<Light>();
        targetIntensity = flickerLight.intensity;
        StartCoroutine(Flicker());
    }

    System.Collections.IEnumerator Flicker()
    {
        while (true)
        {
            targetIntensity = Random.Range(minIntensity, maxIntensity);
            flickerLight.intensity = Mathf.Lerp(flickerLight.intensity, targetIntensity, flickerSpeed);
            yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));
        }
    }
}