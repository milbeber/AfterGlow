using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Yarn.Unity;

/// <summary>
/// Raycast по кликам мыши через New Input System (день 16).
/// Повесьте на один объект в сцене исследования (например, "ExplorationController").
/// Работает только в состоянии GameState.EXPLORATION и сам переключает
/// DIALOGUE <-> EXPLORATION при старте/окончании любого диалога Yarn Spinner.
/// </summary>
public class InteractionRaycaster : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private LayerMask interactiveLayers = ~0;

    private InputAction clickAction;
    private InteractiveObject hovered;
    private DialogueExtensions dialogueExtensions;
    private Coroutine pendingInteract;
    private static readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    /// Сердечки видны только в свободном осмотре — после выбора «Осмотреться», пока не пошёл следующий диалог.
    public static bool HeartsVisible { get; private set; }

    private bool CanExplore()
    {
        if (dialogueExtensions != null && dialogueExtensions.IsBacklogOpen) return false;
        if (dialogueRunner != null && dialogueRunner.IsDialogueRunning) return false;
        if (GameManager.Instance == null) return true; // сцена запущена отдельно (тест в редакторе)
        return GameManager.Instance.CurrentState == GameState.EXPLORATION;
    }

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (dialogueRunner == null) dialogueRunner = FindFirstObjectByType<DialogueRunner>();
        if (dialogueExtensions == null) dialogueExtensions = FindFirstObjectByType<DialogueExtensions>();

        // Действие создаётся в коде, чтобы не зависеть от вашего Input Actions asset.
        clickAction = new InputAction("ExploreClick", InputActionType.Button, "<Pointer>/press");
        clickAction.performed += OnClick;
    }

    private void OnEnable()
    {
        clickAction.Enable();
        if (dialogueRunner != null)
        {
            dialogueRunner.onDialogueStart.AddListener(HandleDialogueStart);
            dialogueRunner.onDialogueComplete.AddListener(HandleDialogueComplete);
        }
    }

    private void OnDisable()
    {
        clickAction.Disable();
        if (dialogueRunner != null)
        {
            dialogueRunner.onDialogueStart.RemoveListener(HandleDialogueStart);
            dialogueRunner.onDialogueComplete.RemoveListener(HandleDialogueComplete);
        }
        HeartsVisible = false;
        ClearHover();
        if (pendingInteract != null)
        {
            StopCoroutine(pendingInteract);
            pendingInteract = null;
        }
    }

    private void OnDestroy() => clickAction.performed -= OnClick;

    // --- Синхронизация с GameManager ---
    private void HandleDialogueStart()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.CurrentState == GameState.EXPLORATION)
            gm.SetState(GameState.DIALOGUE);
    }

    private void HandleDialogueComplete()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.CurrentState == GameState.DIALOGUE)
            gm.SetState(GameState.EXPLORATION);
    }

    // --- Ввод ---
    private void Update()
    {
        HeartsVisible = CanExplore();
        if (!HeartsVisible || IsPointerOverUI()) { ClearHover(); return; }

        var target = RaycastAtPointer();
        if (target == hovered) return;

        if (hovered != null) hovered.SetHover(false);
        hovered = target;
        if (hovered != null) hovered.SetHover(true);
    }

    private void OnClick(InputAction.CallbackContext ctx)
    {
        if (!CanExplore() || IsPointerOverUI()) return;

        var target = RaycastAtPointer();
        if (target == null) return;

        // Ждём отпускания кнопки, иначе тот же клик сразу пролистнёт первую реплику Yarn.
        if (pendingInteract != null) StopCoroutine(pendingInteract);
        pendingInteract = StartCoroutine(InteractAfterRelease(target));
    }

    private IEnumerator InteractAfterRelease(InteractiveObject target)
    {
        while (Pointer.current != null && Pointer.current.press.isPressed)
            yield return null;
        yield return null;

        pendingInteract = null;
        if (target != null && CanExplore() && !IsPointerOverUI())
            target.Interact(dialogueRunner);
    }

    private InteractiveObject RaycastAtPointer()
    {
        if (cam == null || Pointer.current == null) return null;

        Vector2 screenPos = Pointer.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(screenPos);

        RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, interactiveLayers);
        return hit.collider != null ? hit.collider.GetComponent<InteractiveObject>() : null;
    }

    private static bool IsPointerOverUI()
    {
        if (EventSystem.current == null || Pointer.current == null) return false;

        var data = new PointerEventData(EventSystem.current)
        {
            position = Pointer.current.position.ReadValue()
        };
        uiHits.Clear();
        EventSystem.current.RaycastAll(data, uiHits);
        return uiHits.Count > 0;
    }

    private void ClearHover()
    {
        if (hovered != null) hovered.SetHover(false);
        hovered = null;
    }
}
