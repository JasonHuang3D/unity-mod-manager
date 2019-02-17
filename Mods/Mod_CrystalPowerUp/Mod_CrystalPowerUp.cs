using UnityModManagerNet;
using Harmony12;
using System.Reflection;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using System;

using XiaWorld;

namespace Mod_CrystalPowerUp
{
    public class Settings : UnityModManager.ModSettings
    {


        public uint youRate = 100;
        public bool youAllowStack = false;
        public bool youToMax = false;

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


            GUILayout.Label("幽淬概率", new GUILayoutOption[0]);
            GUILayout.Label((settings.youRate).ToString() + "%", txtFieldStyle, GUILayout.Width(40));
            settings.youRate = (uint)(GUILayout.HorizontalSlider(settings.youRate, 1, 100, GUILayout.Width(200)));
            GUILayout.Space(10);

            settings.youAllowStack = GUILayout.Toggle(settings.youAllowStack, "允许幽淬物品叠加", new GUILayoutOption[0]);
            GUILayout.Space(10);

            settings.youToMax = GUILayout.Toggle(settings.youToMax, "一次淬到满阶", new GUILayoutOption[0]);
            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();

        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

    }

    [HarmonyPatch(typeof(ItemThing), "SoulCrystalYouPowerUp")]
    public static class ItemThing_SoulCrystalYouPowerUp_Patch
    {
        public static bool new_SoulCrystalYouPowerUp(ItemThing __instance, float someFloat)
        {
            if (__instance.Rate >= 12) return false;

            float rate = Main.settings.youToMax ? 1.0f : Main.settings.youRate / 100.0f;

            if (World.RandomRate(rate))
            {
                ItemThing target = __instance;
                if (!Main.settings.youAllowStack && __instance.Count > 1)
                {
                    target = __instance.Split(1, true);
                    __instance.map.DropItem(target, __instance.Key, true, true, true, false, 0f);
                    GameObject gameObject = UnityEngine.Object.Instantiate(Resources.Load("Effect/System/FlyLine")) as GameObject;
                    FlyLineRender component = gameObject.GetComponent<FlyLineRender>();
                    component.Begin(__instance.Pos, target.Pos, 0.2f, null);
                }

                int times = Main.settings.youToMax ? Math.Max(1, 12 - target.Rate) : 1;

                for (int i = 0; i < times; i++)
                {
                    target.YouPower++;
                    target.Rate++;
                }

                if (target.View != null && target.Rate >= 3)
                {
                    target.View.ShowItemRay(new Color?(GameDefine.GetRateColor(target.Rate)));
                    target.NeedClick = true;
                }
                GameWatch.Instance.PlayUIAudio("Sound/ding");
                return true;
            }

            return false;
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            Main.Logger.Log(" ItemThing_SoulCrystalYouPowerUp_Patch Transpiler init codes ");
            var codes = new List<CodeInstruction>(instructions);

            int startIndex = 0;


            var injectedCodes = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Call, typeof(ItemThing_SoulCrystalYouPowerUp_Patch).GetMethod("new_SoulCrystalYouPowerUp")),
                new CodeInstruction(OpCodes.Ret)
            };

            codes.InsertRange(startIndex, injectedCodes);


            return codes.AsEnumerable();
        }
    }
}
