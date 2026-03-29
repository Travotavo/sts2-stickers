using Godot;
using System;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using TravStickers;

public partial class StickerOption : NClickableControl
{
	// Called when the node enters the scene tree for the first time.
	private StickerUi? parent;
	public bool _isDummy = false;
	private TextureRect _stickerTexture;
	public String StickerID;
	public override void _Ready()
	{
		ConnectSignals();
		_stickerTexture = this.GetNode<TextureRect>((NodePath)"%stickerTexture");
	}

	private static readonly string _scenePath = "res://" + MainFile.ModId + "/scenes/sticker_option.tscn";
	public static StickerOption Create(StickerUi parent)
	{
		StickerOption instance = PreloadManager.Cache.GetScene(_scenePath).Instantiate<StickerOption>();
		instance.parent = parent;
		return instance;
	}

	public void setTexture(String StickerID)
	{
		var texture = ResourceLoader.Load<Texture2D>("res://" + MainFile.ModId + "/images/stickers/" + StickerID + ".png");
		_stickerTexture.Texture = texture;
		this.StickerID = StickerID;
		//_stickerTexture.PivotOffset = new Vector2((float)texture.GetWidth() / 2,(float)texture.GetHeight() / 2);
		//_stickerTexture.Position = -_stickerTexture.PivotOffset;
	}
	
	protected override void OnFocus()
	{
	}

	protected override void OnUnfocus()
	{
	}

	protected override void OnPress()
	{
		if (_isDummy) return;
		StickerOption dummy = (StickerOption)Duplicate();
		dummy.StickerID = this.StickerID;
		parent.grabSticker(dummy);
	}

	protected override void OnRelease()
	{
	}
}
