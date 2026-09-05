using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Quartermaster
{

    internal sealed class QuartermasterScrollMarker : MonoBehaviour { }

    internal sealed class ConvoyScrollSizer : MonoBehaviour
    {
        internal RectTransform? Viewport;
        internal RectTransform? Content;
        internal ContributeToFaction? Menu;

        internal float MaxHeight;

        private const float Cadence = 0.5f;

        private float _nextMeasure;

        private void LateUpdate()
        {
            if (Viewport == null || Content == null) return;

            if (Menu != null && Time.unscaledTime >= _nextMeasure)
            {
                _nextMeasure = Time.unscaledTime + Cadence;

                if (Viewport.parent is RectTransform parent && parent != null)
                {

                    float measured = ConvoyListPanel.MeasureBudget(
                        Menu, Viewport, parent, announce: false, fallback: 0f);
                    if (measured > 0f) MaxHeight = measured;
                }
            }

            float wanted = LayoutUtility.GetPreferredHeight(Content);
            if (wanted <= 0f) return;

            float height = Mathf.Min(wanted, MaxHeight);
            if (Mathf.Abs(Viewport.sizeDelta.y - height) < 0.5f) return;

            Viewport.sizeDelta = new Vector2(Viewport.sizeDelta.x, height);
        }
    }

    internal sealed class ConvoySmoothScroll : MonoBehaviour,
                                               UnityEngine.EventSystems.IScrollHandler
    {
        internal ScrollRect? Scroll;

        internal float Step = 60f;

        private const float Remaining = 0.0001f;

        private float _target = -1f;

        public void OnScroll(UnityEngine.EventSystems.PointerEventData data)
        {
            if (Scroll == null || Scroll.content == null) return;

            float travel = Scroll.content.rect.height - Scroll.viewport.rect.height;
            if (travel <= 1f) return;

            if (_target < 0f) _target = Scroll.verticalNormalizedPosition;

            _target = Mathf.Clamp01(_target + data.scrollDelta.y * Step / travel);
        }

        private void LateUpdate()
        {
            if (Scroll == null || _target < 0f) return;

            float now = Scroll.verticalNormalizedPosition;

            if (Mathf.Abs(now - _target) < 0.0005f)
            {
                Scroll.verticalNormalizedPosition = _target;
                _target = -1f;
                return;
            }

            float t = 1f - Mathf.Pow(Remaining, Time.unscaledDeltaTime);
            Scroll.verticalNormalizedPosition = Mathf.Lerp(now, _target, t);
        }
    }

    internal static class ConvoyListPanel
    {
        private static readonly FieldInfo? BackgroundField =
            AccessTools.Field(typeof(ContributeToFaction), "convoySelectBackground");

        private static readonly string[] Furniture =
        {
            "currentFunds", "remainingFunds", "giveVehicleValue", "giveAirframeValue",
            "contributeValue", "contributeConfirm", "giveVehicleConfirm",
            "giveAirframesConfirm", "contributeSlider",
        };

        private const float ScrollSensitivity = 40f;

        private const float Margin = 8f;

        private const float MinBudget = 60f;

        internal static void ClearOldButtons(ContributeToFaction menu)
        {
            if (menu == null || BackgroundField == null) return;
            if (BackgroundField.GetValue(menu) is not Transform background || background == null) return;

            int removed = 0;
            for (int i = background.childCount - 1; i >= 0; i--)
            {
                Transform child = background.GetChild(i);
                if (child.GetComponent<ConvoyPurchaseOption>() == null) continue;

                Object.Destroy(child.gameObject);
                removed++;
            }

            if (removed > 0)
                QuartermasterPlugin.Diag(
                    $"{removed} convoy button(s) from a previous pass were destroyed before the "
                    + "list was rebuilt, so they cannot stack.");
        }

        internal static void MakeScrollable(ContributeToFaction menu)
        {
            if (menu == null) return;

            if (BackgroundField == null)
            {
                QuartermasterPlugin.Log.LogWarning(
                    "ContributeToFaction has no 'convoySelectBackground' field on this game "
                    + "version, so the convoy list cannot be made to scroll. The options are "
                    + "still added; a long list will be clipped.");
                return;
            }

            if (BackgroundField.GetValue(menu) is not Transform background || background == null)
                return;

            if (background.GetComponent<QuartermasterScrollMarker>() != null) return;

            if (background is not RectTransform content)
            {
                QuartermasterPlugin.Log.LogWarning(
                    "The convoy list's container is not a RectTransform, so it was left alone.");
                return;
            }

            if (content.parent is not RectTransform parent || parent == null) return;

            if (parent.GetComponent<ScrollRect>() != null)
            {
                FitContent(content);
                background.gameObject.AddComponent<QuartermasterScrollMarker>();
                QuartermasterPlugin.Diag(
                    "The convoy list was already in a scroll view; only its content size was fitted.");
                return;
            }

            var viewport = new GameObject(
                "Quartermaster_ConvoyViewport", typeof(RectTransform), typeof(RectMask2D));

            var viewportRect = (RectTransform)viewport.transform;
            viewportRect.SetParent(parent, worldPositionStays: false);
            viewportRect.SetSiblingIndex(content.GetSiblingIndex());

            viewportRect.anchorMin = content.anchorMin;
            viewportRect.anchorMax = content.anchorMax;
            viewportRect.pivot = content.pivot;
            viewportRect.anchoredPosition = content.anchoredPosition;
            viewportRect.sizeDelta = content.sizeDelta;
            viewportRect.localScale = content.localScale;
            viewportRect.localRotation = content.localRotation;

            LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
            PinToTopLeft(viewportRect);

            float budget = MeasureBudget(menu, viewportRect, parent);

            content.SetParent(viewportRect, worldPositionStays: false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.offsetMin = new Vector2(0f, content.offsetMin.y);
            content.offsetMax = new Vector2(0f, content.offsetMax.y);

            bool fitted = FitContent(content);

            ScrollRect scroll = viewport.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            scroll.scrollSensitivity = 0f;
            scroll.inertia = false;

            ConvoySmoothScroll smooth = viewport.AddComponent<ConvoySmoothScroll>();
            smooth.Scroll = scroll;
            smooth.Step = ScrollSensitivity;

            if (fitted)
            {
                ConvoyScrollSizer sizer = viewport.AddComponent<ConvoyScrollSizer>();
                sizer.Viewport = viewportRect;
                sizer.Content = content;
                sizer.Menu = menu;
                sizer.MaxHeight = budget;
            }

            background.gameObject.AddComponent<QuartermasterScrollMarker>();

            QuartermasterPlugin.Log.LogInfo(
                fitted
                    ? $"The convoy buy list grows with its entries up to {budget:0} px and "
                      + "scrolls past that."
                    : "The convoy buy list scrolls over the box the game gave it. It cannot "
                      + "grow: its container has no layout group to ask.");
        }

        internal static float MeasureBudget(
            ContributeToFaction menu, RectTransform viewportRect, RectTransform parent,
            bool announce = true, float fallback = float.NaN)
        {
            if (float.IsNaN(fallback)) fallback = viewportRect.rect.height;

            float forced = QuartermasterPlugin.BuyListHeight;

            if (forced > 0f)
            {
                if (announce)
                    QuartermasterPlugin.Log.LogInfo(
                        $"BuyListHeight is set to {forced:0} px, so the convoy list uses that "
                        + "instead of the room measured in the menu.");

                return forced;
            }

            float listTop = TopIn(viewportRect, parent);
            float highestBelow = float.NegativeInfinity;
            string named = "";

            foreach (string field in Furniture)
            {
                FieldInfo? info = AccessTools.Field(typeof(ContributeToFaction), field);
                if (info == null) continue;

                if (info.GetValue(menu) is not Component component || component == null) continue;
                if (component.transform is not RectTransform rect) continue;

                if (!component.gameObject.activeInHierarchy) continue;

                if (rect.IsChildOf(viewportRect)) continue;

                float top = TopIn(rect, parent);
                if (top >= listTop) continue;

                if (top > highestBelow)
                {
                    highestBelow = top;
                    named = field;
                }
            }

            if (float.IsNegativeInfinity(highestBelow))
            {
                if (announce)
                    QuartermasterPlugin.Diag(
                        "Nothing sits below the convoy list, so it keeps the height the game "
                        + "gave it.");

                return fallback;
            }

            float budget = listTop - highestBelow - Margin;

            if (budget < MinBudget)
            {
                if (announce)
                    QuartermasterPlugin.Diag(
                        $"Only {budget:0} px above '{named}', under the {MinBudget:0} px floor, "
                        + $"so the container's own {fallback:0} px was kept. The list may "
                        + "overlap. Set BuyListHeight to override.");

                return fallback;
            }

            if (announce)
                QuartermasterPlugin.Diag(
                    $"The convoy list has {budget:0} px of room before '{named}'; its container "
                    + $"was drawn at {fallback:0} px. Set BuyListHeight to give it more.");

            return budget;
        }

        private static float TopIn(RectTransform rect, RectTransform space)
        {
            Rect r = rect.rect;
            Vector3 world = rect.TransformPoint(new Vector3(r.xMin, r.yMax, 0f));
            return space.InverseTransformPoint(world).y;
        }

        private static void PinToTopLeft(RectTransform rect)
        {
            if (rect.parent is not RectTransform parent || parent == null) return;

            Rect r = rect.rect;
            Vector3 world = rect.TransformPoint(new Vector3(r.xMin, r.yMax, 0f));
            Vector2 local = parent.InverseTransformPoint(world);
            var parentTopLeft = new Vector2(parent.rect.xMin, parent.rect.yMax);

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(r.width, r.height);
            rect.anchoredPosition = local - parentTopLeft;
        }

        private static bool FitContent(RectTransform content)
        {
            if (content.GetComponent<LayoutGroup>() == null)
            {
                QuartermasterPlugin.Diag(
                    "The convoy list's container has no layout group, so its height was left as "
                    + "the game set it.");
                return false;
            }

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = content.gameObject.AddComponent<ContentSizeFitter>();

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutRebuilder.MarkLayoutForRebuild(content);
            return true;
        }
    }
}
