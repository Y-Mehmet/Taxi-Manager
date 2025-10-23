using System.Collections;
using UnityEngine;
using TMPro; // TextMeshPro kullanmak için bu satýr gerekli
// Eðer eski UI Text kullanýyorsanýz -> using UnityEngine.UI;

public class TypewriterHelper : MonoBehaviour
{
    public static TypewriterHelper Instance { get; private set; }
    private void Awake()
    {
        // Singleton deseni ile bu sýnýfýn tek örneðini oluþtur
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Bu nesneyi sahneler arasýnda taþýmak için
        }
        else
        {
            Destroy(gameObject); // Eðer zaten bir örnek varsa, bu yeni örneði yok et
        }
    }
    private Coroutine typingCoroutine;

    
    public Coroutine Run(string textToType, TextMeshProUGUI textLabel, float typingSpeed = 0.05f)
    {
        // Eðer zaten çalýþan bir yazma iþlemi varsa, onu durdur.
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // Yeni yazma iþlemini baþlat ve referansýný sakla.
        typingCoroutine = StartCoroutine(TypeText(textToType, textLabel, typingSpeed));
        return typingCoroutine;
    }

    private IEnumerator TypeText(string textToType, TextMeshProUGUI textLabel, float typingSpeed)
    {
        // Metin kutusunu temizleyerek baþla
        textLabel.text = "";

        // Her harf için döngüye gir
        foreach (char letter in textToType)
        {
            textLabel.text += letter; // Bir sonraki harfi ekle
            yield return new WaitForSeconds(typingSpeed); // Belirtilen süre kadar bekle
        }

        // Yazma iþlemi bittiðinde coroutine referansýný temizle
        typingCoroutine = null;
    }
    public void CompleteTyping(string textToType, TextMeshProUGUI textLabel)
    {
        // Eðer bir yazma iþlemi varsa ve henüz bitmediyse
        if (typingCoroutine != null)
        {
            // Coroutine'i hemen durdur
            StopCoroutine(typingCoroutine);

            // Metin kutusuna tüm metni anýnda yaz
            textLabel.text = textToType;

            // Coroutine referansýný temizle
            typingCoroutine = null;
        }
    }
}