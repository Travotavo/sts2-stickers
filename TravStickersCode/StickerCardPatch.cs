using System.Reflection;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace TravStickers.TravStickersCode;

[HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
public class StickerCardPatch
{
    [HarmonyPrefix]
    public static void Prefix(NCard __instance)
    {
        var stickerLayer = NStickerLayer.StickerLayer[__instance];
        stickerLayer.Card_ID = __instance.Model.Id.ToString();
        stickerLayer.AddStickerChildren();
    }
}

[HarmonyPatch(typeof(NCard), nameof(NCard.Create))]
public class StickerCreateCardPatch
{
    [HarmonyPostfix]
    public static NCard Postfix(NCard __result)
    {
        var stickerLayer = NStickerLayer.StickerLayer[__result];
        stickerLayer.Card_ID = __result.Model.Id.ToString();
        stickerLayer.AddStickerChildren();
        return __result;
    }
}

[HarmonyPatch(typeof(NGame), nameof(NCard._Ready))]
public class StickerEditorPatch
{
    [HarmonyPostfix]
    public static void Postfix(NGame __instance)
    {
        __instance.AddChild(StickerUi.StickerEditorLayer[__instance]);
    }
}
[HarmonyPatch(typeof(NInspectCardScreen), nameof(NInspectCardScreen._Ready))]
public class InspectionScreenButtonPatch{
    private static readonly string _scenePath = "res://" + MainFile.ModId + "/scenes/sticker_editor_button.tscn";

    private static OpenEditorButton _stickerButton;
    
    [HarmonyPostfix]
    public static void Postfix(NInspectCardScreen __instance)
    {
        OpenEditorButton overlay = PreloadManager.Cache.GetScene(_scenePath).Instantiate<OpenEditorButton>();
        __instance.AddChild(overlay);
        _stickerButton = overlay;
        _stickerButton.Connect(NClickableControl.SignalName.Released, Callable.From(new Action<NButton>(_ =>openStickerMenu())));
    }
    
    public static void openStickerMenu()
    {
        var cardsField = typeof(NInspectCardScreen).GetField("_cards",BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance);
        var indexField = typeof(NInspectCardScreen).GetField("_index",BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance);
        var model = ((List<CardModel>)cardsField.GetValue(NGame.Instance.InspectCardScreen))[(int)indexField.GetValue(NGame.Instance.InspectCardScreen)];
        StickerUi.OpenStickerMenu().Open(model);
    }
}
