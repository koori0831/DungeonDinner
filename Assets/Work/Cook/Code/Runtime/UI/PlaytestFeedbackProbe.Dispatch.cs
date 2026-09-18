#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Work.Cook.Code.Data;
using Work.Dispatch.Code.Runtime;
using Work.Dispatch.Code.UI;
using Work.NPC.Code.Runtime;
using Work.TimeSystem;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed partial class PlaytestFeedbackProbe
    {
        private IEnumerator DispatchVisualChecks()
        {
            yield return Unblocked();
            var screen = Find<DispatchScreenPresenter>();
            var document = screen.GetComponent<UIDocument>();
            var originalSettings = document.panelSettings;
            var settings = Instantiate(originalSettings);
            settings.clearColor = true;
            document.panelSettings = settings;
            var theme = Field<CookingUiPresentationSettingsSO>(screen, "presentationTheme");
            var manager = Find<DispatchManager>();
            var time = Find<GameTimeService>();
            screen.Show();
            yield return Frames(5);
            var tree = document.rootVisualElement;
            yield return CaptureDispatch(settings, "dispatch-locked", new Vector2Int(1920, 1080));
            Check(tree.Query<VisualElement>(className: "is-locked").ToList().Any(r => r.ClassListContains("npc-row")), "locked state reaches visible NPC rows");

            // Explicit test fixture: make the catalog's supported companion available.
            var director = Find<NpcEncounterDirector>();
            string npcId = manager.Catalog.NpcRules[0].NpcId;
            Field<NpcEncounterHistory>(director, "_history").RecordResult(npcId, "dispatch-visual-qa", default, 10, time.CurrentDay);
            screen.Show();
            yield return Frames(5);
            var npcRow = tree.Query<VisualElement>(className: "npc-row").ToList().First(r => !r.ClassListContains("is-locked"));
            DispatchClick(npcRow);
            yield return Frames(5);
            Check(tree.Query<VisualElement>(className: "npc-row").ToList().Any(r => r.ClassListContains("is-selected")), "NPC selection remains visible after list rebuild");
            DispatchClick(tree.Query<VisualElement>(className: "region-row").First());
            yield return Frames(5);
            Check(tree.Query<VisualElement>(className: "region-row").ToList().Any(r => r.ClassListContains("is-selected")), "destination selection remains visible after list rebuild");
            for (int index = 0; index < 3; index++)
            {
                var row = tree.Query<VisualElement>(className: "material-row").ToList()[index];
                Check(row.Q<Button>("increase-button").style.backgroundImage.value.sprite == theme.SecondaryButtonSprite, "virtualized quantity button uses shared artwork");
                DispatchSubmit(row.Q<Button>("increase-button"));
                yield return Frames(4);
            }
            Check(tree.Query<VisualElement>(className: "material-row").ToList().Count(r => r.ClassListContains("is-selected")) == 3, "three requested materials are highlighted");
            foreach (var size in Resolutions())
            {
                yield return CaptureDispatch(settings, "dispatch-request", size);
                CheckDispatchBounds(tree.Q<VisualElement>(className: "ledger"), tree.Q<VisualElement>("dispatch-root"), "ledger stays inside screen");
                CheckDispatchBounds(tree.Q<Button>("dispatch-button"), tree.Q<VisualElement>(className: "ledger"), "send button remains in ledger");
                var thumb = tree.Q<ScrollView>(className: "summary-scroll").verticalScroller.slider.Q("unity-dragger");
                Color thumbColor = thumb.resolvedStyle.backgroundColor;
                Check(thumbColor.r > thumbColor.g && thumbColor.g > thumbColor.b, "scrollbar thumb uses the shared brown palette");
            }
            DispatchSubmit(tree.Q<Button>("dispatch-button"));
            yield return Frames(4);
            Check(tree.Q<VisualElement>("confirmation-modal").resolvedStyle.display == DisplayStyle.Flex, "send action opens confirmation");
            Check(tree.Q<VisualElement>(className: "modal-card").style.backgroundImage.value.sprite == theme.PanelSprite, "confirmation uses the shared frame");
            foreach (var size in Resolutions())
            {
                yield return CaptureDispatch(settings, "dispatch-confirm", size);
                CheckDispatchBounds(tree.Q<Button>("confirmation-accept-button"), tree.Q<VisualElement>(className: "modal-card"), "confirmation button stays in frame");
            }
            DispatchSubmit(tree.Q<Button>("confirmation-cancel-button"));
            yield return Frames(2);
            Check(!manager.HasActiveJob, "cancel does not dispatch");
            DispatchSubmit(tree.Q<Button>("dispatch-button"));
            DispatchSubmit(tree.Q<Button>("confirmation-accept-button"));
            yield return Frames(4);
            Check(manager.HasActiveJob, "confirmation starts the requested dispatch");
            Check(tree.Q<Button>("active-tab-button").style.backgroundImage.value.sprite == theme.PrimaryButtonSprite, "active tab uses selected artwork");
            yield return CaptureDispatch(settings, "dispatch-active", new Vector2Int(1920, 1080));
            var job = manager.ActiveJob;
            time.AdvanceTime(job.RequiredTime, GameTimeActivityType.Adventure);
            DispatchSubmit(tree.Q<Button>("report-tab-button"));
            yield return Frames(5);
            var claim = tree.Query<Button>("claim-button").First();
            Check(claim != null && claim.style.backgroundImage.value.sprite == theme.PrimaryButtonSprite, "new return report gets themed claim button");
            yield return CaptureDispatch(settings, "dispatch-report", new Vector2Int(1920, 1080));
            DispatchSubmit(claim);
            yield return Frames(4);
            Check(manager.ReturnedReports.Count == 0, "claim button delivers rewards and removes the report");
            var toast = tree.Q<Label>("toast-label");
            Check(toast.resolvedStyle.color.grayscale > .6f, "toast keeps readable light text");
            yield return CaptureDispatch(settings, "dispatch-claimed", new Vector2Int(1920, 1080));
            screen.enabled = false;
            screen.enabled = true;
            screen.Show();
            yield return Frames(4);
            Check(document.rootVisualElement.Q<VisualElement>(className: "ledger").style.backgroundImage.value.sprite == theme.PanelSprite, "reopening preserves the shared panel");
            screen.Hide();
            var target = settings.targetTexture;
            document.panelSettings = originalSettings;
            if (target != null) { target.Release(); Destroy(target); }
            Destroy(settings);
        }

        private static void DispatchClick(VisualElement element)
        {
            using var click = ClickEvent.GetPooled();
            click.target = element;
            element.SendEvent(click);
        }

        private static void DispatchSubmit(Button button)
        {
            using var submit = NavigationSubmitEvent.GetPooled();
            submit.target = button;
            button.SendEvent(submit);
        }

        private void CheckDispatchBounds(VisualElement element, VisualElement parent, string label)
        {
            Rect bounds = parent.worldBound;
            bounds.xMin -= 2; bounds.xMax += 2; bounds.yMin -= 2; bounds.yMax += 2;
            Check(bounds.Contains(element.worldBound.min) && bounds.Contains(element.worldBound.max), label);
        }

        private IEnumerator CaptureDispatch(PanelSettings settings, string name, Vector2Int size)
        {
            if (settings.targetTexture == null || settings.targetTexture.width != size.x || settings.targetTexture.height != size.y)
            {
                var old = settings.targetTexture;
                var target = new RenderTexture(size.x, size.y, 24, RenderTextureFormat.ARGB32);
                target.Create(); settings.targetTexture = target;
                if (old != null) { old.Release(); Destroy(old); }
            }
            yield return Frames(8);
            yield return new WaitForEndOfFrame();
            var previous = RenderTexture.active;
            RenderTexture.active = settings.targetTexture;
            var texture = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(_output, name + "-" + size.x + "x" + size.y + ".png"), texture.EncodeToPNG());
            Destroy(texture);
            Write("DISPATCH CAPTURE " + name + " " + size);
        }
    }
}
#endif
