using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Level sonu fatura verilerini tutan sınıf
/// </summary>
[System.Serializable]
public class LevelInvoiceData
{
    // Gelirler (Income)
    public int completedPassengers = 0;  // Tamamlanan passenger sayısı
    public int passengerEarnings = 0;    // completedPassengers * 20

    // Giderler (Expenses) - Temp Resource'tan kesilir
    public int crashCount = 0;           // Kaza sayısı
    public int crashPenalty = 0;         // Calculated based on jokers
    
    public int uberPickupCount = 0;      // Uber yolcu alım sayısı
    public int uberPenalty = 0;          // uberPickupCount * 100

    // Vergi
    public float taxRate = 0.10f;        // Default 10% tax
    public int taxAmount = 0;

    /// <summary>
    /// Toplam geliri hesapla
    /// </summary>
    public int CalculateTotalIncome()
    {
        passengerEarnings = completedPassengers * 20;
        return passengerEarnings;
    }

    /// <summary>
    /// Toplam gideri hesapla (cezalar + vergi)
    /// </summary>
    public int CalculateTotalExpenses()
    {
        // Get tax rate from JokerSystem
        if (JokerSystem.Instance != null)
        {
            taxRate = JokerSystem.Instance.GetTaxRate();
        }

        // Calculate crash penalty based on active jokers
        int baseCrashPenalty = crashCount * 500;
        if (JokerSystem.Instance != null)
        {
            crashPenalty = JokerSystem.Instance.GetCrashPenalty(baseCrashPenalty);
        }
        else
        {
            crashPenalty = baseCrashPenalty;
        }
        
        // Uber cezası
        uberPenalty = uberPickupCount * 100;

        // Vergi hesapla
        int grossIncome = passengerEarnings;
        taxAmount = Mathf.RoundToInt(grossIncome * taxRate);

        return crashPenalty + uberPenalty + taxAmount;
    }

    /// <summary>
    /// Net kazancı hesapla (gelir - gider)
    /// </summary>
    public int CalculateNetEarnings()
    {
        int income = CalculateTotalIncome();
        int expenses = CalculateTotalExpenses();
        return income - expenses;
    }

    /// <summary>
    /// Passenger tamamlandığında çağrılır
    /// </summary>
    public void OnPassengerCompleted()
    {
        completedPassengers++;
        Debug.Log($"[Invoice] Passenger completed. Total: {completedPassengers}");
    }

    /// <summary>
    /// Kaza olduğunda çağrılır
    /// </summary>
    public void OnCrashOccurred()
    {
        crashCount++;
        Debug.Log($"[Invoice] Crash occurred. Total crashes: {crashCount}");
    }

    /// <summary>
    /// Uber yolcu aldığında çağrılır
    /// </summary>
    public void OnUberPickup()
    {
        uberPickupCount++;
        Debug.Log($"[Invoice] Uber pickup. Total pickups: {uberPickupCount}");
    }

    /// <summary>
    /// Fatura detaylarını debug log'a yazdır
    /// </summary>
    public void PrintInvoice()
    {
        Debug.Log("========== LEVEL INVOICE ==========");
        Debug.Log($"<color=green>INCOME:</color>");
        Debug.Log($"  Completed Passengers: {completedPassengers} x 20 = +{passengerEarnings} coins");
        Debug.Log($"  Total Income: +{CalculateTotalIncome()} coins");
        
        Debug.Log($"<color=red>EXPENSES:</color>");
        if (crashCount > 0)
        {
            int basePenalty = crashCount * 500;
            if (crashPenalty == 0)
                Debug.Log($"  Crashes: {crashCount} x 500 = -{basePenalty} coins (COVERED BY INSURANCE)");
            else if (crashPenalty == 100 * crashCount)
                Debug.Log($"  Crash Repair: {crashCount} x 100 = -{crashPenalty} coins (OWN REPAIR STATION)");
            else
                Debug.Log($"  Crash Penalty: {crashCount} x 500 = -{crashPenalty} coins");
        }
        if (uberPickupCount > 0)
            Debug.Log($"  Uber Penalty: {uberPickupCount} x 100 = -{uberPenalty} coins");
        if (taxAmount > 0)
            Debug.Log($"  Tax ({taxRate * 100}%): -{taxAmount} coins");
        else
            Debug.Log($"  Tax: 0 coins (Tax Joker Active)");
        Debug.Log($"  Total Expenses: -{CalculateTotalExpenses()} coins");
        
        Debug.Log($"<color=yellow>NET EARNINGS: {CalculateNetEarnings()} coins</color>");
        Debug.Log("===================================");
    }
}
