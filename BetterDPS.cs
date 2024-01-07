using MelonLoader;
using BTD_Mod_Helper;
using BetterDPS;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Simulation.Objects;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using BTD_Mod_Helper.Extensions;
using UnityEngine;
using System.Linq;
using BTD_Mod_Helper.Api.Components;
using Il2CppTMPro;
using BTD_Mod_Helper.Api.Enums;
using System.Collections.Generic;
using System;
using Il2CppAssets.Scripts;
using BTD_Mod_Helper.Api.ModOptions;
using Il2CppAssets.Scripts.Unity.UI_New.Popups;
using Il2CppAssets.Scripts.Unity;

[assembly: MelonInfo(typeof(BetterDPS.BetterDPS), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace BetterDPS;

public class BetterDPS : BloonsTD6Mod
{
    public override void OnApplicationStart()
    {
        ModHelper.Msg<BetterDPS>("BetterDPS loaded!");
    }
    private double count = 0;
    private bool showing = false;

    private ModHelperPanel? globaldisplay = null;
    private ModHelperButton? infobutton = null;
    private ModHelperText? globaldps = null;
    private ModHelperText? globalavg = null;
    private ModHelperText? globalmax = null;
    private ModHelperText? title = null;

    private Vector3? oldPosBasic = null, oldPosAdvanced = null;

    private Dictionary<ObjectId, List<int>> highDPST = new();
    private Dictionary<ObjectId, long> oldDamage = new();

    private ModSettingHotkey ResetPosition = new(KeyCode.F12);

    private int maxRecord = 5;

    private bool inRound = false;
    private bool draggable = true;

    private float lastClickTime;
    private float doubleClickThreshold = 0.5f;
    private bool isWaitingForDoubleClick;

    private double time = 1.0;

    public override void OnUpdate()
    {
        double dt = Time.deltaTime;

        if (InGame.instance != null && InGame.instance.bridge != null)
        {

            UpdateInputs();

            UpdateText();

            if (InGame.instance.GetTowers().Count > 0)
            {
                count += dt;
            }
            else
            {
                oldDamage.Clear();
                highDPST.Clear();
            }

            if (!showing)
            {
                CreateBasicUI();
                showing = true;
            }
            if (count >= time)
            {
                UpdateDamageNumbers();
                count = 0;
            }

            UpdateUIPosition();
        } else
        {
            showing = false;
            highDPST.Clear();
            oldDamage.Clear();
        }
        base.OnUpdate();
    }
    private void UpdateUIPosition()
    {
        if (globaldisplay != null)
        {
            if (draggable) UpdateDraggableComponent(globaldisplay, ref oldPosAdvanced);
        }
        else if (globaldps != null)
        {
           if (draggable) UpdateDraggableComponent(globaldps, ref oldPosBasic);

            var cashText = GameObject.Find("Cash");

            if (oldPosBasic == null && cashText != null)
            {
                globaldps.transform.position = cashText.transform.position + Vector3.right * 140;
            }
        }
        if (oldPosAdvanced == null && globaldisplay != null)
        {
            globaldisplay.transform.position = new Vector2(Screen.width / 2, Screen.height / 2);
        }
    }
    private void UpdateDamageNumbers()
    {
        foreach (var t in InGame.instance.GetTowers())
        {
            if (highDPST.ContainsKey(t.Id) && oldDamage.ContainsKey(t.Id))
            {
                if (oldDamage[t.Id] > t.damageDealt)
                {
                    oldDamage.Remove(t.Id);
                    continue;
                }
                if (inRound)
                {
                    highDPST[t.Id].Add((int)(t.damageDealt - oldDamage[t.Id]));
                }

                if (maxRecord > 0)
                {
                    if (highDPST[t.Id].Count > maxRecord)
                    {
                        for (int i = 0; i <= (highDPST[t.Id].Count - maxRecord); ++i)
                        {
                            highDPST[t.Id].RemoveAt(i);
                        }
                    }
                }
            }
            else
            {
                if (!highDPST.ContainsKey(t.Id)) highDPST.Add(t.Id, new() { 0 });
                if (!oldDamage.ContainsKey(t.Id)) oldDamage.Add(t.Id, t.damageDealt);
            }
        }
        foreach (var t in InGame.instance.GetTowers())
        {
            oldDamage[t.Id] = t.damageDealt;
        }
    }
    private void CreateBasicUI()
    {
        if (globaldisplay != null) globaldisplay.DeleteObject();

        Info info = new("DPS:InfoButton", -470, 0, 100, 100);
        Action showDisplay = CreateAdvancedUI;
        infobutton = ModHelperButton.Create(info, VanillaSprites.InfoBtn2, showDisplay);
        globaldps = ModHelperText.Create(new("DPS:GlobalDPS", 0, 0, 800, 200), "DPS: 0", 100, TextAlignmentOptions.Left);
        globaldps.Text.alignment = TextAlignmentOptions.Left;
        
        InGame.instance.GetInGameUI().AddModHelperComponent(globaldps);
        
        globaldps.transform.position = (Vector3)(oldPosBasic != null ? oldPosBasic : globaldps.transform.position);

        globaldps.AddModHelperComponent(infobutton);
    }
    private void CreateAdvancedUI()
    {
        globaldps.DeleteObject();
        infobutton.DeleteObject();

        string u = maxRecord > 0 ? $"{maxRecord}s" : "Round";

        globaldisplay = ModHelperPanel.Create(new("DPS:GlobalDisplay", 0, 0, 800, 560), VanillaSprites.MainBgPanel);
        globaldps = ModHelperText.Create(new("DPS:GlobalDPS", -70, 110, 500, 200), "DPS:   0", 60, TextAlignmentOptions.Left);
        globalavg = ModHelperText.Create(new("DPS:GlobalAVG", -70, 10, 500, 200), $"AVG({u}):  0", 60, TextAlignmentOptions.Left);
        globalmax = ModHelperText.Create(new("DPS:GlobalMAX", -70, -90, 500, 200), $"MAX({u}): 0", 60, TextAlignmentOptions.Left);
        title = ModHelperText.Create(new("DPS:Title", 100, 210, 700, 100), "Global Stats", 80, TextAlignmentOptions.Top);

        globaldps.Text.alignment = TextAlignmentOptions.Left;
        globalavg.Text.alignment = TextAlignmentOptions.Left;
        globalmax.Text.alignment = TextAlignmentOptions.Left;
        title.Text.alignment = TextAlignmentOptions.Left;

        Info info = new("DPS:CollapseButton", -350, 230, 150, 150);
        Action action = CreateBasicUI;
        var killButton = ModHelperButton.Create(info, VanillaSprites.CloseBtn, action);

        InGame.instance.GetInGameUI().AddModHelperComponent(globaldisplay);

        Action[] actions = { CustomTimePeriod, new Action(() => maxRecord = -1) };
        string[] texts = { "Set", "Round" };
        for (int i = 0; i < 2; ++i)
        {
            var j = new Info($"DPS:{actions[i].Method.Name}Button", -250 + i * 200, -200, 140 + i * 70, 100);
            var k = new Info($"DPS:{actions[i].Method.Name}ButtonText", 0, 0, 140, 100);
            var b = ModHelperButton.Create(j, VanillaSprites.RedBtn, actions[i]);
            b.AddText(k, texts[i], 40, TextAlignmentOptions.Midline);
            
            globaldisplay.AddModHelperComponent(b);
        }

        if (oldPosAdvanced != null) globaldisplay.transform.position = (Vector3)(oldPosAdvanced); 
        globaldisplay.AddModHelperComponent(globaldps);
        globaldisplay.AddModHelperComponent(globalmax);
        globaldisplay.AddModHelperComponent(globalavg);
        globaldisplay.AddModHelperComponent(title);
        globaldisplay.AddModHelperComponent(killButton);
    }
    private void CustomTimePeriod()
    {
        PopupScreen.instance.ShowSetValuePopup(
            "Custom Time Period",
            "The amount of seconds the average and maximum dps are calculated over. Ranges from 1-120",
            new Action<int>(i => maxRecord = Math.Clamp(i, 1, 120)),
            maxRecord
            );
    }
    public override void OnRoundStart()
    {
        inRound = true;
        if (maxRecord == -1)
            highDPST.Clear();
        base.OnRoundStart();
    }
    public override void OnRoundEnd()
    {
        inRound = false;
        base.OnRoundEnd();
    }
    private void UpdateText()
    {
        var dpspre = "DPS: ";
        string u = maxRecord > 0 ? $"{maxRecord}s" : "Round";
        var avgpre = $"AVG ({u}): ";
        var maxpre = $"MAX ({u}): ";
        if (InGame.instance.inputManager.SelectedTower != null)
        {
            var selected = InGame.instance.inputManager.SelectedTower.tower;
            if (globaldps != null) globaldps.SetText(dpspre + (Math.Max(selected.damageDealt - oldDamage[selected.Id], 0)).ToString());
            if (globaldisplay != null)
            {
                globalavg.SetText(avgpre + (Math.Max((int)(highDPST[selected.Id].Average()), 0)).ToString());
                globalmax.SetText(maxpre + Math.Max(highDPST[selected.Id].Max(), 0).ToString());
                title.SetText(selected.namedMonkeyKey);
                print(string.Join(", ", highDPST[selected.Id]) + " " + "l");
            }
        }
        else
        {
            var num1 = InGame.instance.GetTowers().Count > 0 ? InGame.instance.GetTowers().Select(t => t.damageDealt).Sum() : 0;
            var num2 = oldDamage.Values.Count > 0 ? oldDamage.Values.Sum() : 0;
            if (globaldps != null) globaldps.SetText(dpspre + (Math.Max(num1 - num2, 0)).ToString());
            if (globaldisplay != null)
            {
                if (highDPST.Count > 0)
                {
                    globalavg.SetText(avgpre + Math.Max((int)(GetIndexSums(highDPST).Average()), 0).ToString());
                    globalmax.SetText(maxpre + Math.Max(GetIndexSums(highDPST).Max(), 0).ToString());
                } else
                {
                    globalavg.SetText(avgpre + "0");
                    globalmax.SetText(maxpre + "0");
                }
                title.SetText("Global Stats");
            }
        }
    }
    private Vector2 offset;
    private void UpdateDraggableComponent(ModHelperComponent panel, ref Vector3? vector)
    {
        Rect frame = new(panel.transform.position.x - panel.initialInfo.Width/2, panel.transform.position.y - panel.initialInfo.Height/2, panel.initialInfo.Width, panel.initialInfo.Height);
        if (Input.GetMouseButtonDown(0) && frame.Contains(Input.mousePosition.WhydidthechickencrosstheroadTogettotheotherside()))
        {
            offset = panel.transform.position.WhydidthechickencrosstheroadTogettotheotherside() - Input.mousePosition.WhydidthechickencrosstheroadTogettotheotherside();
            vector = panel.transform.position;
        }
        if (Input.GetMouseButton(0))
        {
            if (frame.Contains(Input.mousePosition.WhydidthechickencrosstheroadTogettotheotherside()))
            {
                panel.transform.position = Input.mousePosition + new Vector3(offset.x, offset.y, Input.mousePosition.z);
                vector = panel.transform.position;
            }
        }
    }
    public override void OnTowerCreated(Tower tower, Entity target, Model modelToUse)
    {
        highDPST.TryAdd(tower.Id, new() { 0 });
        oldDamage.TryAdd(tower.Id, tower.damageDealt);
        base.OnTowerCreated(tower, target, modelToUse);
    }
    public override void OnTowerDestroyed(Tower tower)
    {
        highDPST.Remove(tower.Id);
        oldDamage.Remove(tower.Id);
        base.OnTowerDestroyed(tower);
    }
    private void UpdateInputs()
    {
        if (ResetPosition.JustPressed())
        {
            if (isWaitingForDoubleClick)
            {
                isWaitingForDoubleClick = false;
                
                draggable = !draggable;
                if (!draggable)
                {
                    Game.instance.ShowMessage("DPS UI Locked");

                }
                else
                {
                    Game.instance.ShowMessage("DPS UI Unlocked");
                }
            }
            else
            {
                isWaitingForDoubleClick = true;
                lastClickTime = Time.time;
            }
        }
        if (isWaitingForDoubleClick && Time.time - lastClickTime >= doubleClickThreshold)
        {
            // No double click detected within the threshold, treat as a single click
            isWaitingForDoubleClick = false;
            oldPosAdvanced = null;
            oldPosBasic = null;
            Game.instance.ShowMessage("DPS UI Reset");
        }
    }
    public static List<int> GetIndexSums(Dictionary<ObjectId, List<int>> dictionary)
    {
        List<int> sums = new List<int>();

        // Find the maximum length of the lists
        int maxLength = 0;
        foreach (List<int> values in dictionary.Values)
        {
            maxLength = Math.Max(maxLength, values.Count);
        }

        // Calculate sums at each index
        for (int i = 0; i < maxLength; i++)
        {
            int sum = 0;
            foreach (List<int> values in dictionary.Values)
            {
                if (i < values.Count)
                {
                    sum += values[i];
                }
            }
            sums.Add(sum);
        }

        return sums;
    }
    public void print(string text)
    {
        ModHelper.Msg<BetterDPS>(text);
    }
}
public static class shart
{
    public static Vector2 WhydidthechickencrosstheroadTogettotheotherside(this Vector3 vec)
    {
        return new Vector2(vec.x, vec.y);
    }
}