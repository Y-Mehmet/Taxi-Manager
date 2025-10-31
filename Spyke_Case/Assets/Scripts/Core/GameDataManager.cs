using UnityEngine;
using System;

/// <summary>
/// Oyun verilerini yöneten, kaydetme ve yükleme operasyonlarını koordine eden merkezi yönetici.
/// Singleton deseni kullanır.
/// </summary>
public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }

    private SaveGameData saveData;
    private const string SAVE_FILE_NAME = "savegame.json";

    // Diğer yöneticilerin veri yüklendiğinde güncellenmesi için olay.
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
    /// Oyunu yükler. Kayıt dosyası yoksa yeni bir tane oluşturur.
    /// </summary>
    public void LoadGame()
    {
        saveData = SaveSystem.Load();

        if (saveData == null)
        {
            Debug.Log("No save data found, creating new game data.");
            saveData = new SaveGameData();
        }

        // Veri yüklendikten sonra olayları tetikle.
        OnDataLoaded?.Invoke(saveData);
    }

    /// <summary>
    /// Oyunu kaydeder. Tüm yöneticilerden güncel verileri toplar.
    /// </summary>
    public void SaveGame()
    {
        if (saveData == null)
        {
            Debug.LogWarning("SaveData is null. Cannot save game.");
            return;
        }

        // Diğer yöneticilerden verileri topla
        // Bu yöneticilerin sahnede aktif ve erişilebilir olduğu varsayılır.
       ResourceManager.Instance?.SaveData(saveData);
       AbilityManager.Instance?.SaveData(saveData);
       MetroManager.Instance?.SaveData(saveData);
        // Diğer yöneticiler için de benzer çağrılar eklenebilir.

        SaveSystem.Save(saveData);
    }

    /// <summary>
    /// Diğer scriptlerin başlangıçta veri alabilmesi için kullanılır.
    /// </summary>
    public SaveGameData GetSaveData()
    {
        return saveData;
    }
}