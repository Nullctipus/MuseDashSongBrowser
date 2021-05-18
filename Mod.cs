using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using ModHelper;
using Newtonsoft.Json;
using System.IO;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

namespace SongBrowser
{
	public class Menu : MonoBehaviour
	{
		public static Rect windowRect = new Rect(Screen.width / 3, Screen.height / 3, 950, Screen.height / 1.5f);
		public static bool ShowMenu = false;
		static readonly string[] starurls = new string[]
		{
			"https://mdmc.moe/images/icon_easy.png",
			"https://mdmc.moe/images/icon_hard.png",
			"https://mdmc.moe/images/icon_master.png"
		};
		static Texture2D easy;
		static Texture2D hard;
		static Texture2D master;
		void OnGUI()
        {
			if (ShowMenu)
			{
				windowRect = GUI.Window(0, windowRect, DoWindow, "Song Browser");
			}
		}
		KeyCode MenuKey = KeyCode.Insert;
		public void LoadImage(int type, string url)
        {
			Mod.DownloadData(url, delegate (byte[] data)
			{
				Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false, true);
				texture.LoadImage(data);
				texture.mipMapBias = 0;
				texture.anisoLevel = 1;
				texture.filterMode = FilterMode.Bilinear;
				texture.Apply();
                switch (type) 
				{
					case 0:
						easy = texture;
						break;

					case 1:
						hard = texture;
						break;

					default:
						master = texture;
						break;
				}
			});
		}
		public void Start()
        {
			if (!File.Exists(Path.Combine(Mod.CurrentDirectory, "SongBrowserkey.txt")))
			{
				File.WriteAllText(Path.Combine(Mod.CurrentDirectory, "SongBrowserkey.txt"), "F11");
			}
			MenuKey = (KeyCode)Enum.Parse(typeof(KeyCode), File.ReadAllText(Path.Combine(Mod.CurrentDirectory, "SongBrowserkey.txt")));
			windowRect = new Rect(Screen.width / 3, Screen.height / 3, 950, Screen.height / 1.5f);
			LoadImage(0, starurls[0]);
			LoadImage(1, starurls[1]);
			LoadImage(2, starurls[2]);


		}
		public void Update()
		{
			if (Input.GetKeyDown(MenuKey))
			{
				if (CustomAlbumInterface.SkinchangerMenu)
					CustomAlbumInterface.SkinchangerMenu = false;
				ShowMenu = !ShowMenu;
			}
		}
		Vector2 scroll = Vector2.zero;
		public void DoWindow(int windowID)
		{
			scroll = GUILayout.BeginScrollView(scroll,false,true);
			int final = 0;
			for (int i = 0; i < Mod.Charts.Length / 3; i++)
			{
				for (int j = 0; j < 3; j++)
				{
					final = i * 3 + j;
					CreateChart(Mod.Charts[final], j, i);
				}
			}
			for (int j = 0; j < Mod.Charts.Length % 3; j++)
			{
				CreateChart(Mod.Charts[final + j], j, final);
			}
			// generate space because bad
			GUILayout.Space(final*230/3+(final%3>0 ? 200 : 0));
			GUILayout.Label(console);
			GUILayout.EndScrollView();
			GUI.DragWindow(new Rect(0, 0, Screen.width, Screen.height));
		}
		public float CenterText(string text,int width)
        {
			float scale = GUI.skin.font.fontSize * text.Length / 2;
			GUILayout.BeginHorizontal();
			if (width / 2 - scale + (scale / 2) > 0)
				GUILayout.Space(width / 2 - scale + (scale / 2));
			GUILayout.Label(text);
			GUILayout.EndHorizontal();
			return width / 2 - scale + (scale / 2);
		}
		public void Log(string text,float time = 5)
        {
			StartCoroutine(LogTime(text, time));
        }
		IEnumerator LogTime(string text, float time)
        {
			console = text;
			yield return new WaitForSeconds(time);
			if (console == text)
				console = "";
        }
		string console = "";
		Color back = new Color(0.188f, 0.125f, 0.349f);
		Color forr = new Color(1, 1 / 3, 0.764f);
		public void CreateChart(ChartInfo chart,int x, int y)
        {
			GUILayout.BeginArea(new Rect(x*310,y*240,300,230));
			GUI.Box(new Rect(0, 50, 300, 180),"");

			GUILayout.BeginHorizontal();
			GUILayout.Space(105);
			Color c = GUI.backgroundColor;
			GUI.backgroundColor = Color.clear;
			GUILayout.Box(chart.Cover == null ? new GUIContent("") : new GUIContent(chart.Cover),GUILayout.Width(100),GUILayout.Height(100));
			GUI.backgroundColor = c;
			GUILayout.EndHorizontal();

			CenterText(chart.name, 300);
			CenterText("By " + chart.author,300);
			CenterText("Designer: " + chart.levelDesigner + "   BPM:" + chart.bpm, 300);

			GUILayout.BeginHorizontal();
			Color c2 = GUI.contentColor;
			GUI.backgroundColor =back;
			GUI.contentColor = forr;
            if (GUILayout.Button("Download", GUILayout.Height(40)))
            {

				ModLogger.AddLog("CustomAlbum", "Downloading", chart.name);
				Log("Downloading " + chart.name);
				Mod.DownloadData("https://mdmc.moe/api/download/" + chart.id, delegate (byte[] data)
				   {
					   ModLogger.AddLog("CustomAlbum", "Downloaded", chart.name);
					   Log("Downloaded " + chart.name);
					   CustomAlbumInterface.Inject(chart.name, Convert.FromBase64String(Encoding.Default.GetString(data)));
				   });
            }
			GUI.contentColor = c2;
			GUI.backgroundColor = Color.clear;
			GUILayout.Box(new GUIContent(chart.difficulty1 == "0" ? "" : chart.difficulty1, chart.difficulty1 == "0" ? null : easy), GUILayout.Width(60), GUILayout.Height(40));
			GUILayout.Box(new GUIContent(chart.difficulty2 == "0" ? "" : chart.difficulty2, chart.difficulty2 == "0" ? null : hard), GUILayout.Width(60), GUILayout.Height(40));
			GUILayout.Box(new GUIContent(chart.difficulty3 == "0" ? "" : chart.difficulty3, chart.difficulty3 == "0" ? null : master), GUILayout.Width(60), GUILayout.Height(40));
			GUI.backgroundColor = c;

			GUILayout.EndHorizontal();

			GUILayout.EndArea();
        }
    }

	public class ChartInfo
	{
        public override string ToString()
        {
            return string.Concat($"Name: {name}\nAuthor: {author}\nLevelDesigner: {levelDesigner}\nLevelDesigner1: {levelDesigner1}\nLevelDesigner2: {levelDesigner2}\nLevelDesigner3: {levelDesigner3}\nLevelDesigner4: {levelDesigner4}\ndifficulty1: {difficulty1}\ndifficulty2: {difficulty2}\ndifficulty3: {difficulty3}\nBPM:{bpm}\n");
        }
		public string ImageUrl()
        {
			return string.Format("https://mdmc.moe/data/charts/{0}/cover.png", id);
        }
		public Texture2D Cover
        {
			get
			{
				if (cover == null)
				{
					if (gettingImage)
						return null;
					gettingImage = true;
					Mod.DownloadData(ImageUrl(), delegate (byte[] data)
					 {
						 cover = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
						 cover.LoadImage(data);
						 cover.mipMapBias = 0;
						 cover.anisoLevel = 1;
						 cover.filterMode = FilterMode.Bilinear;
						 cover.Apply();
						 gettingImage = false;
					 });
					return null;
				}
				return cover;
			}
        }
		private bool gettingImage = false;
		Texture2D cover;
        public string name = "";
		public string author = "";
		public string bpm = "0";
		public string scene = "";
		public string levelDesigner = "";
		public string levelDesigner1 = "";
		public string levelDesigner2 = "";
		public string levelDesigner3 = "";
		public string levelDesigner4 = "";
		public string difficulty1 = "0";
		public string difficulty2 = "0";
		public string difficulty3 = "0";
		public string unlocklevel = "0";
		public string id = "0";
	}
    public class Mod : IMod
    {
		public static Mod Instance;
        public string Name => "Song Browser";

        public string Description => "No need to leave the game";

        public string Author => "BustR75";

        public string HomePage => "Is this even used?";

		public static HarmonyMethod GetPatch(string name)
		{
			return new HarmonyMethod(typeof(Mod).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic));
		}
		public static void Destroy()
        {
			harmony.UnpatchAll();
			GameObject.Destroy(menu.gameObject);
			Charts = null;
        }
		static Harmony harmony;
		public void DoPatching()
        {
			Instance = this;
			harmony = new Harmony("apo.bustr75.songbrowser");
			harmony.Patch(typeof(Assets.Scripts.GameCore.Managers.MainManager).GetMethod("InitLanguage", BindingFlags.NonPublic | BindingFlags.Instance), null, GetPatch(nameof(OnStart)));
			CustomAlbumInterface.Init();
		}
		private static void OnStart()
        {
			GameObject brow = new GameObject();
			GameObject.DontDestroyOnLoad(brow);
			menu = brow.AddComponent<Menu>();
			DownloadData("https://mdmc.moe/api/data/charts", delegate (byte[] json) {
				Charts = JsonConvert.DeserializeObject<ChartInfo[]>(Encoding.Default.GetString(json));
			});
		}
		public static void DownloadData(string url, Action<byte[]> callback)
        {
			menu.StartCoroutine(downloadData(url, callback));
        }
		static IEnumerator downloadData(string url,Action<byte[]> callback)
        {
			UnityWebRequest www = UnityWebRequest.Get(url);
			www.SendWebRequest();
			while (!www.downloadHandler.isDone) yield return new WaitForEndOfFrame();
			callback.Invoke(www.downloadHandler.data);
        }
		public static ChartInfo[] Charts;
		public static Menu menu;
		public static string CurrentDirectory = Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
		
	}
}
