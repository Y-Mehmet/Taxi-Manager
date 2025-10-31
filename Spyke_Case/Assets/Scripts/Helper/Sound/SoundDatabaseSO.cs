using UnityEngine;

// Bu s�n�f, bir ses klibini onun enum t�r�yle e�le�tirmek i�in kullan�l�r.
[System.Serializable]
public class Sound
{
    public SoundType soundType;
    public AudioClip audioClip;
    [Range(0f, 1f)]
    public float volume = .25f; // Sesin varsay�lan ses seviyesi
    public bool loop = false; // Sesin d�ng�de �al�n�p �al�nmayaca��

}

// Bu attribute, Unity men�s�ne yeni bir asset olu�turma se�ene�i ekler.
[CreateAssetMenu(fileName = "SoundDatabase", menuName = "ScriptableObjects/Sound Database")]
public class SoundDatabaseSO : ScriptableObject
{
    public Sound[] sounds;
    public Sound[] correctSounds;
}