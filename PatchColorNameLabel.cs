using HarmonyLib;
using Timberborn.Illumination;
using Timberborn.AutomationBuildings;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Reflection;

namespace Calloatti.AutoTools
{
  public static class PatchColorNameLabel
  {
    private static Label _colorNameLabel;

    private static readonly Dictionary<Color32, string> ColorNames = new Dictionary<Color32, string>
    {
      { new Color32(217, 125, 32, 255), "Default (Amber)" },
      { new Color32(41, 150, 204, 255), "Bot Blue" },
      { new Color32(133, 189, 204, 255), "Automator Blue" },
      { new Color32(31, 204, 31, 255), "Standard Green" },
      { new Color32(204, 50, 0, 255), "Standard Orange" },
      { new Color32(230, 0, 0, 255), "Standard Red" },
      { new Color32(18, 22, 26, 255), "Dimmed / Off" },
      { new Color32(255, 255, 255, 255), "Pure White" },
      { new Color32(255, 20, 135, 255), "Neon Rose" },
      { new Color32(255, 10, 186, 255), "Hot Pink" },
      { new Color32(255, 0, 255, 255), "Fuchsia" },
      { new Color32(209, 0, 255, 255), "Electric Purple" },
      { new Color32(150, 0, 255, 255), "Violet" },
      { new Color32(79, 0, 255, 255), "Indigo" },
      { new Color32(26, 64, 255, 255), "Royal Blue" },
      { new Color32(0, 125, 255, 255), "Electric Blue" },
      { new Color32(0, 181, 255, 255), "Azure" },
      { new Color32(0, 255, 255, 255), "Cyan" },
      { new Color32(0, 255, 191, 255), "Turquoise" },
      { new Color32(26, 255, 145, 255), "Aqua" },
      { new Color32(64, 255, 105, 255), "Mint Green" },
      { new Color32(5, 255, 84, 255), "Spring Green" },
      { new Color32(115, 255, 0, 255), "Vibrant Lime" },
      { new Color32(171, 255, 0, 255), "Chartreuse" },
      { new Color32(255, 255, 31, 255), "Lemon Yellow" },
      { new Color32(255, 209, 0, 255), "Golden Yellow" },
      { new Color32(255, 140, 36, 255), "Coral" },
      { new Color32(255, 105, 79, 255), "Salmon Pink" },
      { new Color32(255, 69, 125, 255), "Rose" },
      { new Color32(235, 115, 255, 255), "Lavender" },
      { new Color32(255, 46, 204, 255), "Magenta" },
      { new Color32(255, 36, 99, 255), "Raspberry" },
    };

    public static void Apply(Harmony harmony)
    {
      var targetType = AccessTools.TypeByName("Timberborn.IlluminationUI.CustomizableIlluminatorFragment");
      if (targetType == null) return;

      var initMethod = AccessTools.Method(targetType, "InitializeFragment");
      var updateColorMethod = AccessTools.Method(targetType, "UpdateCustomColor");

      harmony.Patch(initMethod, postfix: new HarmonyMethod(typeof(PatchColorNameLabel), nameof(InitializeLabel)));
      harmony.Patch(updateColorMethod, postfix: new HarmonyMethod(typeof(PatchColorNameLabel), nameof(UpdateLabelText)));
    }

    private static void InitializeLabel(object __instance, VisualElement __result)
    {
      var rgbField = __result.Q<TextField>("Rgb");
      var rgbContainer = rgbField?.parent;
      if (rgbContainer == null) return;

      rgbContainer.style.flexDirection = FlexDirection.Column;
      rgbContainer.style.alignItems = Align.Center;
      rgbField.style.width = new Length(100, LengthUnit.Pixel);
      rgbField.style.marginBottom = 0;

      _colorNameLabel = new Label("Selected Color");
      _colorNameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
      _colorNameLabel.style.marginTop = -4;
      _colorNameLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
      rgbContainer.Add(_colorNameLabel);
    }

    private static void UpdateLabelText(object __instance)
    {
      if (_colorNameLabel == null) return;

      var field = __instance.GetType().GetField("_customizableIlluminator", BindingFlags.NonPublic | BindingFlags.Instance);
      var illuminator = field?.GetValue(__instance) as CustomizableIlluminator;

      if (illuminator == null) return;

      Color32 currentColor = illuminator.CustomColor;

      if (ColorNames.TryGetValue(currentColor, out string name))
      {
        _colorNameLabel.text = name;
      }
      else
      {
        _colorNameLabel.text = "Custom Hex Color";
      }
    }
  }
}