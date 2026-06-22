using UnityEngine;
using UnityEngine.UI;

namespace Work.Dispatch.Code.Runtime
{
    /// <summary>
    /// 버튼 클릭을 파견 지도 열기 동작에 연결
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DispatchOpenButtonBinder : MonoBehaviour
    {
        [SerializeField] private DispatchController dispatchController;
        [SerializeField] private Button button;
        [SerializeField] private bool findControllerOnEnable = true;
        [SerializeField] private bool bindButtonOnEnable = true;

        private void Reset()
        {
            button = GetComponent<Button>();
            dispatchController = GetComponentInParent<DispatchController>();
        }

        private void Awake()
        {
            EnsureReferences();
            BindButton();
        }

        private void OnEnable()
        {
            EnsureReferences();

            if (bindButtonOnEnable == true)
            {
                BindButton();
            }
        }

        private void OnDisable()
        {
            UnbindButton();
        }

        /// <summary>
        /// 지도 열기 액션 실행
        /// </summary>
        public void InvokeOpenMap()
        {
            EnsureReferences();

            if (dispatchController == null)
            {
                return;
            }

            dispatchController.OpenMap();
        }

        private void EnsureReferences()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (dispatchController != null || findControllerOnEnable == false)
            {
                return;
            }

            dispatchController = GetComponentInParent<DispatchController>();
            if (dispatchController == null)
            {
                dispatchController = FindFirstObjectByType<DispatchController>();
            }
        }

        private void BindButton()
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(InvokeOpenMap);
            button.onClick.AddListener(InvokeOpenMap);
        }

        private void UnbindButton()
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(InvokeOpenMap);
        }
    }
}
