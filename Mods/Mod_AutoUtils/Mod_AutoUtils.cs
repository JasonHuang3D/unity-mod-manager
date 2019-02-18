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

        public bool showLing = true;

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

            settings.showLing = GUILayout.Toggle(settings.showLing, "显示灵气数值", new GUILayoutOption[0]);
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

    [HarmonyPatch(typeof(Wnd_GameMain), "OnInit")]
    public static class Wnd_GameMain_OnInit_Patch
    {
        public static void Postfix(Wnd_GameMain __instance)
        {
            __instance.UIInfo.m_n32.height = 300;
        }
    }

    [HarmonyPatch(typeof(Wnd_GameMain), "UpdateGridInfo")]
    public static class Wnd_GameMain_UpdateGridInfo_Patch
    {
        private static void calcGridLingImpact(int gridKey, out float baseJulingValue, out float baseLingImpact)
        {

            Map map = WorldMgr.Instance.curWorld.map;

            float baseLingValue = map.Ling.GetLing(gridKey);
            baseJulingValue = map.Effect.GetEffect(gridKey, XiaWorld.g_emMapEffectKind.LingAddion, 0f, false);
            float dBaseLing = baseLingValue - baseJulingValue;

            float sumBasePredictLingValue = baseLingValue;
            float sumLingAttenuation = 0.0f;

            List<Thing> thingsAtBaseGrid = map.Things.GetThingsAtGrid(gridKey);
            var listThingsImpactLing = new List<Thing>();
            if (thingsAtBaseGrid != null)
            {
                foreach (Thing thing in thingsAtBaseGrid)
                {
                    if (thing != null && thing.def != null && thing.AtG && thing.Attenuation > 0f && thing.Absorption > 0f && thing.Accommodate > 0f && thing.IsValid && thing.CheckWorkingAfterPutdown())
                    {
                        sumLingAttenuation += thing.Attenuation;
                        listThingsImpactLing.Add(thing);
                    }
                }
            }
          
            TerrainDef terrainAtBaseGrid = map.Terrain.GetTerrain(gridKey);
            if (terrainAtBaseGrid.Attenuation > 0.0f)
                sumLingAttenuation += terrainAtBaseGrid.Attenuation;

            List<int> neighborKeys = GridMgr.Inst.GetNeighbor(gridKey);

            for (int i = 0; i < neighborKeys.Count; i++)
            {
                int neighborKey = neighborKeys[i];
                float neighborLingValue = map.Ling.GetLing(neighborKey);

                float dNeighborLing = neighborLingValue - map.Effect.GetEffect(neighborKey, g_emMapEffectKind.LingAddion, 0.0f, true);

                if (dNeighborLing < 1.0f || dBaseLing >= dNeighborLing) continue;

                float addLingValue = 0.0f;
                float dNeighborBaseLing = dNeighborLing - dBaseLing;
                if (dNeighborBaseLing >= 15.6f)
                    addLingValue = dNeighborBaseLing / 1.3f;
                else if (dNeighborBaseLing >= 2.4f)
                    addLingValue = dNeighborBaseLing / 1.6f;
                else if (dNeighborBaseLing >= 1.8f)
                    addLingValue = Mathf.Pow(2.0f, dNeighborBaseLing * 5.0f - 10.0f) / 5.0f;
                else if (dNeighborBaseLing >= 0.4f)
                    addLingValue = 0.2f;
                else
                    addLingValue = 0.0f;

                float lostLingValue = 0.0f;
                float tempBaseLing = sumBasePredictLingValue;

                foreach (Thing thing in listThingsImpactLing)
                {
                    if (tempBaseLing > 0f)
                    {
                        float thingAbsorpt = Mathf.Min(tempBaseLing, Mathf.Max(tempBaseLing - thing.LingV / thing.Accommodate, (float)thing.TempRate) * thing.Absorption * 0.004f);
                        tempBaseLing -= thingAbsorpt;
                        lostLingValue += thingAbsorpt;
                    }
                }
                //there is no impact of NPCs, since npc's absorption only calculate once per update frame. When accessing this function, Npc's impact  has already been calculated.
                sumBasePredictLingValue = Mathf.Max(0.0f, sumBasePredictLingValue + addLingValue * Mathf.Max(1.0f - sumLingAttenuation, 0.0f) - lostLingValue);
            }


            baseLingImpact = sumBasePredictLingValue - baseLingValue;
        }

        public static void new_UpdateGridInfo(Wnd_GameMain __instance)
        {
            if (UI_WorldLayer.Instance == null)
                return;

            int mouseGridKey = UI_WorldLayer.Instance.MouseGridKey;

            var _this = Traverse.Create(__instance);

            if (_this.Field("lastkey").GetValue<int>() == mouseGridKey)
                return;

            _this.Field("lastkey").SetValue(mouseGridKey);

            Map map = WorldMgr.Instance.curWorld.map;
            if (GridMgr.Inst.KeyVaild(mouseGridKey))
            {
                if (!map.IsInDark(mouseGridKey, true))
                {
                    float temperature = map.GetTemperature(mouseGridKey);
                    string terrainName = map.Terrain.GetTerrainName(mouseGridKey, true);
                    TerrainDef terrain = map.Terrain.GetTerrain(mouseGridKey);

                    string lingQiString = "";
                    if (Main.settings.showLing)
                    {

                        calcGridLingImpact(mouseGridKey,  out float baseJulingValue, out float baseLingImpact);

                        float baseLingEffectedValue = map.GetLing(mouseGridKey);

                        lingQiString = string.Format("灵气值:{0}\n聚灵值:{1}\n灵气增幅:{2}", new object[]
                        {
                            baseLingEffectedValue.ToString(),
                            baseJulingValue.ToString(),
                            baseLingImpact.ToString()
                        });

                    }

                    __instance.UIInfo.m_n32.text = string.Format("{0}{8}{6}\n{1}\n{2}\n{4}\n{3}{5}({7:f1}℃)\n{9}", new object[]
                   {
                        map.Terrain.GetTerrainName(mouseGridKey, false),
                        XiaWorld.GameDefine.GetValueByMap<string>(XiaWorld.GameDefine.FertilityDesc, map.GetFertility(mouseGridKey)),
                        XiaWorld.GameDefine.GetValueByMap<string>(XiaWorld.GameDefine.BeautyDesc, map.GetBeauty(mouseGridKey, true)),
                        XiaWorld.GameDefine.GetValueByMap<string>(XiaWorld.GameDefine.TemperatureDesc, temperature),
                        XiaWorld.GameDefine.GetValueByMap<string>(XiaWorld.GameDefine.LightDesc, map.GetLight(mouseGridKey)),
                        (XiaWorld.AreaMgr.Instance.CheckArea(mouseGridKey, "Room") == null) ? string.Empty : "(室)",
                        (!string.IsNullOrEmpty(terrainName)) ? ("(" + terrainName + ")") : string.Empty,
                        temperature,
                        (!terrain.IsWater || map.Snow.GetSnow(mouseGridKey) < 200) ? string.Empty : "(冰)",
                        lingQiString
                   });
                }
                else
                {
                    __instance.UIInfo.m_n32.text = "未探索";
                }
            }
            else
            {
                __instance.UIInfo.m_n32.text = "未探索";
            }
            if (__instance.openFengshui && GridMgr.Inst.KeyVaild(mouseGridKey))
            {
                float[] elementProportion = map.GetElementProportion(mouseGridKey);
                for (int i = 1; i < elementProportion.Length - 1; i++)
                {
                    __instance.UIInfo.m_ElementShow.GetChild("E" + (i - 1)).asProgress.value = (double)(elementProportion[i] * 100f);
                }
            }
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            Main.Logger.Log(" Wnd_GameMain_UpdateGridInfo_Patch Transpiler init codes ");
            var codes = new List<CodeInstruction>(instructions);

            int startIndex = 0;


            var injectedCodes = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, typeof(Wnd_GameMain_UpdateGridInfo_Patch).GetMethod("new_UpdateGridInfo")),
                new CodeInstruction(OpCodes.Ret)
            };

            codes.InsertRange(startIndex, injectedCodes);


            return codes.AsEnumerable();
        }
    }
}

