using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class PhotoCaptureAndJudge : MonoBehaviour
{
    public TutorialMission tutorialMission;
    public bool useTutorial = true;
    public CanvasGroup flashCanvasGroup;
    public float flashDuration = 0.2f;
    public RawImage photoDisplay;
    public float photoDisplayDuration = 5f;
    public GameObject panel;
    public GameObject CameraFrame;
    public RenderTexture captureRT;
    

    [Header("Input")]
    public InputActionProperty triggerButton;   // 오른손 트리거

    [Header("Camera / Distance")]
    public Camera captureCam;                   // 카메라(대개 MainCamera)
    public float maxJudgeDistance = 5f;
    public LayerMask TargetLayer;          // PhotoTarget 레이어만 체크

    [Header("Save")]
    public bool savePhotoToFile = true;
    public string saveFolder = "Photos";

    void Start()
    {
        if (savePhotoToFile)
        {
            string path = Path.Combine(Application.persistentDataPath, saveFolder);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            saveFolder = path;   // 퀘스트 빌드 대비
        }
    }

    void PlayShutterEffect()
    {
        // 사운드
        //if (shutterAudioSrc != null && shutterClip != null)
        //{
        //    shutterAudioSrc.PlayOneShot(shutterClip);
        //}

        // 플래시 코루틴 실행
        if (flashCanvasGroup != null)
        {
            StartCoroutine(FlashRoutine());
        }
    }

    IEnumerator FlashRoutine()
    {
        float fadeInDuration = flashDuration * 0.3f;
        float holdDuration = flashDuration * 0.2f;
        float fadeOutDuration = flashDuration * 0.5f;

        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            flashCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeInDuration);
            yield return null;
        }
        flashCanvasGroup.alpha = 1f;

        yield return new WaitForSeconds(holdDuration);

        t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            flashCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeOutDuration);
            yield return null;
        }
        flashCanvasGroup.alpha = 0f;
    }

    void OnEnable() => triggerButton.action.Enable();
    void OnDisable() => triggerButton.action.Disable();

    void Update()
    {
        if (triggerButton.action.WasPressedThisFrame())
        {
            PlayShutterEffect();
            StartCoroutine(CaptureAndShowPhoto());       
            int count = JudgeMultipleTargets();
            Debug.Log($"현재 프레임 + 거리 통과 타겟 개수: {count}");

            if (useTutorial &&
               TutorialManager.Instance != null &&
               TutorialManager.Instance.Current == TutorialManager.Step.TakePhoto)
            {
                TutorialManager.Instance.OnTutorialPhotoTaken();
            }
        }
    }

    /* ----------------------- 화면에 사진 띄우기 ----------------------- */
    IEnumerator CaptureAndShowPhoto()
    {

        // yield return new WaitForEndOfFrame();

        // int cropSize = Mathf.Min(Screen.width, Screen.height); // 정사각형 crop
        // int startX = (Screen.width - cropSize) / 2;
        // int startY = (Screen.height - cropSize) / 2;

        // Texture2D screenTex = new Texture2D(cropSize, cropSize, TextureFormat.RGB24, false);
        // screenTex.ReadPixels(new Rect(startX, startY, cropSize, cropSize), 0, 0);
        // screenTex.Apply();
        captureCam.enabled = true;
        captureCam.targetTexture = captureRT;
        captureCam.Render();

        RenderTexture.active = captureRT;
        Texture2D tex = new Texture2D(captureRT.width, captureRT.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, captureRT.width, captureRT.height), 0, 0);
        tex.Apply();

        captureCam.enabled = false;
        RenderTexture.active = null;


        if (savePhotoToFile)
        {
            string file = Path.Combine(saveFolder,
                $"Photo_{System.DateTime.Now:yyyyMMdd_HHmmss}.png");
            File.WriteAllBytes(file, tex.EncodeToPNG());
            Debug.Log($"사진 저장: {file}");
        }

        // RawImage 활성화 + 표시
        if (photoDisplay != null)
        {
            photoDisplay.texture = tex;
            photoDisplay.gameObject.SetActive(true);
            panel.gameObject.SetActive(true);

            yield return new WaitForSeconds(photoDisplayDuration);

            // RawImage 비활성화
            photoDisplay.gameObject.SetActive(false);
            panel.gameObject.SetActive(false);

            // 메모리 해제
            Destroy(tex);
        }
    }

    /* ---------------- 다중 타겟 판정 ---------------- */
    int JudgeMultipleTargets()
    {
        Vector3 camPos = captureCam.transform.position;
        Collider[] hits = Physics.OverlapSphere(camPos, maxJudgeDistance, TargetLayer);

        int visibleCount = 0;
        foreach (Collider col in hits)
        {
            if (!col.CompareTag("MissionTarget")) continue; 

            if (IsInView(col.transform))
            {
                visibleCount++;
#if UNITY_EDITOR
                Debug.Log($"뷰포트 + 거리 통과: {col.name}");
#endif
            }
        }

        return visibleCount; // 프레임 + 거리 통과한 개수만 반환
    }

    // 카메라 뷰 안(0~1) + 정면(Z>0)인지 검사
    bool IsInView(Transform target)
    {
        Vector3 vPos = captureCam.WorldToViewportPoint(target.position);
        return vPos.z > 0f && vPos.x is >= 0f and <= 1f && vPos.y is >= 0f and <= 1f;
    }

    // 카메라 → 타겟 라인에 가로막는 물체가 없는지 검사
    // bool IsVisible(Vector3 camPos, Collider targetCol)
    // {
    //     Vector3 targetCenter = targetCol.bounds.center;
    //     Vector3 dir = targetCenter - camPos;

    //     // Linecast로 가장 먼저 맞는 콜라이더가 본인인가?
    //     return !Physics.Linecast(camPos, targetCenter, out RaycastHit hit, ~0)
    //         || hit.collider == targetCol;
    // }
}