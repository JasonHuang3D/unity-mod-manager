using UnityModManagerNet;
using Harmony12;
using System.Reflection;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using FairyGUI;
using System;

namespace Mod_PaintFu
{
    public class Settings : UnityModManager.ModSettings
    {

        public uint fuRate = 100;

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

            GUILayout.Label("画符品质", new GUILayoutOption[0]);
            GUILayout.Label((settings.fuRate).ToString() + "%", txtFieldStyle, GUILayout.Width(40));

            settings.fuRate = (uint)(GUILayout.HorizontalSlider(settings.fuRate, 1, 200, GUILayout.Width(200)));
            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
           
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

    }


    [HarmonyPatch(typeof(Wnd_FuPatinter), "OnSelectGong")]
    public static class Wnd_FuPatinter_OnSelectGong_Patch
    {
        public static void new_OnSelectGong(EventContext context)
        {

            GObject gobject = context.data as GObject;
            string text = (string)gobject.data;

            var _this = Traverse.Create(Wnd_FuPatinter.Instance);
            var _this_SelectName = _this.Field("SelectName");

     
            _this_SelectName.SetValue(text);

            _this.Field("UIInfo").Field("m_n62").Property("grayed").SetValue(false);
            _this.Field("UIInfo").Field("m_n63").Property("text").SetValue(string.Format("{0:P0}", Main.settings.fuRate/100.0f ));


            MapRender.Instance.PaintPanel.ResetPaint();
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            Main.Logger.Log(" Wnd_FuPatinter_OnSelectGong_Patch Transpiler init codes ");
            var codes = new List<CodeInstruction>(instructions);

            int startIndex = 0;


            var injectedCodes = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldsfld, typeof(Main).GetField("enabled")),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Beq_S, 6),

                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Call, typeof(Wnd_FuPatinter_OnSelectGong_Patch).GetMethod("new_OnSelectGong")),
                new CodeInstruction(OpCodes.Ret)
            };

            codes.InsertRange(startIndex, injectedCodes);


            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(Wnd_FuPatinter), "QuickP")]
    public static class Wnd_FuPatinter_QuickP_Patch
    {
        public static void new_QuickP()
        {
            var _this = Traverse.Create(Wnd_FuPatinter.Instance);

            _this.Field("CallBack").GetValue<Action<string, float, Texture2D, bool>>()?.Invoke(_this.Field("SelectName").GetValue<string>(), Main.settings.fuRate / 100.0f, null, false);

            Wnd_FuPatinter.Instance.Hide();
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            Main.Logger.Log(" Wnd_FuPatinter_QuickP_Patch Transpiler init codes ");
            var codes = new List<CodeInstruction>(instructions);
    
            int startIndex = 0;


            var injectedCodes = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldsfld, typeof(Main).GetField("enabled")),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Beq_S, 5),

                new CodeInstruction(OpCodes.Call, typeof(Wnd_FuPatinter_QuickP_Patch).GetMethod("new_QuickP")),
                new CodeInstruction(OpCodes.Ret)
            };

            codes.InsertRange(startIndex, injectedCodes);
    
    
            return codes.AsEnumerable();
        }
    }
}
