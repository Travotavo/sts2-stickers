using Godot;
using System;
using System.Reflection;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using TravStickers;
using TravStickers.TravStickersCode;

public partial class sticker_placement_node : NClickableControl
{
	private TextureRect _fakeSticker;
	public StickerUi parent;
	private PlacedSticker _stickerStats;
	
	private static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new("StickerEditor", LogType.Generic);
	public override void _Ready()
	{
		ConnectSignals();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		base._UnhandledInput(@event);
		if (@event is InputEventMouseButton)
		{
			InputEventMouseButton emb = (InputEventMouseButton) @event;
			if (@event.IsPressed())
			{
				if (_fakeSticker == null) return;
				if (emb.ButtonIndex == MouseButton.WheelUp)
				{
					_fakeSticker.Scale += new Vector2(0.005f,0.005f);
				}
				else if (emb.ButtonIndex == MouseButton.WheelDown)
				{
					_fakeSticker.Scale -= new Vector2(0.005f,0.005f);
				}
				_fakeSticker.Scale = _fakeSticker.Scale.Clamp(new  Vector2(0.025f,0.025f),new  Vector2(0.175f,0.175f));
			}
		}
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		var cardsField = typeof(NClickableControl).GetField("_isHovered",BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance);
		if ((bool)cardsField.GetValue(this) && parent._activeSticker != null)
		{
			if (_fakeSticker == null)
			{
				Logger.Info("Drawing Fake Sticker");
				TextureRect texture_rect = new TextureRect();
				var texture = ResourceLoader.Load<Texture2D>("res://" + MainFile.ModId + "/images/stickers/" + parent._activeSticker.StickerID + ".png");
				texture_rect.Texture = texture;
				_stickerStats = new PlacedSticker(parent._activeSticker.StickerID,0,new Vector2(),0.1f);
				texture_rect.PivotOffset = new Vector2(texture.GetWidth()/2,texture.GetHeight()/2);
				texture_rect.Scale = new Vector2(_stickerStats.scale,_stickerStats.scale);
				texture_rect.MouseFilter = MouseFilterEnum.Ignore;
				_fakeSticker = texture_rect;
				_fakeSticker.Modulate = new Color(1, 1, 1, 0.5f);
				
				AddChild(texture_rect);
			}
			else
			{
				_fakeSticker.GlobalPosition = GetGlobalMousePosition() - (_fakeSticker.PivotOffset) * _fakeSticker.Scale;
			}
		}
		else
		{
			if (_fakeSticker != null)
			{
				_fakeSticker.QueueFreeSafely();
				_fakeSticker = null;
			}
		}
	}

	protected override void OnFocus()
	{
	}

	protected override void OnUnfocus()
	{
	}

	protected override void OnPress()
	{
		if (_stickerStats != null)
		{
			_stickerStats.rotation = _fakeSticker.Rotation;
			_stickerStats.position = _fakeSticker.Position;
			_stickerStats.scale = _fakeSticker.Scale.X;
			parent.placeSticker(_stickerStats);
			_stickerStats = null;
			_fakeSticker.QueueFreeSafely();
			_fakeSticker = null;
		}
	}

	protected override void OnRelease()
	{
	}
	
}
