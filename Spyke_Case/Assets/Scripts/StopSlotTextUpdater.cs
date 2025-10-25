using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StopSlotTextUpdater : MonoBehaviour
{
    private PassengerGroup passengerGroup;
    public TMP_Text slotText;

    void Start()
    {
        if (slotText != null)
            slotText.text = "";
    }

    void OnDestroy()
    {
        SetPassengerGroup(null);
    }

    public void SetPassengerGroup(PassengerGroup newGroup)
    {
        // If we were tracking an old group, unsubscribe from its events.
        if (passengerGroup != null)
        {
            Debug.Log($"[StopSlotTextUpdater] Unsubscribing from {passengerGroup.name}");
            passengerGroup.OnCapacityChanged -= UpdateSlotText;
        }

        passengerGroup = newGroup;

        // If a new group is assigned, subscribe to its event and update the text immediately.
        if (passengerGroup != null)
        {
            Debug.Log($"[StopSlotTextUpdater] SetPassengerGroup: Now tracking {passengerGroup.name}. Initial capacity: {passengerGroup.GroupSize}");
            passengerGroup.OnCapacityChanged += UpdateSlotText;
            // Perform an initial update with the current capacity.
            UpdateSlotText(passengerGroup.GroupSize);
        }
        else if (slotText != null)
        {
            // If the group is cleared (e.g., it departed), clear the text.
            Debug.Log("[StopSlotTextUpdater] SetPassengerGroup: Passenger group cleared.");
            slotText.text = "";
        }
    }

    // This method now correctly uses the value passed by the event.
    void UpdateSlotText(int remainingCapacity)
    {
        if (passengerGroup != null && slotText != null)
        {
            // Log for debugging to see when updates happen and with what value.
            Debug.LogWarning($"[StopSlotTextUpdater] UpdateSlotText received for {passengerGroup.name}. New capacity: {remainingCapacity}");
            
            if (remainingCapacity > 0)
            {
                slotText.text = remainingCapacity.ToString();
            }
            else
            {
                // When capacity is 0 or less, clear the text.
                slotText.text = "";
            }
        }
        else if (slotText != null)
        {
            Debug.LogWarning("[StopSlotTextUpdater] UpdateSlotText called but passenger group is null. Clearing text.");
            slotText.text = "";
        }
    }
}