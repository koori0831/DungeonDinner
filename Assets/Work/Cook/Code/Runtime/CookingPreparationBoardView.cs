using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime
{
    public sealed class CookingPreparationBoardView : MonoBehaviour
    {
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private Transform modelAnchor;
        [SerializeField] private RawImage previewImage;
        [SerializeField] private GameObject emptyModelFallback;
        [SerializeField] private bool findGamePanelOnEnable = true;
        [SerializeField] private bool showFallbackWhenModelMissing;
        [SerializeField] private bool createPreviewImageWhenMissing = true;

        [Header("Preview Render")]
        [SerializeField] private Vector2Int renderTextureSize = new Vector2Int(512, 512);
        [SerializeField] private Color clearColor = new Color(0f, 0f, 0f, 0f);
        [SerializeField] private Vector3 previewRootPosition = new Vector3(10000f, 10000f, 10000f);
        [SerializeField] private Vector3 modelRotationEuler = new Vector3(18f, -32f, 0f);
        [SerializeField, Min(0.01f)] private float modelScale = 1f;
        [SerializeField, Min(0.1f)] private float boundsPadding = 1.25f;
        [SerializeField, Min(0.01f)] private float minimumCameraSize = 0.5f;
        [SerializeField, Range(0, 31)] private int previewLayer = 31;

        [Header("Auto Layout")]
        [SerializeField] private int previewSiblingIndex = 1;
        [SerializeField, Min(32f)] private float previewPreferredHeight = 320f;

        private CookingGamePanel _subscribedPanel;
        private IngredientSO _currentIngredient;
        private GameObject _currentPrefab;
        private GameObject _spawnedModel;
        private RenderTexture _renderTexture;
        private Camera _previewCamera;
        private Transform _previewRoot;
        private Transform _previewModelRoot;
        private bool _ownsPreviewImage;

        private void Reset()
        {
            modelAnchor = transform;
            gamePanel = GetComponentInParent<CookingGamePanel>();
        }

        private void OnEnable()
        {
            EnsureReferences();
            SubscribePanel();
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribePanel();
            ClearSpawnedModel();
            SetPreviewVisible(false);
        }

        private void OnDestroy()
        {
            UnsubscribePanel();
            ClearSpawnedModel();
            DestroyPreviewResources();
        }

        public void SetGamePanel(CookingGamePanel value)
        {
            if (gamePanel == value)
                return;

            UnsubscribePanel();
            gamePanel = value;

            if (isActiveAndEnabled)
                SubscribePanel();

            Refresh();
        }

        public void Refresh()
        {
            EnsureReferences();

            IngredientSO ingredient = gamePanel != null ? gamePanel.GetCurrentPreparationIngredient() : null;
            GameObject prefab = ingredient != null ? ingredient.ModelPrefab : null;

            if (_currentIngredient == ingredient && _currentPrefab == prefab && _spawnedModel != null)
            {
                SetPreviewVisible(true);
                return;
            }

            if (_currentIngredient == ingredient && _currentPrefab == prefab && prefab == null)
            {
                SetPreviewVisible(false);
                SetFallbackVisible(false);
                return;
            }

            _currentIngredient = ingredient;
            _currentPrefab = prefab;
            ClearSpawnedModel();

            if (prefab == null)
            {
                SetPreviewVisible(false);
                SetFallbackVisible(true);
                return;
            }

            EnsurePreviewPipeline();
            if (_previewModelRoot == null)
            {
                SetPreviewVisible(false);
                SetFallbackVisible(false);
                return;
            }

            _spawnedModel = Instantiate(prefab, _previewModelRoot);
            _spawnedModel.transform.localPosition = Vector3.zero;
            _spawnedModel.transform.localRotation = Quaternion.Euler(modelRotationEuler);
            _spawnedModel.transform.localScale = Vector3.one * modelScale;
            SetLayerRecursively(_spawnedModel, previewLayer);
            FitPreviewCameraToModel();

            SetPreviewVisible(true);
            SetFallbackVisible(false);
        }

        private void EnsureReferences()
        {
            if (modelAnchor == null)
                modelAnchor = transform;

            if (gamePanel != null || findGamePanelOnEnable == false)
                return;

            gamePanel = GetComponentInParent<CookingGamePanel>();
            if (gamePanel == null)
                gamePanel = FindFirstObjectByType<CookingGamePanel>();
        }

        private void SubscribePanel()
        {
            if (_subscribedPanel == gamePanel)
                return;

            UnsubscribePanel();
            if (gamePanel == null)
                return;

            gamePanel.SnapshotChanged += HandleSnapshotChanged;
            _subscribedPanel = gamePanel;
        }

        private void UnsubscribePanel()
        {
            if (_subscribedPanel == null)
                return;

            _subscribedPanel.SnapshotChanged -= HandleSnapshotChanged;
            _subscribedPanel = null;
        }

        private void HandleSnapshotChanged(CookingGameSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Screen == CookingGameScreenState.Preparation)
            {
                Refresh();
                return;
            }

            _currentIngredient = null;
            _currentPrefab = null;
            ClearSpawnedModel();
            SetPreviewVisible(false);
            SetFallbackVisible(false);
        }

        private void EnsurePreviewPipeline()
        {
            EnsurePreviewImage();
            EnsureRenderTexture();
            EnsurePreviewCamera();

            if (previewImage != null)
                previewImage.texture = _renderTexture;
            if (_previewCamera != null)
                _previewCamera.targetTexture = _renderTexture;
        }

        private void EnsurePreviewImage()
        {
            if (previewImage != null || createPreviewImageWhenMissing == false)
                return;

            GameObject previewObject = new GameObject(
                "IngredientPreviewImage",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage),
                typeof(LayoutElement));
            previewObject.layer = gameObject.layer;
            previewObject.transform.SetParent(transform, false);
            previewObject.transform.SetSiblingIndex(Mathf.Clamp(previewSiblingIndex, 0, transform.childCount - 1));

            RectTransform rectTransform = previewObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 0.5f);
            rectTransform.anchorMax = new Vector2(1f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(0f, previewPreferredHeight);

            LayoutElement layoutElement = previewObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = previewPreferredHeight * 0.6f;
            layoutElement.preferredHeight = previewPreferredHeight;
            layoutElement.flexibleHeight = 1f;

            previewImage = previewObject.GetComponent<RawImage>();
            previewImage.raycastTarget = false;
            previewImage.color = Color.white;
            previewObject.SetActive(false);
            _ownsPreviewImage = true;
        }

        private void EnsureRenderTexture()
        {
            int width = Mathf.Max(32, renderTextureSize.x);
            int height = Mathf.Max(32, renderTextureSize.y);

            if (_renderTexture != null && _renderTexture.width == width && _renderTexture.height == height)
                return;

            if (_renderTexture != null)
            {
                if (previewImage != null && previewImage.texture == _renderTexture)
                    previewImage.texture = null;

                _renderTexture.Release();
                DestroyUnityObject(_renderTexture);
            }

            _renderTexture = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32)
            {
                name = "CookingIngredientPreviewTexture",
                useMipMap = false,
                autoGenerateMips = false
            };
            _renderTexture.Create();
        }

        private void EnsurePreviewCamera()
        {
            if (_previewRoot == null)
            {
                GameObject rootObject = new GameObject($"{name}_IngredientPreviewRuntime");
                rootObject.hideFlags = HideFlags.HideAndDontSave;
                _previewRoot = rootObject.transform;
            }

            _previewRoot.position = previewRootPosition;
            _previewRoot.rotation = Quaternion.identity;

            if (_previewModelRoot == null)
            {
                GameObject modelRootObject = new GameObject("ModelRoot");
                modelRootObject.hideFlags = HideFlags.HideAndDontSave;
                modelRootObject.transform.SetParent(_previewRoot, false);
                _previewModelRoot = modelRootObject.transform;
            }

            _previewModelRoot.localPosition = Vector3.zero;
            _previewModelRoot.localRotation = Quaternion.identity;
            _previewModelRoot.localScale = Vector3.one;

            if (_previewCamera == null)
            {
                GameObject cameraObject = new GameObject("Camera");
                cameraObject.hideFlags = HideFlags.HideAndDontSave;
                cameraObject.transform.SetParent(_previewRoot, false);
                _previewCamera = cameraObject.AddComponent<Camera>();
                _previewCamera.clearFlags = CameraClearFlags.SolidColor;
                _previewCamera.orthographic = true;
                _previewCamera.nearClipPlane = 0.01f;
                _previewCamera.depth = -100f;
                _previewCamera.enabled = false;
            }

            _previewCamera.backgroundColor = clearColor;
            _previewCamera.cullingMask = 1 << previewLayer;
            _previewCamera.transform.position = previewRootPosition + new Vector3(0f, 0f, -10f);
            _previewCamera.transform.rotation = Quaternion.identity;
        }

        private void FitPreviewCameraToModel()
        {
            if (_spawnedModel == null || _previewCamera == null || _previewModelRoot == null)
                return;

            if (TryCalculateModelBounds(out Bounds bounds) == false)
            {
                SetPreviewVisible(false);
                return;
            }

            _spawnedModel.transform.position += _previewModelRoot.position - bounds.center;

            if (TryCalculateModelBounds(out bounds) == false)
                return;

            float aspect = _renderTexture != null && _renderTexture.height > 0
                ? (float)_renderTexture.width / _renderTexture.height
                : 1f;
            float halfHeight = Mathf.Max(bounds.extents.y, bounds.extents.x / Mathf.Max(0.01f, aspect));
            float cameraSize = Mathf.Max(minimumCameraSize, halfHeight * boundsPadding);
            float cameraDistance = Mathf.Max(10f, bounds.extents.z * 4f + 4f);

            _previewCamera.orthographicSize = cameraSize;
            _previewCamera.farClipPlane = cameraDistance + Mathf.Max(10f, bounds.size.z * 4f);
            _previewCamera.transform.position = _previewModelRoot.position + new Vector3(0f, 0f, -cameraDistance);
            _previewCamera.transform.rotation = Quaternion.identity;
        }

        private bool TryCalculateModelBounds(out Bounds bounds)
        {
            bounds = default;

            if (_spawnedModel == null)
                return false;

            Renderer[] renderers = _spawnedModel.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer modelRenderer = renderers[i];
                if (modelRenderer == null)
                    continue;

                if (hasBounds == false)
                {
                    bounds = modelRenderer.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(modelRenderer.bounds);
            }

            return hasBounds;
        }

        private void ClearSpawnedModel()
        {
            if (_spawnedModel == null)
                return;

            DestroyUnityObject(_spawnedModel);

            _spawnedModel = null;
        }

        private void SetPreviewVisible(bool visible)
        {
            if (previewImage != null && previewImage.gameObject.activeSelf != visible)
                previewImage.gameObject.SetActive(visible);

            if (_previewCamera != null)
                _previewCamera.enabled = visible;
        }

        private void SetFallbackVisible(bool visible)
        {
            if (emptyModelFallback == null)
                return;

            bool nextVisible = visible && showFallbackWhenModelMissing;
            if (emptyModelFallback.activeSelf != nextVisible)
                emptyModelFallback.SetActive(nextVisible);
        }

        private void DestroyPreviewResources()
        {
            if (_renderTexture != null)
            {
                if (previewImage != null && previewImage.texture == _renderTexture)
                    previewImage.texture = null;

                _renderTexture.Release();
                DestroyUnityObject(_renderTexture);
                _renderTexture = null;
            }

            if (_previewRoot != null)
            {
                DestroyUnityObject(_previewRoot.gameObject);
                _previewRoot = null;
                _previewCamera = null;
                _previewModelRoot = null;
            }

            if (_ownsPreviewImage == true && previewImage != null)
            {
                DestroyUnityObject(previewImage.gameObject);
                previewImage = null;
                _ownsPreviewImage = false;
            }
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            if (target == null)
                return;

            target.layer = layer;
            Transform targetTransform = target.transform;
            for (int i = 0; i < targetTransform.childCount; i++)
                SetLayerRecursively(targetTransform.GetChild(i).gameObject, layer);
        }

        private static void DestroyUnityObject(Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }
    }
}
