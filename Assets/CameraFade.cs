using System.Collections;
using UnityEngine;

public class VRCameraFade : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private Color fadeColor = Color.black;
    [SerializeField] private float defaultFadeDuration = 1.0f;
    [SerializeField] private bool fadeInOnStart = true;

    private Material fadeMaterial;
    private float currentAlpha = 0f;

    public static VRCameraFade Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Create an unlit transparent material dynamically for the lens overlay
        Shader unlitShader = Shader.Find("Unlit/Transparent");
        if (unlitShader != null)
        {
            fadeMaterial = new Material(unlitShader);
        }
    }

    private void Start()
    {
        if (fadeInOnStart)
        {
            currentAlpha = 1f;
            FadeIn(defaultFadeDuration);
        }
    }

    public void FadeIn(float duration)
    {
        StartCoroutine(Routine_Fade(1f, 0f, duration));
    }

    public void FadeOut(float duration)
    {
        StartCoroutine(Routine_Fade(0f, 1f, duration));
    }

    public IEnumerator Routine_FadeOut(float duration)
    {
        yield return StartCoroutine(Routine_Fade(currentAlpha, 1f, duration));
    }

    private IEnumerator Routine_Fade(float startAlpha, float targetAlpha, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
            yield return null;
        }

        currentAlpha = targetAlpha;
    }

    private void OnPostRender()
    {
        if (currentAlpha <= 0f || fadeMaterial == null) return;

        // Render full-screen quad right across the camera lens projection
        fadeMaterial.SetPass(0);
        GL.PushMatrix();
        GL.LoadOrtho();
        GL.Begin(GL.QUADS);
        
        Color renderColor = fadeColor;
        renderColor.a = currentAlpha;
        GL.Color(renderColor);

        GL.Vertex3(0, 0, -12);
        GL.Vertex3(0, 1, -12);
        GL.Vertex3(1, 1, -12);
        GL.Vertex3(1, 0, -12);

        GL.End();
        GL.PopMatrix();
    }
}