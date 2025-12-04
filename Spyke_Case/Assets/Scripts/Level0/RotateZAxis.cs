using UnityEngine;

public class RotateZAxis : MonoBehaviour
{
    // DÃ¶nÃ¼ÅŸ hÄ±zÄ± (derece/saniye)
    public float rotationSpeed = 100f; 

    // Update is called once per frame
    void Update()
    {
        // GameObject'in transform bileÅŸenini Z ekseni etrafÄ±nda dÃ¶ndÃ¼rÃ¼r.
        // Time.deltaTime, dÃ¶nÃ¼ÅŸÃ¼n kare hÄ±zÄ±ndan baÄŸÄ±msÄ±z olmasÄ±nÄ± saÄŸlar.
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }
}
