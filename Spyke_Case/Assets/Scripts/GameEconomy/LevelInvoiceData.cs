using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Level sonu fatura verilerini tutan sÄ±nÄ±f
/// Hesaplama SÄ±rasÄ±: Gelir - Giderler - Joker AvantajlarÄ± = Ara Toplam â†’ Vergi â†’ Net
/// </summary>
[System.Serializable]
public class LevelInvoiceData
{
    // Gelirler (Income)
    public int completedPassengers = 0;  // Tamamlanan passenger sayÄ±sÄ±
    public int passengerEarnings = 0;    // completedPassengers * 20

    // Giderler (Expenses)
    public int crashCount = 0;           // Kaza sayÄ±sÄ±
    public int crashPenalty = 0;         // Joker'e gÃ¶re hesaplanÄ±r
    
    public int uberPickupCount = 0;      // Uber yolcu alÄ±m sayÄ±sÄ±
    public int uberPenalty = 0;          // uberPickupCount * 100

    // Vergi (Tax) - Ara toplamdan hesaplanÄ±r
    public float taxRate = 0.20f;        // Default 20% tax
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
    /// SIRA: 1. Gelir, 2. Giderler (crash, uber), 3. Joker avantajlarÄ±, 4. Ara toplam, 5. Vergi
    /// </summary>
    public int CalculateTotalExpenses()
    {
        // 1. Gelir hesapla
        int income = CalculateTotalIncome();

        // 2. Giderler - Crash Penalty (joker'e gÃ¶re)
        int baseCrashPenalty = crashCount * 500;
        if (JokerSystem.Instance != null)
        {
            crashPenalty = JokerSystem.Instance.GetCrashPenalty(baseCrashPenalty);
        }
        else
        {
            crashPenalty = baseCrashPenalty;
        }
        
        // 3. Giderler - Uber Penalty
        uberPenalty = uberPickupCount * 100;

        // 4. Ara Toplam (Gelir - Giderler)
        int subtotal = income - crashPenalty - uberPenalty;

        // 5. Vergi hesapla (Ara toplamdan)
        // Get tax rate from JokerSystem
        if (JokerSystem.Instance != null)
        {
            taxRate = JokerSystem.Instance.GetTaxRate();
        }

        // Vergi sadece pozitif ara toplamdan alÄ±nÄ±r
        if (subtotal > 0)
        {
            taxAmount = Mathf.RoundToInt(subtotal * taxRate);
        }
        else
        {
            taxAmount = 0; // Zarar varsa vergi yok
        }

        return crashPenalty + uberPenalty + taxAmount;
    }

    /// <summary>
    /// Net kazancÄ± hesapla (gelir - gider)
    /// </summary>
    public int CalculateNetEarnings()
    {
        int income = CalculateTotalIncome();
        int expenses = CalculateTotalExpenses();
        return income - expenses;
    }

    /// <summary>
    /// Passenger tamamlandÄ±ÄŸÄ±nda Ã§aÄŸrÄ±lÄ±r
    /// </summary>
    public void OnPassengerCompleted()
    {
        completedPassengers++;
        Debug.Log($"[Invoice] Passenger completed. Total: {completedPassengers}");
    }

    /// <summary>
    /// Kaza olduÄŸunda Ã§aÄŸrÄ±lÄ±r
    /// </summary>
    public void OnCrashOccurred()
    {
        crashCount++;
        Debug.Log($"[Invoice] Crash occurred. Total crashes: {crashCount}");
    }

    /// <summary>
    /// Uber yolcu aldÄ±ÄŸÄ±nda Ã§aÄŸrÄ±lÄ±r
    /// </summary>
    public void OnUberPickup()
    {
        uberPickupCount++;
        Debug.Log($"[Invoice] Uber pickup. Total pickups: {uberPickupCount}");
    }

    /// <summary>
    /// Fatura detaylarÄ±nÄ± debug log'a yazdÄ±r
    /// </summary>
    public void PrintInvoice()
    {
        Debug.Log("========== LEVEL INVOICE ==========");
        Debug.Log($"<color=green>INCOME:</color>");
        Debug.Log($"  Passenger Income: {completedPassengers} x 20 = +{passengerEarnings} coins");
        
        Debug.Log($"<color=red>EXPENSES:</color>");
        if (crashCount > 0)
        {
            int basePenalty = crashCount * 500;
            if (crashPenalty == 0)
                Debug.Log($"  Crash Penalty: {crashCount} x 500 = -{basePenalty} coins (COVERED BY INSURANCE)");
            else if (crashPenalty == 100 * crashCount)
                Debug.Log($"  Crash Penalty: {crashCount} x 100 = -{crashPenalty} coins (OWN REPAIR STATION)");
            else
                Debug.Log($"  Crash Penalty: {crashCount} x 500 = -{crashPenalty} coins");
        }
        if (uberPickupCount > 0)
            Debug.Log($"  Uber Penalty: {uberPickupCount} x 100 = -{uberPenalty} coins");
        
        // Ara toplam
        int subtotal = passengerEarnings - crashPenalty - uberPenalty;
        Debug.Log($"<color=yellow>SUBTOTAL (before tax): {subtotal} coins</color>");
        
        // Vergi
        if (taxAmount > 0)
            Debug.Log($"  Tax ({taxRate * 100}%): -{taxAmount} coins");
        else
            Debug.Log($"  Tax: 0 coins (Tax Joker Active or No Profit)");
        
        Debug.Log($"<color=yellow>NET EARNINGS: {CalculateNetEarnings()} coins</color>");
        Debug.Log("===================================");
    }
}
