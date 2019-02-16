using UnityModManagerNet;
using Harmony12;
using System.Reflection;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using FairyGUI;
using System;

using XiaWorld;

namespace Mod_AutoUtils
{
    public class Settings : UnityModManager.ModSettings
    {

        public bool autoSearchBodies = true;
        public bool autoMoveBodies = true;
        public bool autoSlaughtAnimals = true;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

    }

    public static class Main
    {
        public static bool enabled;
        public static Settings settings;
        public static UnityModManager.ModEntry.ModLogger Logger;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            settings = Settings.Load<Settings>(modEntry);

            Logger = modEntry.Logger;

            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            return true;
        }

        public static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            if (!value)
                return false;

            enabled = value;

            return true;
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUIStyle txtFieldStyle = GUI.skin.textField;
            txtFieldStyle.alignment = TextAnchor.MiddleCenter;

            GUILayout.BeginHorizontal();

            settings.autoSearchBodies = GUILayout.Toggle(settings.autoSearchBodies, "自动搜索尸体", new GUILayoutOption[0]);
            GUILayout.Space(10);

            settings.autoMoveBodies = GUILayout.Toggle(settings.autoMoveBodies, "自动搬运尸体到傀儡停放处", new GUILayoutOption[0]);
            GUILayout.Space(10);

            settings.autoSlaughtAnimals = GUILayout.Toggle(settings.autoSlaughtAnimals, "自动屠宰动物", new GUILayoutOption[0]);
            GUILayout.Space(10);

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();

        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

    }

    [HarmonyPatch(typeof(Npc), "DoDeath")]
    public static class Npc_DoDeath_Patch
    {
        public static void Postfix(Npc __instance)
        {
            if (Main.settings.autoSlaughtAnimals && !__instance.IsPlayerThing && __instance.Race.RaceType != g_emNpcRaceType.Wisdom && __instance.IsDeath)
            {
                __instance.AddCommand("Slaughter", new object[0]);
                return;
            }

            if (Main.settings.autoSearchBodies && (!__instance.IsPlayerThing || __instance.IsVistor) && !__instance.CanDoAction && __instance.Race.RaceType == g_emNpcRaceType.Wisdom)
            {
                __instance.AddCommand("Seach", new object[0]);

            }

            if (Main.settings.autoMoveBodies && (!__instance.IsPlayerThing || __instance.IsCorpse) && !__instance.CanDoAction)
            {
                BuildingThing rallyPointBuilding = __instance.map.Things.FindBuilding(__instance, 9999, null, 0, false, true, 0, 9999, null, "PuppetPlace", true);
                if (rallyPointBuilding != null)
                {
                    __instance.AddCommand("MoveNpc", new object[]
                    {
                        rallyPointBuilding.Key
                    });
                }
            }
        }
    }
}

