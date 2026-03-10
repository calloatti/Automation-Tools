using Bindito.Core;
using HarmonyLib;
using UnityEngine;

namespace Calloatti.AutoTools
{
  [Context("Game")]
  [Context("MapEditor")] // Good practice so it works in the map editor too!
  internal class PatchConfigurator : IConfigurator
  {
    private const string HarmonyId = "com.calloatti.autotools";
    private static Harmony _harmony;

    public void Configure(IContainerDefinition containerDefinition)
    {
      // The null check ensures we don't patch twice if a player loads a save, 
      // goes to the main menu, and loads another save.
      if (_harmony == null)
      {
        _harmony = new Harmony(HarmonyId);

        // 1. Apply all attribute-based patches automatically
        // This will find AutoRenamePatch (RenameIsolationPatch) and apply it.
        _harmony.PatchAll(typeof(PatchConfigurator).Assembly);

        // 2. Apply your manual Reflection-based patches
        PatchColorNameLabel.Apply(_harmony);

        Debug.Log($"[{HarmonyId}] All Harmony patches applied successfully!");
      }
    }
  }
}