using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StopSlotTextUpdater : MonoBehaviour
{
    private PassengerGroup passengerGroup;
    public TMP_Text slotText;

    void Start()
    {
        // Başlangıçta passengerGroup atanmadıysa, UI'yı temizle
        if (slotText != null)
            slotText.text = "";
    }

    void OnDestroy()
    {
        SetPassengerGroup(null);
    }

    public void SetPassengerGroup(PassengerGroup newGroup)
    {

        if (passengerGroup != null)
        {
            passengerGroup.OnAvailableSlotsChanged -= HandleGroupSizeChanged;
            passengerGroup.OnGroupSizeDecreased -= HandleGroupSizeChanged;
        }

        passengerGroup = newGroup;

        if (passengerGroup != null && slotText != null)
        {
            passengerGroup.OnAvailableSlotsChanged += HandleGroupSizeChanged;
            passengerGroup.OnGroupSizeDecreased += HandleGroupSizeChanged;
            UpdateSlotText(passengerGroup.GroupSize);
        }
        else if (slotText != null)
        {
            slotText.text = "";
        }
    }

    void HandleGroupSizeChanged(int _)
    {
        if (passengerGroup != null && slotText != null)
            UpdateSlotText(passengerGroup.GroupSize);
    }

    void UpdateSlotText(int groupSize)
    {
        slotText.text = groupSize.ToString();
    }
}
