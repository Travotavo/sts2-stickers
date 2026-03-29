using System.Reflection;
using BaseLib.Utils;
using Godot;
using Godot.Collections;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Pooling;
using FileAccess = Godot.FileAccess;
using Logger = Godot.Logger;

namespace TravStickers.TravStickersCode;

public partial class NStickerLayer : TextureRect, IPoolable
{
    private static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new("StickerLayer", LogType.Generic);
    public static readonly SpireField<NCard, NStickerLayer> StickerLayer = new((node) =>
    {
        NStickerLayer stickers = Create();
        //Really want to do some sort of layering here but meh
        //var cardsField = typeof(NCard).GetField("_typePlaque",BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance);
        //var layer = (NinePatchRect)cardsField.GetValue(node);
        //private TextureRect _energyIcon;
        node.AddChild(stickers);
        stickers.Owner = node;
        stickers.Card_ID = node.Model.Id.ToString();
        return stickers;
    });

    public string Card_ID;
    
    public static NStickerLayer NewInstanceForPool()
    {
        NStickerLayer layer = new();
        layer.Size = new Vector2(300,422);
        layer.Position = new Vector2(-150,-211);
        layer.PivotOffset = new Vector2(150,211);
        layer.ExpandMode = ExpandModeEnum.IgnoreSize;
        layer.MouseFilter = MouseFilterEnum.Ignore;
        //var image = Image.LoadFromFile("res://" + MainFile.ModId + "/images/stickers/" + "Defect_Clear" + ".png");
        //layer.Texture = ImageTexture.CreateFromImage(image);
        layer.ClipChildren = ClipChildrenMode.Only;
        //layer.FreeChildren();
        Logger.Info($"Creating Instance");
        return layer;
    }
    
    public static NStickerLayer Create()
    {
        return NodePool.Get<NStickerLayer>();
    }
    
    public void AddStickerChildren()
    {
        Reset();
        if (Card_ID  == null)
        {
            Logger.Info("No card ID!");
            return;
        }
        Texture = ResourceLoader.Load<Texture2D>("res://images/atlases/compressed.sprites/card_template/ancient_portrait_mask_large.tres");
        if (!PlacedSticker.StickerDB.ContainsKey(Card_ID))
        {  
            Logger.Info("No Stickers Found!");
            return;
        }
        var AttachedStickers = PlacedSticker.StickerDB[Card_ID];
        foreach (var sticker in AttachedStickers)
        {
            var instance = new TextureRect();
            instance.Texture = ResourceLoader.Load<Texture2D>("res://" + MainFile.ModId + "/images/stickers/" + sticker.ID + ".png");
            instance.PivotOffset = new Vector2(instance.Texture.GetWidth()/2,instance.Texture.GetHeight()/2);
            instance.Scale = new Vector2(sticker.scale,sticker.scale);
            instance.Rotation = sticker.rotation;
            instance.Position = sticker.position;
            instance.StretchMode = StretchModeEnum.Keep;
            instance.MouseFilter = MouseFilterEnum.Ignore;
            
            Logger.Info($"Drawing Sticker {sticker.ID}");
            AddChild(instance);
        }
    }
    
    public void OnInstantiated()
    {
        //Reset();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!PlacedSticker.StickerDB.ContainsKey(Card_ID)) {Reset(); return;}
        if (GetChildCount() != PlacedSticker.StickerDB[Card_ID].Count)
        {
            Reset();
            AddStickerChildren();
        }
    }

    public override void _Ready()
    {
        //I think this is where I shove all the rendering elements??
        //AddStickerChildren();
    }

    public void Reset()
    {
        if (GetChildCount() != 0)
        {
            foreach (var child in GetChildren())
            {
                child.QueueFreeSafely();
            }
        }
    }
    
    public void OnReturnedFromPool()
    {
    }

    public void OnFreedToPool()
    {
        //Reset();
    }

    private static readonly List<NStickerLayer> _activeHolders = [];
    
    public override void _EnterTree()
    {
        base._EnterTree();
        _activeHolders.Add(this);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        _activeHolders.Remove(this);
    }
}

public partial class PlacedSticker : Resource
{
    private static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new("StickerDatabase", LogType.Generic);
    
    public String ID;
    public float rotation;
    public Vector2 position;
    public float scale;

    public PlacedSticker(String id, float rotation, Vector2 position, float scale)
    {
        ID = id;
        this.rotation = rotation;
        this.position = position;
        this.scale = scale;
    }

    public static System.Collections.Generic.Dictionary<String, List<PlacedSticker>> StickerDB = new System.Collections.Generic.Dictionary<String, List<PlacedSticker>>();
    
    public static void Save(String card_ID,PlacedSticker sticker)
    {
        if (!StickerDB.ContainsKey(card_ID))
        {
            Logger.Info($"Initializing sticker list for {card_ID}");
            StickerDB.Add(card_ID, new List<PlacedSticker>());
        }
        StickerDB[card_ID].Add(sticker);
        Logger.Info($"Placed sticker {sticker.ID} onto {card_ID}");
        SaveStickers();
        //return new Json();
    }

    public static void SaveStickers()
    {
        var data = serializeDB();
        var json = Json.Stringify(data);
        
        using var file = FileAccess.Open("user://mod_configs/Stickers.json", FileAccess.ModeFlags.Write);
        file.StoreString(json);
    }

    public static Dictionary serializeDB()
    {
        var result = new Dictionary();

        foreach (var kvp in StickerDB)
        {
            var list = new Godot.Collections.Array();
            foreach (var sticker in kvp.Value)
            {
                list.Add(sticker.ToDictionary());
            }
            
            result[kvp.Key] = list;
        }   
        
        return result;
    }
    
    public Dictionary ToDictionary()
    {
        return new Dictionary
        {
            {"id", ID},
            {"rotation", rotation},
            {"position_x", position.X},
            {"position_y", position.Y},
            {"scale", scale}
        };
    }

    public static PlacedSticker FromDictionary(Dictionary dictionary)
    {
        return new PlacedSticker(
            (String)dictionary["id"],(float)dictionary["rotation"],new Vector2((float)dictionary["position_x"],(float)dictionary["position_y"]),(float)dictionary["scale"]);
    }
    
    public static void Load()
    {
        if (!FileAccess.FileExists("user://mod_configs/Stickers.json")) return;
        Logger.Info("Loading Stickers.json!");
        using var file = FileAccess.Open("user://mod_configs/Stickers.json", FileAccess.ModeFlags.Read);
        var json = file.GetAsText();
        var parsed = Json.ParseString(json).AsGodotDictionary();

        foreach (var key in parsed.Keys)
        {
            var list = new List<PlacedSticker>();
            var array = (Godot.Collections.Array)parsed[key];

            foreach (Dictionary dict in array)
            {
                list.Add(FromDictionary(dict));
            }
            StickerDB.Add(key.ToString(), list);
        }
    }
}