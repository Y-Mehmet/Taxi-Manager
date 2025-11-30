# PassengerGroup.cs Tutorial Entegrasyonu

## HandleTap Metoduna Eklenecek Kod

`PassengerGroup.cs` dosyasında `HandleTap` metodunu bulun ve aşağıdaki kodu ekleyin:

### Mevcut Kod (Satır 155-166):
```csharp
if (onConveyorBelt)
{
    TryMoveToWaitingArea();
    return;
}

if (AbilityManager.Instance != null && AbilityManager.Instance.IsAbilityModeActive)
{
    // The tap will be handled by the AbilityManager's subscriber. Do nothing here.
    Debug.Log($"[PassengerGroup] Tap on {name} is being handled by an active ability.");
    return;
}
```

### Yeni Kod (Tutorial kontrolü eklenmiş):
```csharp
if (onConveyorBelt)
{
    TryMoveToWaitingArea();
    return;
}

// Tutorial kontrolü - BU KISMI EKLEYİN
if (TutorialManager.Instance != null && TutorialManager.Instance.IsInputBlocked())
{
    Debug.Log($"[PassengerGroup] Tap on {name} during tutorial - letting tutorial handle it.");
    return;
}
// Tutorial kontrolü sonu

if (AbilityManager.Instance != null && AbilityManager.Instance.IsAbilityModeActive)
{
    // The tap will be handled by the AbilityManager's subscriber. Do nothing here.
    Debug.Log($"[PassengerGroup] Tap on {name} is being handled by an active ability.");
    return;
}
```

## Manuel Ekleme Adımları

1. `Assets/Scripts/PassengerGroup.cs` dosyasını açın
2. `HandleTap` metodunu bulun (yaklaşık satır 151)
3. `if (onConveyorBelt)` bloğundan sonra ve `if (AbilityManager.Instance...)` bloğundan önce yukarıdaki tutorial kontrolünü ekleyin
4. Dosyayı kaydedin

## Alternatif: Tüm HandleTap Metodu

Eğer yukarıdaki adımlar karışık geldiyse, tüm `HandleTap` metodunu aşağıdaki ile değiştirin:

```csharp
private void HandleTap(PassengerGroup tappedGroup)
{
    if (tappedGroup != this) return;

    if (onConveyorBelt)
    {
        TryMoveToWaitingArea();
        return;
    }

    // Tutorial kontrolü
    if (TutorialManager.Instance != null && TutorialManager.Instance.IsInputBlocked())
    {
        Debug.Log($"[PassengerGroup] Tap on {name} during tutorial - letting tutorial handle it.");
        return;
    }

    if (AbilityManager.Instance != null && AbilityManager.Instance.IsAbilityModeActive)
    {
        // The tap will be handled by the AbilityManager's subscriber. Do nothing here.
        Debug.Log($"[PassengerGroup] Tap on {name} is being handled by an active ability.");
        return;
    }

    // If the passenger is already at a stop, do not allow it to move again.
    if (StopManager.Instance != null && StopManager.Instance.GetOccupiedStops().ContainsValue(this))
    {
        Debug.Log($"[PassengerGroup] Tap on {name} ignored because it is already at a stop.");
        return;
    }

    Debug.Log($"[PassengerGroup] Tap detected on {name} via event, initiating normal move.");
    OnGroupClicked?.Invoke();
    TryMoveForwardWithLog();
}
```
