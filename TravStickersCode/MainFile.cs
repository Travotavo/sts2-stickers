using System.Reflection;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using TravStickers.TravStickersCode;

namespace TravStickers;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
	public const string ModId = "TravStickers"; //At the moment, this is used only for the Logger and harmony names.

	public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
		new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

	public static void Initialize()
	{
		Harmony harmony = new(ModId);
		
		var assembly = Assembly.GetExecutingAssembly();
		Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(assembly);

		harmony.PatchAll();

		GeneratedNodePool.Init(NStickerLayer.NewInstanceForPool, 32);
		
		PlacedSticker.Load();
	}
}
