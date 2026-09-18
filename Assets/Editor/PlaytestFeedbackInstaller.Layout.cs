using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Runtime.UI;
using Object = UnityEngine.Object;

public static partial class PlaytestFeedbackInstaller
{
    private static T Ref<T>(Object target, string field) where T : Object => new SerializedObject(target).FindProperty(field)?.objectReferenceValue as T;
    private static void Fixed(RectTransform rect, Transform parent, Vector2 position, Vector2 size)
    {
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        var element = rect.GetComponent<LayoutElement>(); if (element != null) element.ignoreLayout = true;
    }
    private static void RemoveLayout(RectTransform rect)
    {
        foreach (var component in rect.GetComponents<LayoutGroup>()) Object.DestroyImmediate(component);
        var fitter = rect.GetComponent<ContentSizeFitter>(); if (fitter != null) Object.DestroyImmediate(fitter);
    }
    private static TextMeshProUGUI Label(Transform parent, string name, string text, Vector2 position, Vector2 size, float fontSize = 22)
    {
        var rect = Child(parent, name); Fixed(rect, parent, position, size);
        var label = Ensure<TextMeshProUGUI>(rect.gameObject);
        label.font = Theme.FontAsset; label.fontSize = fontSize; label.color = Theme.PrimaryTextColor;
        label.text = text; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        return label;
    }
    private static Button Button(Transform parent, string name, string text, Vector2 position, Vector2 size)
    {
        var rect = Child(parent, name); Fixed(rect, parent, position, size); Panel(rect, Theme.PrimaryButtonSprite);
        var button = Ensure<Button>(rect.gameObject);
        button.targetGraphic = rect.GetComponent<Image>(); button.targetGraphic.raycastTarget = true;
        Label(rect, "Label", text, Vector2.zero, size - new Vector2(12, 8), 20);
        if (button.image.sprite == Theme.SecondaryButtonSprite)
            button.GetComponentInChildren<TextMeshProUGUI>().color = Theme.ParchmentColor;
        return button;
    }

