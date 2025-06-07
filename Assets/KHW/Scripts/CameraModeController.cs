using UnityEngine;
using UnityEngine.InputSystem;

// Grip을 눌러 카메라 UI를 켜고 끄는 컨트롤러

public class CameraModeController : MonoBehaviour
{
    // public GameObject cameraModel;
    public GameObject cameraUI; 
    public GameObject cameraModel;

    public InputActionProperty gripAction;   

    public bool IsActive { get; private set; }
    void Start()
    {
        cameraUI.SetActive(false); 
    }


    /* ─────────── Input 바인딩 ─────────── */
    void OnEnable()
    {
        gripAction.action.performed += OnGripPressed;   // 눌렀을 때
        gripAction.action.canceled  += OnGripReleased;  // 뗐을 때
        gripAction.action.Enable();
    }
    void OnDisable()
    {
        gripAction.action.performed -= OnGripPressed;
        gripAction.action.canceled  -= OnGripReleased;
        gripAction.action.Disable();
    }

    /* ─────────── 이벤트 핸들러 ─────────── */
    void OnGripPressed(InputAction.CallbackContext _)
    {
        if (IsActive) return;
        IsActive = true;

        cameraUI.SetActive(true);
        cameraModel.SetActive(false);
        TutorialManager.Instance?.OnRightGrabDone();
    }

    void OnGripReleased(InputAction.CallbackContext _)
    {
        if (!IsActive) return;
        IsActive = false;

        cameraUI.SetActive(false);
        cameraModel.SetActive(true);
    }
}
