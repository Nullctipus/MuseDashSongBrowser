using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using ModHelper;
using System.IO;

namespace SongBrowser
{
    public static class CustomAlbumInterface
    {
        static FieldInfo mods;
        static List<IMod> Mods
        {
            get
            {
                return (List<IMod>)mods.GetValue(null);
            }
            set
            {
                mods.SetValue(null, value);
            }
        }
        static IMod CustomAlbum;
        static Type CustomType;
        static Type CustomAlbumInfo;
        static MethodInfo CustomAlbumFromFile;
        static FieldInfo albumPackPath;
        static FieldInfo skinchangermenu;
        public static bool SkinchangerMenu 
        {
            get
            {
                if (skinchangermenu == null)
                    return false;
                return (bool)skinchangermenu.GetValue(null);
            }
            set
            {
                skinchangermenu?.SetValue(null, value);
            }
        }
        static string AlbumPackPath
        {
            get
            {
                return (string)albumPackPath.GetValue(null);
            }
        }
        static FieldInfo albums;
        public static Dictionary<string, object> Albums
        {
            get
            {
                return (Dictionary<string, object>)albums.GetValue(null);
            }
            set
            {
                albums.SetValue(null, value);
            }
        }
        public static void Init()
        {
            mods = typeof(ModLoader.ModLoader).GetField("mods", BindingFlags.NonPublic | BindingFlags.Static);
            if (CustomAlbum == null)
                CustomAlbum = Mods.First(x => x.Name == "CustomAlbum" && x.Author == "Mo10");
            Type Skinchanger = Mods.First(x => x.Author == "BustR75" && x.Description == "Change the textures on the characters").GetType();
            if (Skinchanger != null)
                skinchangermenu = Skinchanger.GetField("ShowMenu", BindingFlags.Public | BindingFlags.Static);
            if (CustomAlbum == null)
            {
                ModLogger.AddLog("CustomAlbumInterface", "Init", "Failed to find CustomAlbum Mod, Downloading Now");
                Mod.DownloadData("https://cdn.discordapp.com/attachments/812778196130856991/818368646601637888/MuseDashCustomAlbumMod.dll", delegate (byte[] data)
                {
                    File.WriteAllBytes(Path.Combine(Mod.CurrentDirectory, "Mods\\MuseDashCustomAlbumMod.dll" + ""), data);
                    Assembly ass = Assembly.Load(data);
                    foreach(Type type in ass.GetTypes())
                    {
                        if (type.GetInterface(typeof(IMod).ToString()) != null)
                        {
                            List<IMod> Buffer = Mods;
                            CustomAlbum = (IMod)Activator.CreateInstance(type);
                            CustomAlbum.DoPatching();
                            CustomType = type;
                            Buffer.Add(CustomAlbum);
                            Mods = Buffer;
                        }
                    }
                    typeof(ModLoader.ModLoader).GetMethod("LoadDependency", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { ass });
                    Init();

                });
                return;
            }
            if (CustomType == null)
                CustomType = CustomAlbum.GetType();
            CustomAlbumInfo = CustomType.Assembly.GetTypes().First(x=>x.Name.Contains("CustomAlbumInfo"));
            CustomAlbumFromFile = CustomAlbumInfo.GetMethod("LoadFromFile",BindingFlags.Static|BindingFlags.Public);
            Type customalbum = CustomType.Assembly.GetTypes().First(x => x.Name.Contains("CustomAlbum"));
            albums = customalbum.GetField("Albums", BindingFlags.Static | BindingFlags.Public);
            albumPackPath = customalbum.GetField("AlbumPackPath", BindingFlags.Static | BindingFlags.Public);

        }
        public static void LoadCustomAlbum(string directory)
        {
            bool flag = !Directory.Exists(AlbumPackPath);
            if (flag)
            {
                Directory.CreateDirectory(AlbumPackPath);
            }
            try
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(directory);
                object customAlbumInfo = CustomAlbumFromFile.Invoke(null, new object[] { directory });
                bool flag2 = customAlbumInfo != null;
                if (flag2)
                {
                    ModLogger.Debug(string.Format("Loaded archive:{0}", customAlbumInfo));
                    Dictionary<string, object> Buffer = Albums;
                    Buffer.Add("archive_" + fileNameWithoutExtension, customAlbumInfo);
                    Albums = Buffer;
                }
            }
            catch (Exception arg)
            {
                ModLogger.Debug(string.Format("Load archive failed:{0},reason:{1}", directory, arg));
            }

        }
    }
}
