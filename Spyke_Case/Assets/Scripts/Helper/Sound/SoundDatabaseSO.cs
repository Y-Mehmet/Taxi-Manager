using UnityEngine;

// Bu sï¿½nï¿½f, bir ses klibini onun enum tï¿½rï¿½yle eï¿½leï¿½tirmek iï¿½in kullanï¿½lï¿½r.
[System.Serializable]
public class Sound
{
    public SoundType soundType;
    public AudioClip audioClip;
    [Range(0f, 1f)]
    public float volume = .25f; // Sesin varsayï¿½lan ses seviyesi
    public bool loop = false; // Sesin dï¿½ngï¿½de ï¿½alï¿½nï¿½p ï¿½alï¿½nmayacaï¿½ï¿½

}

// Bu attribute, Unity menï¿½sï¿½ne yeni bir asset oluï¿½turma seï¿½eneï¿½i ekler.
[CreateAssetMenu(fileName = "SoundDatabase", menuName = "ScriptableObjects/Sound Database")]
public class SoundDatabaseSO : ScriptableObject
{
    public Sound[] sounds;
    public Sound[] correctSounds;
}