    private static void ConfigureBag(CookingIngredientSelectionView view)
    {
        var window = (RectTransform)view.transform;
        var canvas = view.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        var owner = Ref<CookingGamePanel>(view, "gamePanel");
        var body = Child(window, "Body");
        var available = Ref<RectTransform>(view, "availableIngredientRoot");
        var selected = Ref<RectTransform>(view, "selectedIngredientRoot");
        var availableScroll = available.GetComponentInParent<ScrollRect>(true) ?? available.parent.gameObject.AddComponent<ScrollRect>();
        var selectedScroll = selected.GetComponentInParent<ScrollRect>(true) ?? selected.parent.gameObject.AddComponent<ScrollRect>();
        availableScroll.content = available; availableScroll.viewport = (RectTransform)available.parent;
        selectedScroll.content = selected; selectedScroll.viewport = (RectTransform)selected.parent;
        if (available.parent.GetComponent<RectMask2D>() == null) available.parent.gameObject.AddComponent<RectMask2D>();
        if (selected.parent.GetComponent<RectMask2D>() == null) selected.parent.gameObject.AddComponent<RectMask2D>();
        // Inventory entries are generated from actual stock; remove obsolete scene preview rows.
        foreach (var container in new[] { available, selected })
            for (int index = container.childCount - 1; index >= 0; index--)
                Object.DestroyImmediate(container.GetChild(index).gameObject);
        RemoveLayout(window); RemoveLayout(body);
        var alreadyMoved = window.parent == canvas.transform;
        var oldPosition = window.anchoredPosition;
        bool alreadyTopPivot = window.pivot.y > .99f;
        Fixed(window, canvas.transform, alreadyMoved ? oldPosition : new Vector2(300, 0), new Vector2(720, 680));
        window.pivot = new Vector2(.5f, 1);
        if (!alreadyTopPivot) window.anchoredPosition += new Vector2(0, 340);
        Panel(window, Theme.PanelSprite); window.GetComponent<Image>().raycastTarget = true;
        var windowGroup = Ensure<CanvasGroup>(window.gameObject);
        Fixed(body, window, new Vector2(0, -28), new Vector2(672, 592));
        var bodyGroup = Ensure<CanvasGroup>(body.gameObject);
        var titleBar = Child(window, "TitleBar"); Fixed(titleBar, window, new Vector2(0, 307), new Vector2(672, 50));
        titleBar.anchorMin = titleBar.anchorMax = new Vector2(.5f, 1);
        titleBar.anchoredPosition = new Vector2(0, -33);
        Image(titleBar, new Color(1, 1, 1, 0)).raycastTarget = true;
        Label(titleBar, "Title", "재료 가방", new Vector2(-210, 0), new Vector2(200, 42), 26);
        var oldTitle = window.Find("Title"); if (oldTitle != null) Object.DestroyImmediate(oldTitle.gameObject);
        var collapse = Button(titleBar, "Collapse", "접기", new Vector2(192, 0), new Vector2(88, 42));
        var close = Button(titleBar, "Close", "닫기", new Vector2(286, 0), new Vector2(88, 42));
        var reopen = Button(canvas.transform, "IngredientBagButton", "가방", new Vector2(810, -458), new Vector2(108, 52));
        var reopenRect = (RectTransform)reopen.transform;
        reopenRect.anchorMin = reopenRect.anchorMax = reopenRect.pivot = new Vector2(1, 0);
        reopenRect.anchoredPosition = new Vector2(-24, 24);
        var popup = Ensure<CookingIngredientBagPopup>(canvas.gameObject);
        Set(popup, "owner", owner); Set(popup, "window", window); Set(popup, "windowGroup", windowGroup); Set(popup, "bodyGroup", bodyGroup);
        Set(popup, "collapseButton", collapse); Set(popup, "closeButton", close); Set(popup, "openButton", reopen);
        windowGroup.hideFlags = HideFlags.None; bodyGroup.hideFlags = HideFlags.None;
        EditorUtility.SetDirty(windowGroup); EditorUtility.SetDirty(bodyGroup);

        var drag = Ensure<CookingPopupDragHandle>(titleBar.gameObject); Set(drag, "popup", popup);

        var search = Ref<TMP_InputField>(view, "searchInputField");
        Fixed((RectTransform)search.transform, body, new Vector2(0, 268), new Vector2(656, 42)); Panel((RectTransform)search.transform, Theme.CardSprite);
        search.textComponent.color = Theme.PrimaryTextColor;
        if (search.placeholder is TMP_Text placeholder) placeholder.color = Theme.SecondaryTextColor;
        PlaceField(view, "availableSummaryField", body, new Vector2(0, 228), new Vector2(656, 30), 20);
        ConfigureBagGrid(available, availableScroll, body, new Vector2(0, 80), new Vector2(656, 258));
        ConfigureBagGrid(selected, selectedScroll, body, new Vector2(0, -151), new Vector2(656, 118));
        Set(view, "availableIngredientScrollRect", availableScroll); Set(view, "selectedIngredientScrollRect", selectedScroll);
        PlaceField(view, "selectedSummaryField", body, new Vector2(0, -71), new Vector2(656, 30), 20);
        PlaceField(view, "ingredientDetailField", body, new Vector2(0, -228), new Vector2(656, 32), 17);
        PlaceField(view, "selectionRuleField", body, new Vector2(-124, -270), new Vector2(390, 30), 16);
        PlaceField(view, "emptyAvailableField", (RectTransform)availableScroll.transform, Vector2.zero, new Vector2(610, 50), 20);
        PlaceField(view, "emptySelectedField", (RectTransform)selectedScroll.transform, Vector2.zero, new Vector2(610, 50), 18);
        var confirm = Ref<Button>(view, "confirmButton"); var clear = Ref<Button>(view, "clearButton");
        Fixed((RectTransform)clear.transform, body, new Vector2(181, -269), new Vector2(86, 44));
        Fixed((RectTransform)confirm.transform, body, new Vector2(283, -269), new Vector2(106, 44));
        Panel((RectTransform)clear.transform, Theme.SecondaryButtonSprite); clear.image.raycastTarget = true;
        Panel((RectTransform)confirm.transform, Theme.PrimaryButtonSprite); confirm.image.raycastTarget = true;
        foreach (string old in new[] { "BagSection", "SelectedSection" }) { var t = body.Find(old); if (t != null) Object.DestroyImmediate(t.gameObject); }
        var actions = window.Find("ActionRow"); if (actions != null) Object.DestroyImmediate(actions.gameObject);
        foreach (var image in window.GetComponentsInChildren<Image>(true))
            if (image.GetComponent<Outline>() is Outline outline) Object.DestroyImmediate(outline);
    }

