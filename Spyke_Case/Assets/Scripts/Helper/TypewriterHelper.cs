using System.Collections;
using UnityEngine;
using TMPro; // TextMeshPro kullanmak i�in bu sat�r gerekli
// E�er eski UI Text kullan�yorsan�z -> using UnityEngine.UI;

public class TypewriterHelper : MonoBehaviour
{
    public static TypewriterHelper Instance { get; private set; }
    private void Awake()
    {
        // Singleton deseni ile bu s�n�f�n tek �rne�ini olu�tur
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Bu nesneyi sahneler aras�nda ta��mak i�in
        }
        else
        {
            Destroy(gameObject); // E�er zaten bir �rnek varsa, bu yeni �rne�i yok et
        }
    }
    private Coroutine typingCoroutine;

    
    public Coroutine Run(string textToType, TextMeshProUGUI textLabel, float typingSpeed = 0.05f)
    {
        // E�er zaten �al��an bir yazma i�lemi varsa, onu durdur.
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // Yeni yazma i�lemini ba�lat ve referans�n� sakla.
        typingCoroutine = StartCoroutine(TypeText(textToType, textLabel, typingSpeed));
        return typingCoroutine;
    }

    private IEnumerator TypeText(string textToType, TextMeshProUGUI textLabel, float typingSpeed)
    {
        // Metin kutusunu temizleyerek ba�la
        textLabel.text = "";

        // Her harf i�in d�ng�ye gir
        foreach (char letter in textToType)
        {
            textLabel.text += letter; // Bir sonraki harfi ekle
            yield return new WaitForSeconds(typingSpeed); // Belirtilen s�re kadar bekle
        }

        // Yazma i�lemi bitti�inde coroutine referans�n� temizle
        typingCoroutine = null;
    }
    public void CompleteTyping(string textToType, TextMeshProUGUI textLabel)
    {
        // E�er bir yazma i�lemi varsa ve hen�z bitmediyse
        if (typingCoroutine != null)
        {
            // Coroutine'i hemen durdur
            StopCoroutine(typingCoroutine);

            // Metin kutusuna t�m metni an�nda yaz
            textLabel.text = textToType;

            // Coroutine referans�n� temizle
            typingCoroutine = null;
        }
    }
}