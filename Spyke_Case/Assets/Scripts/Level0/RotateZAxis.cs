using UnityEngine;

public class RotateZAxis : MonoBehaviour
{
    // Dönüş hızı (derece/saniye)
    public float rotationSpeed = 100f;
    
    // Rotation aktif mi?
    private bool isRotating = false;
    
    /// <summary>
    /// Rotation'ı başlat
    /// </summary>
    public void StartRotation()
    {
        isRotating = true;
    }
    
    /// <summary>
    /// Rotation'ı durdur
    /// </summary>
    public void StopRotation()
    {
        isRotating = false;
    }
    
    /// <summary>
    /// Rotation durumunu değiştir
    /// </summary>
    public void SetRotating(bool rotating)
    {
        isRotating = rotating;
    }
    
    // Update is called once per frame
    void Update()
    {
        if (isRotating)
        {
            // GameObject'in transform bileşenini Z ekseni etrafında döndürür.
            // Time.deltaTime, dönüşün kare hızından bağımsız olmasını sağlar.
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }
    }
}
