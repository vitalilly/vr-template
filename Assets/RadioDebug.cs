using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RadioDebugPlay : MonoBehaviour
{
    [SerializeField] private AudioClip testClip;

    void Start()
    {
        AudioSource audio = GetComponent<AudioSource>();
        
        if (testClip == null)
        {
            Debug.LogError("[RadioDebug] No AudioClip assigned in Inspector!", this);
            return;
        }

        Debug.Log($"[RadioDebug] Playing clip: {testClip.name} | Length: {testClip.length}s");
        audio.clip = testClip;
        audio.Play();
    }
}