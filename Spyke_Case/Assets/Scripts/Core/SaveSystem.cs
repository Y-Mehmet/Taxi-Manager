using UnityEngine;
using System.IO;

/// <summary>
/// Kaydetme ve yükleme işlemlerini dosya sistemi seviyesinde yöneten statik sınıf.
/// SaveGameData nesnesini JSON formatına çevirip diske yazar ve okur.
/// </summary>
public static class SaveSystem
{
    private static readonly string SAVE_FILE_NAME = "savegame.json";

    /// <summary>
    /// Verilen SaveGameData nesnesini JSON olarak diske kaydeder.
    /// </summary>
    /// <param name="data">Kaydedilecek veri nesnesi.</param>
    public static void Save(SaveGameData data)
    {
        string path = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        try
        {
            string json = JsonUtility.ToJson(data, true); // 'true' for pretty print
            File.WriteAllText(path, json);
            Debug.Log($"<color=green>Game saved successfully to:</color> {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save data to {path}. Error: {e.Message}");
        }
    }

    /// <summary>
    /// Kayıt dosyasını diskten okur ve SaveGameData nesnesine dönüştürür.
    /// </summary>
    /// <returns>Yüklenen veri nesnesi. Dosya yoksa null döner.</returns>
    public static SaveGameData Load()
    {
        string path = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                SaveGameData data = JsonUtility.FromJson<SaveGameData>(json);
                Debug.Log($"<color=cyan>Game loaded successfully from:</color> {path}");
                return data;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load data from {path}. Error: {e.Message}");
                return null;
            }
        }
        else
        {
            Debug.LogWarning("Save file not found. A new game will be started.");
            return null;
        }
    }
}
