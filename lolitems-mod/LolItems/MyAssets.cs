using UnityEngine;
using System.IO;
using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;

//Static class for ease of access
public static class MyAssets
{
    public static AssetBundle icons;
    public static AssetBundle prefabs;
    public const string iconsName = "icons";
    public const string prefabsName = "prefabs";

    //The direct path to your AssetBundle
    public static string IconAssetBundlePath
    {
        get
        {
            return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(LoLItems.LoLItems.PInfo.Location), iconsName);
        }
    }

    public static string PrefabAssetBundlePath
    {
        get
        {
            return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(LoLItems.LoLItems.PInfo.Location), prefabsName);
        }
    }

    public static void Init()
    {
        //Loads the assetBundle from the Path, and stores it in the static field.
        icons = AssetBundle.LoadFromFile(IconAssetBundlePath);
        prefabs = AssetBundle.LoadFromFile(PrefabAssetBundlePath);
    }

    // Folder next to the mod DLL where loose icon PNGs can be dropped in, for items that don't
    // have (or don't yet have) a proper entry baked into the Unity AssetBundle above.
    public static string CustomIconsPath
    {
        get
        {
            return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(LoLItems.LoLItems.PInfo.Location), "CustomIcons");
        }
    }

    public static Sprite LoadCustomIcon(string fileName)
    {
        string path = System.IO.Path.Combine(CustomIconsPath, fileName);
        if (!File.Exists(path))
        {
            LoLItems.LoLItems.Log.LogWarning("Custom icon not found at " + path + ", falling back to the default mystery icon.");
            return Resources.Load<Sprite>("Textures/MiscIcons/texMysteryIcon");
        }

        byte[] data = File.ReadAllBytes(path);
        Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
        ImageConversion.LoadImage(texture, data);
        texture.filterMode = FilterMode.Point;
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }
}