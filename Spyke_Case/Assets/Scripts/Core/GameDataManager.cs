using UnityEngine;
using System;

/// <summary>
/// Oyun verilerini yÃ¶neten, kaydetme ve yÃ¼kleme operasyonlarÄ±nÄ± koordine eden merkezi yÃ¶netici.
/// Singleton deseni kullanÄ±r.
/// </summary>
public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }

    private SaveGameData saveData;
    private const string SAVE_FILE_NAME = "savegame.json";

    // DiÄŸer yÃ¶neticilerin veri yÃ¼klendiÄŸinde gÃ¼ncellenmesi iÃ§in olay.
    public event Action<SaveGameData> OnDataLoaded;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveGame();
        }
    }

    /// <summary>
    /// Oyunu yÃ¼kler. KayÄ±t dosyasÄ± yoksa yeni bir tane oluÅŸturur.
    /// </summary>
    public void LoadGame()
    {
        saveData = SaveSystem.Load();

        if (saveData == null)
        {
            /* Debug.Log("No save data found, creating new game data."); */
            saveData = new SaveGameData();
        }

        // Veri yÃ¼klendikten sonra olaylarÄ± tetikle.
        OnDataLoaded?.Invoke(saveData);
    }

    /// <summary>
    /// Oyunu kaydeder. TÃ¼m yÃ¶neticilerden gÃ¼ncel verileri toplar.
    /// </summary>
    public void SaveGame()
    {
        if (saveData == null)
        {
//             Debug.LogWarning("SaveData is null. Cannot save game.");
            return;
        }

        // DiÄŸer yÃ¶neticilerden verileri topla
        // Bu yÃ¶neticilerin sahnede aktif ve eriÅŸilebilir olduÄŸu varsayÄ±lÄ±r.
       ResourceManager.Instance?.SaveData(saveData);
       AbilityManager.Instance?.SaveData(saveData);

        // DiÄŸer yÃ¶neticiler iÃ§in de benzer Ã§aÄŸrÄ±lar eklenebilir.

        SaveSystem.Save(saveData);
    }

    /// <summary>
    /// DiÄŸer scriptlerin baÅŸlangÄ±Ã§ta veri alabilmesi iÃ§in kullanÄ±lÄ±r.
    /// </summary>
    public SaveGameData GetSaveData()
    {
        return saveData;
    }
}
