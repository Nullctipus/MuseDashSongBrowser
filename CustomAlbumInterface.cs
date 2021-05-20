using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using ModHelper;
using System.IO;
using System.Collections;
using Assets.Scripts.PeroTools.Commons;

namespace SongBrowser
{
    public static class CustomAlbumInterface
    {
        static FieldInfo mods;
        static bool printmods = true;
        static List<IMod> Mods
        {
            get
            {
                if (printmods)
                {
                    printmods = false;
                    Menu.VLog("Found Mods");
                    foreach(var v in (List<IMod>)mods.GetValue(null))
                    {
                        Menu.VLog(v);
                    }
                }
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
        static MethodInfo InitMusicInfo;
        static FieldInfo albums;
        static FieldInfo musicPackgeUid;
        static FieldInfo jsonName;
        static FieldInfo m_Dictionary;
        static FieldInfo customAssets;
        public static int MusicPackgeUid
        {
            get
            {
                return (int)musicPackgeUid.GetValue(null);
            }
        }
        public static string JsonName
        {
            get
            {
                return (string)jsonName.GetValue(null);
            }
        }
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
                //return "Custom_Albums";
                return (string)albumPackPath.GetValue(null);
            }
        }
        public static IDictionary CustomAssets
        {
            get
            {
                return (IDictionary)customAssets.GetValue(null);
            }
            set
            {
                customAssets.SetValue(null, value);
            }
        }
        public static IDictionary Albums
        {
            get
            {
                try
                {
                    return (IDictionary)albums.GetValue(null);
                }
                catch (Exception e)
                {
                    return new Dictionary<string, object>();
                }
            }
            set
            {
                albums.SetValue(null, value);
            }
        }
        public static Dictionary<string, Newtonsoft.Json.Linq.JArray> Dictionary
        {
            get
            {
                return (Dictionary<string, Newtonsoft.Json.Linq.JArray>)m_Dictionary.GetValue(Singleton<Assets.Scripts.PeroTools.Managers.ConfigManager>.instance);
            }
            set
            {
                m_Dictionary.SetValue(Singleton<Assets.Scripts.PeroTools.Managers.ConfigManager>.instance, value);
            }
        }
        public static void Inject(string name,byte[] data)
        {
            try
            {
                ModLogger.AddLog("CustomAlbum", "Injecting", name);
                Mod.menu.Log("Injecting " + name);
                string directory = Path.Combine(Mod.CurrentDirectory, AlbumPackPath + "\\" + name + ".mdm");

                ModLogger.AddLog("CustomAlbum", "Create File", directory);
                File.WriteAllBytes(directory, data);
                LoadCustomSong(directory);
            }
            catch(Exception e)
            {
                ModLogger.Debug(e);
            }
        }
        public static void Init()
        {
            Menu.VLog("Setting Up Custom Album Reflection");
            mods = typeof(ModLoader.ModLoader).GetField("mods", BindingFlags.NonPublic | BindingFlags.Static);
            lock (Mods)
            {
                if (CustomAlbum == null)
                    CustomAlbum = Mods.First(x => x.Name == "CustomAlbum" && x.Author == "Mo10");
            }
            try
            {
                Menu.VLog("...." + CustomAlbum.Name);
            }
            catch
            {

            }
            Type Skinchanger = Mods.First(x => x.Author == "BustR75" && x.Description == "Change the textures on the characters").GetType();
            if (Skinchanger != null)
                skinchangermenu = Skinchanger.GetField("ShowMenu", BindingFlags.Public | BindingFlags.Static);
            try
            {
                Menu.VLog("...." + skinchangermenu.Name);
            }
            catch { Menu.VLog("Didn't find SkinChanger"); }
            if (CustomAlbum == null)
            {
                ModLogger.AddLog("CustomAlbumInterface", "Init", "Failed to find CustomAlbum Mod, Downloading Now");
                Mod.DownloadData("https://cdn.discordapp.com/attachments/812778196130856991/818368646601637888/MuseDashCustomAlbumMod.dll", delegate (byte[] data)
                {
                    File.WriteAllBytes(Path.Combine(Mod.CurrentDirectory, "Mods\\MuseDashCustomAlbumMod.dll" + ""), data);
                    Assembly ass = Assembly.Load(data);
                    foreach (Type type in ass.GetTypes())
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
                    Menu.VLog("Loaded CustomAlbum Mod");

                });
                return;
            }
            if (CustomType == null)
                CustomType = CustomAlbum.GetType();
            Menu.VLog("...." + CustomType.Name);
            CustomAlbumInfo = CustomType.Assembly.GetTypes().First(x => x.Name == "CustomAlbumInfo");
            Menu.VLog("...." + CustomAlbumInfo.Name);
            CustomAlbumFromFile = CustomAlbumInfo.GetMethods().First(x => x.Name == "LoadFromFile");
            Menu.VLog("...." + CustomAlbumFromFile.Name);
            Type customalbum = CustomType.Assembly.GetTypes().First(x => x.Name == "CustomAlbum");
            Menu.VLog("...." + customalbum.Name);
            albums = customalbum.GetField("Albums", BindingFlags.Static | BindingFlags.Public);
            Menu.VLog("...." + albums.Name);
            albumPackPath = customalbum.GetField("AlbumPackPath", BindingFlags.Static | BindingFlags.Public);
            Menu.VLog("...." + albumPackPath.Name);
            musicPackgeUid = customalbum.GetField("MusicPackgeUid", BindingFlags.Static | BindingFlags.Public);
            Menu.VLog("...." + musicPackgeUid.Name);
            jsonName = customalbum.GetField("JsonName", BindingFlags.Static | BindingFlags.Public);
            Menu.VLog("...." + jsonName.Name);
            customAssets = CustomType.Assembly.GetTypes().First(x => x.Name == "DataPatch").GetField("customAssets",BindingFlags.Public|BindingFlags.Static);
            Menu.VLog("...." + customAssets.Name);

            m_Dictionary = typeof(Assets.Scripts.PeroTools.Managers.ConfigManager).GetField("m_Dictionary", BindingFlags.NonPublic | BindingFlags.Instance);
            Menu.VLog("...." + m_Dictionary.Name);
            InitMusicInfo = typeof(Assets.Scripts.UI.Panels.PnlStage).GetMethod("InitMusicInfo", BindingFlags.NonPublic | BindingFlags.Instance);
            Menu.VLog("...." + InitMusicInfo.Name);
            Menu.VLog("Finished Custom Album Reflection");

        }
        public static void LoadCustomSong(string directory)
        {
            Menu.VLog("Loading " + directory);
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
                    Menu.Instance.Log(string.Format("Loaded archive:{0}", customAlbumInfo));
                    IDictionary Buffer = Albums;
                    Buffer.Add("archive_" + fileNameWithoutExtension, customAlbumInfo);
                    Albums = Buffer;
                }
            }
            catch (Exception arg)
            {
                ModLogger.Debug(string.Format("Load archive failed:{0},reason:{1}", directory, arg));
                Menu.Instance.Log(string.Format("Load archive failed:{0},reason:{1}", directory, arg));
            }
            try
            {
                Dictionary<string, Newtonsoft.Json.Linq.JArray> buffer = Dictionary;
                if (buffer.ContainsKey(JsonName))
                {
                    buffer.Remove(JsonName);
                    Dictionary = buffer;
                }
                IDictionary buffer2 = CustomAssets;
                buffer2.Clear();
                CustomAssets = buffer2;
                InitMusicInfo.Invoke(UnityEngine.Resources.FindObjectsOfTypeAll<Assets.Scripts.UI.Panels.PnlStage>()[0], new object[] { jsonName,musicPackgeUid.ToString()});
            }
            catch(Exception e)
            {

            }
        }
    }
}
