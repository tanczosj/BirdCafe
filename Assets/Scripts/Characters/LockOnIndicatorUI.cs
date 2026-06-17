using UnityEngine;

public class LockOnIndicatorUI : MonoBehaviour
{
    [SerializeField] private BeanLockOn lockOn;
    [SerializeField] private BeanCameraRig cameraRig;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform indicator;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.25f, 0f);
    [SerializeField] private float spinSpeed = 180f;

    private RectTransform canvasRect;
    private float currentAngle;

    private void Awake()
    {
        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (lockOn == null || cameraRig == null || indicator == null || canvasRect == null)
            return;

        if (!lockOn.IsLockedOn || lockOn.CurrentTarget == null)
        {
            if (indicator.gameObject.activeSelf)
                indicator.gameObject.SetActive(false);
            return;
        }

        Vector3 screenPoint = cameraRig.PlayerCamera.WorldToScreenPoint(lockOn.CurrentTarget.AimPosition + worldOffset);
        if (screenPoint.z <= 0f)
        {
            if (indicator.gameObject.activeSelf)
                indicator.gameObject.SetActive(false);
            return;
        }

        if (!indicator.gameObject.activeSelf)
            indicator.gameObject.SetActive(true);

        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out Vector2 localPoint);
        indicator.anchoredPosition = localPoint;

        currentAngle -= spinSpeed * Time.deltaTime;
        indicator.localRotation = Quaternion.Euler(0f, 0f, currentAngle);
    }
}