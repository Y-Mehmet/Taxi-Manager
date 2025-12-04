using System.Collections;
using UnityEngine;
using TMPro; // TextMeshPro kullanmak için bu satır gerekli
// Eğer eski UI Text kullanıyorsanız -> using UnityEngine.UI;

public class TypewriterHelper : MonoBehaviour
{
    public static TypewriterHelper Instance { get; private set; }
    private void Awake()
    {
        // Singleton deseni ile bu sınıfın tek örneğini oluştur
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Bu nesneyi sahneler arasında taşımak için
        }
        else
        {
            Destroy(gameObject); // Eğer zaten bir örnek varsa, bu yeni örneği yok et
        }
    }
    private Coroutine typingCoroutine;

    
    public Coroutine Run(string textToType, TextMeshProUGUI textLabel, float typingSpeed = 0.05f)
    {
        // Eğer zaten çalışan bir yazma işlemi varsa, onu durdur.
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // Yeni yazma işlemini başlat ve referansını sakla.
        typingCoroutine = StartCoroutine(TypeText(textToType, textLabel, typingSpeed));
        return typingCoroutine;
    }

    private IEnumerator TypeText(string textToType, TextMeshProUGUI textLabel, float typingSpeed)
    {
        // Metin kutusunu temizleyerek başla
        textLabel.text = "";

        // Her harf için döngüye gir
        foreach (char letter in textToType)
        {
            textLabel.text += letter; // Bir sonraki harfi ekle
            yield return new WaitForSeconds(typingSpeed); // Belirtilen süre kadar bekle
        }

        // Yazma işlemi bittiğinde coroutine referansını temizle
        typingCoroutine = null;
    }
    public void CompleteTyping(string textToType, TextMeshProUGUI textLabel)
    {
        // Eğer bir yazma işlemi varsa ve henüz bitmediyse
        if (typingCoroutine != null)
        {
            // Coroutine'i hemen durdur
            StopCoroutine(typingCoroutine);

            // Metin kutusuna tüm metni anında yaz
            textLabel.text = textToType;

            // Coroutine referansını temizle
            typingCoroutine = null;
        }
    }
}
