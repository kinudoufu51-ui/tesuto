using UnityEngine;

[RequireComponent(typeof(UniversalFPSController))]
public class FPSInteractionSystem : MonoBehaviour
{
    public KeyCode interactKey = KeyCode.E;
    public float interactRange = 2.4f;
    public float assistSphereRadius = 0.08f;
    public LayerMask interactableMask = ~0;

    private UniversalFPSController controller;
    private IFPSInteractable currentTarget;
    private float currentHoldTimer = 0f;
    private bool interactTriggeredThisPress = false;

    private GUIStyle promptStyle;
    private Texture2D barTex;

    void Awake()
    {
        controller = GetComponent<UniversalFPSController>();
        barTex = new Texture2D(1, 1);
        barTex.SetPixel(0, 0, Color.white);
        barTex.Apply();
    }

    void Update()
    {
        if (controller.IsVaulting)
        {
            ResetHoldState();
            currentTarget = null;
            return;
        }

        ScanForInteractable();
        HandleInteractInput(Time.deltaTime);
    }

    private void ScanForInteractable()
    {
        Camera cam = controller.mainCamera;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        IFPSInteractable found = null;

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableMask, QueryTriggerInteraction.Collide))
        {
            found = hit.collider.GetComponentInParent<IFPSInteractable>();
        }

        if (found == null)
        {
            if (Physics.SphereCast(ray, assistSphereRadius, out RaycastHit sphereHit, interactRange, interactableMask, QueryTriggerInteraction.Collide))
            {
                Vector3 dirToHit = (sphereHit.point - ray.origin).normalized;
                if (!Physics.Raycast(ray.origin, dirToHit, sphereHit.distance - 0.05f, controller.environmentMask, QueryTriggerInteraction.Ignore))
                {
                    found = sphereHit.collider.GetComponentInParent<IFPSInteractable>();
                }
            }
        }

        if (found != currentTarget || (found != null && !found.CanInteract(controller)))
        {
            ResetHoldState();
        }

        currentTarget = (found != null && found.CanInteract(controller)) ? found : null;
    }

    private void HandleInteractInput(float dt)
    {
        if (currentTarget == null) return;

        float holdDuration = currentTarget.GetHoldDuration();
        if (holdDuration <= 0.01f)
        {
            if (Input.GetKeyDown(interactKey)) ExecuteInteraction(false);
        }
        else
        {
            if (Input.GetKey(interactKey) && !interactTriggeredThisPress)
            {
                currentHoldTimer += dt;
                if (currentHoldTimer >= holdDuration)
                {
                    ExecuteInteraction(true);
                    interactTriggeredThisPress = true;
                    currentHoldTimer = 0f;
                }
            }
            else if (Input.GetKeyUp(interactKey))
            {
                ResetHoldState();
            }
        }
    }

    private void ExecuteInteraction(bool isHeavyHold)
    {
        controller.PlayInteractHandFeedback(isHeavyHold);
        currentTarget.OnInteract(controller);
    }

    private void ResetHoldState()
    {
        currentHoldTimer = 0f;
        interactTriggeredThisPress = false;
    }

    void OnGUI()
    {
        if (currentTarget == null) return;

        if (promptStyle == null)
        {
            promptStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                richText = true
            };
            promptStyle.normal.textColor = Color.white;
        }

        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f + 42f;
        float holdReq = currentTarget.GetHoldDuration();
        string actionType = holdReq > 0.01f ? $"長押し {holdReq:F1}s" : "押す";
        string text = $"<color=#FFDD44>[{interactKey}]</color> {currentTarget.GetInteractionPrompt()} <size=11><color=#CCCCCC>({actionType})</color></size>";

        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.Label(new Rect(cx - 200f + 1f, cy + 1f, 400f, 28f), currentTarget.GetInteractionPrompt(), promptStyle);
        GUI.color = Color.white;
        GUI.Label(new Rect(cx - 200f, cy, 400f, 28f), text, promptStyle);

        if (holdReq > 0.01f && currentHoldTimer > 0f)
        {
            float ratio = Mathf.Clamp01(currentHoldTimer / holdReq);
            GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            GUI.DrawTexture(new Rect(cx - 75f, cy + 28f, 150f, 8f), barTex);
            GUI.color = new Color(1.0f, 0.8f, 0.2f, 0.95f);
            GUI.DrawTexture(new Rect(cx - 73f, cy + 30f, 146f * ratio, 4f), barTex);
            GUI.color = Color.white;
        }
    }
}
