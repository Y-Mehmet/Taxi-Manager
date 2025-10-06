using UnityEngine;

/// <summary>
/// Bu script, parent'taki PassengerGroup'un moveDirection değişkenine göre
/// kendi rotasyonunu günceller.
/// Yönler:
/// - (0, 1) -> Yukarı -> 0 derece (ileri)
/// - (1, 0) -> Sağ -> 90 derece
/// - (-1, 0) -> Sol -> -90 derece
/// - (0, -1) -> Aşağı -> 180 derece
/// </summary>
public class ChildDirectionRotator : MonoBehaviour
{
    // Parent'taki ana script'e referans
    private PassengerGroup parentGroup;

    void Start()
    {
        // Script'in bağlı olduğu objenin parent'ından PassengerGroup component'ini bul ve al.
        parentGroup = GetComponentInParent<PassengerGroup>();

        // Eğer parent'ta PassengerGroup script'i bulunamazsa, hata mesajı göster ve script'i devre dışı bırak.
        if (parentGroup == null)
        {
            Debug.LogError("Bu objenin parent'ında 'PassengerGroup' script'i bulunamadı!", this);
            this.enabled = false; // Hata tekrarını önlemek için script'i kapat.
        }
        else
        {
            UpdateRotationBasedOnParentDirection();
        }
    }

   

    private void UpdateRotationBasedOnParentDirection()
    {
        // Parent'ın mevcut hareket yönünü al.
        Vector2Int direction = parentGroup.moveDirection;

        float targetYRotation = 0f; // Varsayılan rotasyon (yukarı yönü için)

        // Gelen yöne göre hedef Y rotasyonunu belirle.
        if (direction == Vector2Int.up) // (0, 1) ise Yön: Yukarı
        {
            targetYRotation = 0f;
        }
        else if (direction == Vector2Int.right) // (1, 0) ise Yön: Sağ
        {
            targetYRotation = 90f;
        }
        else if (direction == Vector2Int.left) // (-1, 0) ise Yön: Sol
        {
            targetYRotation = -90f;
        }
        else if (direction == Vector2Int.down) // (0, -1) ise Yön: Aşağı
        {
            targetYRotation = 180f;
        }

        // Hesaplanmış olan hedef rotasyonu objeye uygula.
        // Quaternion.Euler, derece cinsinden açıları bir Quaternion rotasyonuna çevirir.
        transform.rotation = Quaternion.Euler(90, targetYRotation, 0);
    }
}