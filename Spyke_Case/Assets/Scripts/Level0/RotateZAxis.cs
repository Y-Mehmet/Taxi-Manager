using UnityEngine;

public class RotateZAxis : MonoBehaviour
{
    // Dönüş hızı (derece/saniye)
    public float rotationSpeed = 100f; 

    // Update is called once per frame
    void Update()
    {
        // GameObject'in transform bileşenini Z ekseni etrafında döndürür.
        // Time.deltaTime, dönüşün kare hızından bağımsız olmasını sağlar.
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }
}