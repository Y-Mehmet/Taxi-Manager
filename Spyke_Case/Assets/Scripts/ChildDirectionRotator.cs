using UnityEngine;

/// <summary>
/// Bu script, parent'taki PassengerGroup'un moveDirection deÄŸiÅŸkenine gÃ¶re
/// kendi rotasyonunu gÃ¼nceller.
/// YÃ¶nler:
/// - (0, 1) -> YukarÄ± -> 0 derece (ileri)
/// - (1, 0) -> SaÄŸ -> 90 derece
/// - (-1, 0) -> Sol -> -90 derece
/// - (0, -1) -> AÅŸaÄŸÄ± -> 180 derece
/// </summary>
public class ChildDirectionRotator : MonoBehaviour
{
    // Parent'taki ana script'e referans
    private PassengerGroup parentGroup;

    void Start()
    {
        // Script'in baÄŸlÄ± olduÄŸu objenin parent'Ä±ndan PassengerGroup component'ini bul ve al.
        parentGroup = GetComponentInParent<PassengerGroup>();

        // EÄŸer parent'ta PassengerGroup script'i bulunamazsa, hata mesajÄ± gÃ¶ster ve script'i devre dÄ±ÅŸÄ± bÄ±rak.
        if (parentGroup == null)
        {
            Debug.LogError("Bu objenin parent'Ä±nda 'PassengerGroup' script'i bulunamadÄ±!", this);
            this.enabled = false; // Hata tekrarÄ±nÄ± Ã¶nlemek iÃ§in script'i kapat.
        }
        else
        {
            UpdateRotationBasedOnParentDirection();
        }
    }

   

    private void UpdateRotationBasedOnParentDirection()
    {
        // Parent'Ä±n mevcut hareket yÃ¶nÃ¼nÃ¼ al.
        Vector2Int direction = parentGroup.moveDirection;

        float targetYRotation = 0f; // VarsayÄ±lan rotasyon (yukarÄ± yÃ¶nÃ¼ iÃ§in)

        // Gelen yÃ¶ne gÃ¶re hedef Y rotasyonunu belirle.
        if (direction == Vector2Int.up) // (0, 1) ise YÃ¶n: YukarÄ±
        {
            targetYRotation = 0f;
        }
        else if (direction == Vector2Int.right) // (1, 0) ise YÃ¶n: SaÄŸ
        {
            targetYRotation = 90f;
        }
        else if (direction == Vector2Int.left) // (-1, 0) ise YÃ¶n: Sol
        {
            targetYRotation = -90f;
        }
        else if (direction == Vector2Int.down) // (0, -1) ise YÃ¶n: AÅŸaÄŸÄ±
        {
            targetYRotation = 180f;
        }

        // HesaplanmÄ±ÅŸ olan hedef rotasyonu objeye uygula.
        // Quaternion.Euler, derece cinsinden aÃ§Ä±larÄ± bir Quaternion rotasyonuna Ã§evirir.
        transform.rotation = Quaternion.Euler(0, targetYRotation, 0);
    }
}
