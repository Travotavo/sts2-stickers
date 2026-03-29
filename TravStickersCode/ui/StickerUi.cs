using BaseLib.Utils;
using Godot;
using System;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Test;
using TravStickers;
using TravStickers.TravStickersCode;
using FileAccess = Godot.FileAccess;

public partial class StickerUi : Control, IScreenContext
{
	private Control _cardAnchor;
	private Control _cardContainer; // For stupid layering
	private AnimationPlayer _animationPlayer;
	
	private NCard _card;
	private Control _activeStickerParent;
	public StickerOption _activeSticker;
	private sticker_placement_node _stickerPlacement;
	private OpenEditorButton _trashButton;

	public Control _stickerBox;
	
	private static readonly string _scenePath = "res://" + MainFile.ModId + "/scenes/sticker_ui.tscn";
	private static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new("StickerEditor", LogType.Generic);

	public static readonly SpireField<NGame, Control> StickerEditorLayer = new((node) =>
	{
		Control layer = new Control();
		layer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		layer.MouseFilter = MouseFilterEnum.Ignore;
		Logger.Info("Created StickerEditorLayer");
		return layer;
	});

	public static StickerUi? StickerEditScreen = null;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_cardAnchor = this.GetNode<Control>((NodePath)"%travCardAnchor");
		_activeStickerParent = this.GetNode<Control>((NodePath)"%stickerMouseFollower");
		_cardContainer = this.GetNode<Control>((NodePath)"%cardContainer");
		_animationPlayer = this.GetNode<AnimationPlayer>("%travIntroAnimator");
		_card = NCard.Create(ModelDb.Card<Acrobatics>());
		_stickerBox = this.GetNode<Control>((NodePath)"%stickerBox");
		_cardContainer.AddChildSafely(_card);
		_animationPlayer.Play("slide");
		_stickerPlacement = this.GetNode<sticker_placement_node>((NodePath)"%stickerPlacement");
		_stickerPlacement.parent = this;
		_trashButton = this.GetNode<OpenEditorButton>((NodePath)"%TrashButton");
		_trashButton.Connect(NClickableControl.SignalName.Released, Callable.From(new Action<NButton>(_ =>clearStickers())));
		var files = DirAccess.GetFilesAt("res://" + MainFile.ModId + "/images/stickers");
		foreach (var file in files)
		{
			Logger.Info($"This is the sticker {file.Split('.')[0]}"); //This is the worst solution ever, fix later
			var sticker = StickerOption.Create(this);
			_stickerBox.AddChildSafely(sticker);
			sticker.setTexture(file.Split('.')[0]);
		}
	}

	public void clearStickers()
	{
		Logger.Info($"{PlacedSticker.StickerDB.Remove(_card.Model.Id.ToString())}");
		_card.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
		PlacedSticker.SaveStickers();
	}

	public void Open(CardModel template)
	{
		_card.Model = template;
		_card.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
		Visible = true;
		ActiveScreenContext.Instance.Update();
		NHotkeyManager.Instance.AddBlockingScreen(this);
		NHotkeyManager.Instance.PushHotkeyPressedBinding(MegaInput.cancel, Close);
		NHotkeyManager.Instance.PushHotkeyPressedBinding(MegaInput.pauseAndBack, Close);
	}
	
	public static StickerUi Create()
	{
		return PreloadManager.Cache.GetScene(_scenePath).Instantiate<StickerUi>();
	}

	public void grabSticker(StickerOption option)
	{
		if (_activeSticker != null)
		{
			_activeSticker.QueueFreeSafely();
			_activeSticker = null;
		}

		_activeSticker = option;
		_activeSticker._isDummy = true;
		_activeSticker.MouseFilter = MouseFilterEnum.Ignore;
		_activeSticker.Scale = new Vector2(0.5f,0.5f);
		_activeStickerParent.AddChild(_activeSticker);
		Logger.Info($"Grabbed Sticker {_activeSticker.Name}");
		_activeSticker.Position = -_activeSticker.PivotOffset;
	}

	public void placeSticker(PlacedSticker sticker)
	{
		if (_activeSticker == null) return;
		PlacedSticker.Save(_card.Model.Id.ToString(), sticker);
		_activeSticker.QueueFreeSafely();
		_activeSticker = null;
		_card.UpdateVisuals(_card.DisplayingPile,CardPreviewMode.Normal);
	}
	
	public static StickerUi OpenStickerMenu()
	{
		if (StickerEditScreen == null)
		{
			StickerEditScreen = Create();
			StickerEditorLayer.Get(NGame.Instance).AddChild(StickerEditScreen);
		}
		return StickerEditScreen;
	}

	public void Close()
	{
		if (Visible)
		{
			MouseFilter = MouseFilterEnum.Ignore;
			Visible = false;
			NHotkeyManager.Instance.RemoveHotkeyPressedBinding(MegaInput.cancel, Close);
			NHotkeyManager.Instance.RemoveHotkeyPressedBinding(MegaInput.pauseAndBack, Close);
			NHotkeyManager.Instance.RemoveBlockingScreen(this);
			if (_activeSticker != null)
			{
				_activeSticker.QueueFreeSafely();
			}
			_activeSticker = null;
			//_animationPlayer.PlayBackwards("slide");
		}
	}

	private void OnBackstopPressed(NButton _)
	{
		Close();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (_activeSticker != null)
		{
			_activeStickerParent.Position =  GetViewport().GetMousePosition();
		}
	}

	public Control? DefaultFocusedControl => null;
}
