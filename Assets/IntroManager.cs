using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class IntroVideoManager : MonoBehaviour
{
    [SerializeField] private string mainSceneName = "Boat";
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private bool allowSkip = true;

    private VideoPlayer videoPlayer;
    private bool isTransitioning = false;

    private void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
    }

    private void Start()
    {
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.Play();
    }

    private void Update()
    {
        if (allowSkip && !isTransitioning)
        {
            if (Input.anyKeyDown || Input.GetButtonDown("Fire1"))
            {
                StartCoroutine(TransitionRoutine());
            }
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        if (isTransitioning) yield break;
        isTransitioning = true;

        videoPlayer.loopPointReached -= OnVideoFinished;

        // Fade to black if VRCameraFade is present on Main Camera
        if (VRCameraFade.Instance != null)
        {
            yield return VRCameraFade.Instance.Routine_FadeOut(fadeDuration);
        }
        else
        {
            yield return new WaitForSeconds(fadeDuration);
        }

        SceneManager.LoadScene(mainSceneName);
    }
}