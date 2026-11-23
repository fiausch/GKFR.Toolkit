using HarmonyLib;
using TMPro;
using UnityEngine;

namespace GKFR.ModLoader
{
    [HarmonyPatch(typeof(MenuHDMain), nameof(MenuHDMain.Enter))]
    static class MenuHint
    {
        const string HintName = "GKFR_ModLoaderHint";

        static void Postfix(MenuHDMain __instance)
        {
            Transform existing = __instance.transform.Find(HintName);
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return;
            }

            TextMeshProUGUI template = __instance.GetComponentInChildren<TextMeshProUGUI>(true);
            if (template == null)
            {
                return;
            }

            var hintObject = new GameObject(HintName, typeof(RectTransform));
            hintObject.transform.SetParent(__instance.transform, false);
            var rect = hintObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0.45f, 0.08f);
            rect.offsetMin = new Vector2(24f, 12f);
            rect.offsetMax = new Vector2(0f, 0f);

            TextMeshProUGUI label = hintObject.AddComponent<TextMeshProUGUI>();
            label.font = template.font;
            label.fontSharedMaterial = template.fontSharedMaterial;
            label.fontSize = Mathf.Max(22f, template.fontSize * 0.55f);
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.BottomLeft;
            label.color = new Color(1f, 0.78f, 0.18f, 1f);
            label.enableWordWrapping = true;
            label.raycastTarget = false;
            label.text = "F1 for the mod loader";
        }
    }
}