    private static void PlaceField(Object owner, string field, RectTransform parent, Vector2 position, Vector2 size, float fontSize)
    {
        var text = Ref<TextMeshProUGUI>(owner, field); if (text == null) return;
        Fixed(text.rectTransform, parent, position, size); text.font = Theme.FontAsset; text.fontSize = fontSize;
        text.color = Theme.PrimaryTextColor; text.raycastTarget = false; text.enableAutoSizing = false;
    }
    private static void ConfigureBagGrid(RectTransform content, ScrollRect scroll, RectTransform body, Vector2 position, Vector2 size)
    {
        Fixed((RectTransform)scroll.transform, body, position, size); RemoveLayout(content);
        var grid = content.gameObject.AddComponent<GridLayoutGroup>(); grid.cellSize = new Vector2(150, 112); grid.spacing = new Vector2(12, 12);
        grid.padding = new RectOffset(8, 8, 8, 8); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 4;
        content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
        content.anchoredPosition = Vector2.zero; content.sizeDelta = new Vector2(0, 0);
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 24;
        var background = scroll.GetComponent<Image>(); if (background != null) { background.sprite = Theme.ReceiptSprite; background.color = Color.white; background.type = UnityEngine.UI.Image.Type.Sliced; }
    }

    private static void ConfigureResult(CookingResultView view)
    {
        var root = (RectTransform)view.transform;
        RemoveLayout(root);
        root.anchorMin = root.anchorMax = new Vector2(.66f, .5f); root.pivot = new Vector2(.5f, .5f);
        root.anchoredPosition = Vector2.zero; root.sizeDelta = new Vector2(720, 760);
        Panel(root, Theme.PanelSprite); root.GetComponent<Image>().raycastTarget = true;
        var hero = Child(root, "DishHeroPanel"); RemoveLayout(hero);
        Fixed(hero, root, new Vector2(0, 238), new Vector2(656, 230));
        var visual = Ref<RectTransform>(view, "dishVisualRoot"); Fixed(visual, hero, new Vector2(-175, 0), new Vector2(220, 210));
        PlaceField(view, "dishNameField", hero, new Vector2(105, 48), new Vector2(290, 90), 28);
        PlaceField(view, "recipeField", hero, new Vector2(105, -28), new Vector2(290, 48), 18);
        PlaceField(view, "representativeTagsField", hero, new Vector2(105, -78), new Vector2(290, 40), 17);
        var quality = Ref<CanvasGroup>(view, "qualityGroup");
        var qualityRect = (RectTransform)quality.transform;
        Fixed(qualityRect, root, new Vector2(0, 70), new Vector2(590, 86)); RemoveLayout(qualityRect); Panel(qualityRect, Theme.CardSprite);
        var qualityVisual = Ref<RectTransform>(view, "qualityVisualRoot");
        if (qualityVisual == qualityRect) { qualityVisual = Child(qualityRect, "QualityContent"); Set(view, "qualityVisualRoot", qualityVisual); }
        Fixed(qualityVisual, qualityRect, Vector2.zero, new Vector2(550, 74)); RemoveLayout(qualityVisual);
        var qualityIcon = Ref<Image>(view, "qualityIconImage");
        Fixed(qualityIcon.rectTransform, qualityVisual, new Vector2(-210, 0), new Vector2(60, 60)); qualityIcon.preserveAspect = true;
        PlaceField(view, "qualityNameField", qualityVisual, new Vector2(20, 17), new Vector2(360, 34), 24);
        PlaceField(view, "qualityScoreField", qualityVisual, new Vector2(20, -19), new Vector2(360, 30), 18);
        var toggle = Ref<Button>(view, "detailsToggleButton");
        Fixed((RectTransform)toggle.transform, root, new Vector2(0, -12), new Vector2(200, 46));
        Panel((RectTransform)toggle.transform, Theme.SecondaryButtonSprite); toggle.image.raycastTarget = true;
        var drawer = Ref<GameObject>(view, "detailsDrawer");
        var drawerRect = (RectTransform)drawer.transform;
        Fixed(drawerRect, root, new Vector2(0, -150), new Vector2(614, 202)); Panel(drawerRect, Theme.CardSprite);
        foreach (Transform child in drawer.transform.Cast<Transform>().ToArray())
            if (child.name != "DetailScroll") Object.DestroyImmediate(child.gameObject);
        var scrollRect = Child(drawer.transform, "DetailScroll"); Stretch(scrollRect);
        scrollRect.offsetMin = new Vector2(16, 16); scrollRect.offsetMax = new Vector2(-16, -16);
        var scroll = Ensure<ScrollRect>(scrollRect.gameObject);
        var viewport = Child(scrollRect, "Viewport"); Stretch(viewport);
        Image(viewport, Color.clear).raycastTarget = true;
        Ensure<RectMask2D>(viewport.gameObject);
        var summary = Label(viewport, "Summary", "", Vector2.zero, new Vector2(550, 0), 18);
        summary.alignment = TextAlignmentOptions.TopLeft; summary.textWrappingMode = TextWrappingModes.Normal;
        summary.margin = new Vector4(8, 4, 12, 8);
        var textRect = summary.rectTransform; textRect.anchorMin = new Vector2(0, 1); textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(.5f, 1); textRect.anchoredPosition = Vector2.zero; textRect.sizeDelta = Vector2.zero;
        Ensure<ContentSizeFitter>(textRect.gameObject).verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport; scroll.content = textRect; scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 24;
        Set(view, "detailSummaryField", summary);
        Work.Cook.Code.Info.InfoDisplayPanel.AddReadingScrollbar(scroll, 0);
        var reaction = Ref<CanvasGroup>(view, "reactionGroup");
        if (reaction != null)
        {
            var rect = (RectTransform)reaction.transform; RemoveLayout(rect);
            Fixed(rect, root, new Vector2(0, -125), new Vector2(590, 126)); Panel(rect, Theme.CardSprite);
            var content = Ref<RectTransform>(view, "reactionVisualRoot");
            if (content == rect) { content = Child(rect, "ReactionContent"); Set(view, "reactionVisualRoot", content); }
            RemoveLayout(content);
            Fixed(content, rect, Vector2.zero, new Vector2(558, 114));
            var npcIcon = Ref<Image>(view, "npcIconImage");
            Fixed(npcIcon.rectTransform, content, new Vector2(-218, 0), new Vector2(66, 92)); npcIcon.preserveAspect = true;
            var mood = Ref<Image>(view, "reactionIconImage");
            Fixed(mood.rectTransform, content, new Vector2(-145, 10), new Vector2(46, 46)); mood.preserveAspect = true;
            PlaceField(view, "npcNameField", content, new Vector2(55, 37), new Vector2(350, 28), 19);
            PlaceField(view, "reactionNameField", content, new Vector2(55, 8), new Vector2(350, 28), 21);
            PlaceField(view, "reactionSummaryField", content, new Vector2(55, -32), new Vector2(350, 46), 16);
        }
        var reward = Ref<CanvasGroup>(view, "rewardPreviewGroup");
        if (reward != null)
        {
            var rect = (RectTransform)reward.transform; RemoveLayout(rect);
            Fixed(rect, root, new Vector2(0, -216), new Vector2(400, 40));
            var icon = Ref<Image>(view, "rewardIconImage"); Fixed(icon.rectTransform, rect, new Vector2(-90, 0), new Vector2(32, 32));
            PlaceField(view, "rewardPreviewField", rect, new Vector2(30, 0), new Vector2(190, 36), 22);
        }
        var actions = Ref<CanvasGroup>(view, "actionGroup");
        Fixed((RectTransform)actions.transform, root, new Vector2(0, -300), new Vector2(590, 58));
        var hand = Ref<Button>(view, "handToNpcButton");
        Fixed((RectTransform)hand.transform, actions.transform, Vector2.zero, new Vector2(280, 56));
        Panel((RectTransform)hand.transform, Theme.PrimaryButtonSprite); hand.image.raycastTarget = true;
        var backdrop = Ref<CanvasGroup>(view, "backdropGroup");
        if (backdrop != null && backdrop.TryGetComponent<Image>(out var dim)) { dim.color = Color.clear; dim.raycastTarget = false; }
        var kept = new System.Collections.Generic.HashSet<Image> {
            root.GetComponent<Image>(), qualityRect.GetComponent<Image>(), drawerRect.GetComponent<Image>(),
            toggle.image, hand.image, Ref<Image>(view, "dishIconImage"), qualityIcon,
            Ref<Image>(view, "npcIconImage"), Ref<Image>(view, "reactionIconImage"), Ref<Image>(view, "rewardIconImage"),
            Ref<Image>(view, "revealInputBlocker")
        };
        if (reaction != null) kept.Add(reaction.GetComponent<Image>());
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            if (kept.Contains(image) || image.name == "PaperBackground" || image.transform.IsChildOf(drawer.transform)) continue;
            image.enabled = false; image.raycastTarget = false;
        }
        foreach (var field in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            field.color = Theme.PrimaryTextColor; field.font = Theme.FontAsset;
            field.textWrappingMode = TextWrappingModes.Normal;
        }
    }

    private static void ConfigureSystemPanels(GameObject root)
    {
        foreach (var view in root.GetComponentsInChildren<CookingKnowledgeUpdateView>(true))
        {
            var page = Ref<RectTransform>(view, "pageRoot");
            RemoveLayout(page);
            Fixed(page, view.transform, Vector2.zero, new Vector2(640, 480));
            Panel(page, Theme.PanelSprite);
            PlaceField(view, "titleField", page, new Vector2(0, 171), new Vector2(554, 48), 28);
            PlaceField(view, "bodyField", page, new Vector2(0, 12), new Vector2(554, 245), 20);
            var next = Ref<Button>(view, "nextButton");
            Fixed((RectTransform)next.transform, page, new Vector2(0, -178), new Vector2(200, 52)); Panel((RectTransform)next.transform, Theme.PrimaryButtonSprite);
        }
        foreach (var flow in root.GetComponentsInChildren<Work.Cook.Code.Runtime.Systems.CookingBusinessFlowController>(true))
        {
            var panel = Ref<RectTransform>(flow, "actionRoot"); if (panel == null) continue;
            Panel(panel, Theme.PanelSprite);
            foreach (var text in panel.GetComponentsInChildren<TextMeshProUGUI>(true)) { text.font = Theme.FontAsset; text.color = Theme.PrimaryTextColor; }
            foreach (var button in panel.GetComponentsInChildren<Button>(true)) Panel((RectTransform)button.transform, Theme.PrimaryButtonSprite);
        }
        foreach (var toast in root.GetComponentsInChildren<CookingRewardToastView>(true))
        {
            var panel = Ref<RectTransform>(toast, "visualRoot"); if (panel == null) continue;
            Panel(panel, Theme.PanelSprite);
            foreach (var text in panel.GetComponentsInChildren<TextMeshProUGUI>(true)) { text.font = Theme.FontAsset; text.color = Theme.PrimaryTextColor; }
        }
    }
}
