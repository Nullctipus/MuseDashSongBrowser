﻿using System;
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

                });
                return;
            }
            if (CustomType == null)
                CustomType = CustomAlbum.GetType();
            CustomAlbumInfo = CustomType.Assembly.GetTypes().First(x => x.Name == "CustomAlbumInfo");
            CustomAlbumFromFile = CustomAlbumInfo.GetMethods().First(x => x.Name == "LoadFromFile");
            Type customalbum = CustomType.Assembly.GetTypes().First(x => x.Name == "CustomAlbum");
            albums = customalbum.GetField("Albums", BindingFlags.Static | BindingFlags.Public);
            albumPackPath = customalbum.GetField("AlbumPackPath", BindingFlags.Static | BindingFlags.Public);
            musicPackgeUid = customalbum.GetField("MusicPackgeUid", BindingFlags.Static | BindingFlags.Public);
            jsonName = customalbum.GetField("JsonName", BindingFlags.Static | BindingFlags.Public);
            customAssets = CustomType.Assembly.GetTypes().First(x => x.Name == "DataPatch").GetField("customAssets",BindingFlags.Public|BindingFlags.Static);

            m_Dictionary = typeof(Assets.Scripts.PeroTools.Managers.ConfigManager).GetField("m_Dictionary", BindingFlags.NonPublic | BindingFlags.Instance);
            InitMusicInfo = typeof(Assets.Scripts.UI.Panels.PnlStage).GetMethod("InitMusicInfo", BindingFlags.NonPublic | BindingFlags.Instance);

        }
        public static void LoadCustomSong(string directory)
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
                    IDictionary Buffer = Albums;
                    Buffer.Add("archive_" + fileNameWithoutExtension, customAlbumInfo);
                    Albums = Buffer;
                }
            }
            catch (Exception arg)
            {
                ModLogger.Debug(string.Format("Load archive failed:{0},reason:{1}", directory, arg));
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