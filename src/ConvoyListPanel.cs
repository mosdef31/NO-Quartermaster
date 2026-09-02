using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Quartermaster
{

    internal sealed class QuartermasterScrollMarker : MonoBehaviour { }

    internal static class ConvoyListPanel
    {
        private static readonly FieldInfo? BackgroundField =
            AccessTools.Field(typeof(ContributeToFaction), "convoySelectBackground");

        private const float ScrollSensitivity = 40f;

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

            Transform parent = content.parent;
            if (parent == null) return;

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

            content.SetParent(viewportRect, worldPositionStays: false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.offsetMin = new Vector2(0f, content.offsetMin.y);
            content.offsetMax = new Vector2(0f, content.offsetMax.y);

            FitContent(content);

            ScrollRect scroll = viewport.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = ScrollSensitivity;
            scroll.inertia = false;

            background.gameObject.AddComponent<QuartermasterScrollMarker>();

            QuartermasterPlugin.Log.LogInfo(
                "The convoy buy list is now scrollable, so a list longer than the panel can be "
                + "reached with the mouse wheel.");
        }

        private static void FitContent(RectTransform content)
        {
            if (content.GetComponent<LayoutGroup>() == null)
            {
                QuartermasterPlugin.Diag(
                    "The convoy list's container has no layout group, so its height was left as "
                    + "the game set it.");
                return;
            }

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = content.gameObject.AddComponent<ContentSizeFitter>();

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutRebuilder.MarkLayoutForRebuild(content);
        }
    }
}
