using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class TitleUIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TitleUIManager manager;
    [SerializeField] private Button button;
    [SerializeField] private RectTransform selectionTarget;
    [SerializeField] private TitleButtonAction action;

    private RectTransform _rectTransform;

    public TitleButtonAction Action => action;
    public RectTransform SelectionTarget => selectionTarget != null ? selectionTarget : RectTransform;
    public RectTransform RectTransform
    {
        get
        {
            if (_rectTransform == null)
                _rectTransform = transform as RectTransform;

            return _rectTransform;
        }
    }

    private void Reset()
    {
        button = GetComponent<Button>();
        manager = GetComponentInParent<TitleUIManager>();
        selectionTarget = transform as RectTransform;
    }

    private void Awake()
    {
        EnsureReferences();
    }

    private void OnEnable()
    {
        EnsureReferences();
        BindButton();
    }

    private void OnDisable()
    {
        UnbindButton();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        NotifyHovered();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (manager != null)
            manager.HideSelection(this);
    }

    public void NotifyHovered()
    {
        EnsureReferences();

        if (button != null && button.IsInteractable() == false)
            return;

        if (manager != null)
            manager.MoveSelectionTo(this);
    }

    public void InvokeAction()
    {
        EnsureReferences();

        if (button != null && button.IsInteractable() == false)
            return;

        if (manager != null)
            manager.InvokeButton(this);
    }

    private void EnsureReferences()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (_rectTransform == null)
            _rectTransform = transform as RectTransform;

        if (selectionTarget == null)
            selectionTarget = _rectTransform;

        if (manager == null)
        {
            manager = GetComponentInParent<TitleUIManager>();
            if (manager == null)
                manager = FindFirstObjectByType<TitleUIManager>();
        }
    }

    private void BindButton()
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(InvokeAction);
        button.onClick.AddListener(InvokeAction);
    }

    private void UnbindButton()
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(InvokeAction);
    }
}

public enum TitleButtonAction
{
    Start,
    Settings,
    Exit
}
