using UnityEngine;
using System;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public static event Action OnSpeedToggleClicked;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SpeedToggleClicked()
    {
        OnSpeedToggleClicked?.Invoke();
    }
}