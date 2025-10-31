using FMOD;
using FMODUnity;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CFKillFeedback
{
	public class ModBehaviour : Duckov.Modding.ModBehaviour
	{
		public static bool Loaded = false;
		public static Dictionary<string, object> DefaultConfig = new Dictionary<string, object>();
		public static Vector3 IconSizeDrop = new Vector3(4.0f, 4.0f);
		public static Vector3 IconSizeStay = new Vector3(2.0f, 2.0f);
		//public static Vector2 IconSizeBaseMulti = new Vector2(960.0f, 540.0f);
		//public static Vector3 IconPosPrctDrop = new Vector3(0.0f, -1.0f); //基于屏幕中心的百分比，1.0为一个半径
		//public static Vector3 IconPosPrctStay = new Vector3(0.0f, -0.8f); //基于屏幕中心的百分比，1.0为一个半径
		public static Vector3 IconPosPrctDrop = new Vector3(0.5f, 0.1f); //基于屏幕尺寸，原点为左下角
		public static Vector3 IconPosPrctStay = new Vector3(0.5f, 0.2f); //基于屏幕尺寸，原点为左下角
		public static float IconShakeLength = 0.1f; //基于半个屏幕尺寸中长和高之间较小的值
		public static float IconShakeModSecond = 0.05f;
		public static ModBehaviour? Instance;
		// 所有图标名称，这些名称将用于拼接成定位图片资源的路径(.png)
		public static readonly string[] IconNames = new string[]
		{
			"kill",
			"kill2",
			"kill3",
			"kill4",
			"kill5",
			"kill6",
			"headshot",
			"headshot_gold",
			//"wallshot",
			//"wallshot_gold",
			"grenade_kill",
			"melee_kill"
		};
		// 所有音频名称，这些名称将用于拼接成定位音频资源的路径(.wav)
		public static readonly string[] AudioNames = new string[]
		{
			"kill",
			"kill2", //double kill
			"kill3", //multi kill
			"kill4", //ultra kill
			"kill5", //unbreakable
			"kill6", //unbelievable
			"kill7", //听不懂
			"kill8", //听不懂
			"headshot", //headshot
			"grenade_kill",
			"melee_kill",
			"death" //呃啊
		};
		// 从硬盘加载到内存的图片资源
		public static Dictionary<string, Texture2D> KillFeedbackIcons = new Dictionary<string, Texture2D>();
		// 从硬盘加载到内存的音频资源
		//public static Dictionary<string, AudioClip> KillFeedbackAudios = new Dictionary<string, AudioClip>();
		public static Dictionary<string, Sound> KillFeedbackAudios_FMOD = new Dictionary<string, Sound>();
		internal static Image? ui_image;
		internal static RectTransform? ui_transform;
		internal static CanvasGroup? ui_canvasgroup;
		// 配置文件-音量，0-1决定音量大小
		public static float volume = 1.0f;
		// 配置文件-简单音效，为true时无论伤害类型与连杀数如何只播放普通击杀和爆头击杀的音效
		public static bool simple_sfx = false;
		// 配置文件-禁用图标，为true时不显示图标
		public static bool disable_icon = false;
		// 配置文件-图标尺寸乘数
		public static float icon_size_multi = 1.0f;
		// 配置文件-图标不透明度
		public static float icon_alpha = 0.75f;
		// 配置文件-连杀计时
		public static float combo_seconds = 8.0f;
		// 配置文件-图标掉入时间
		public static float icon_drop_seconds = 0.1f;
		// 配置文件-图标停留时间
		public static float icon_stay_seconds = 1.0f;
		// 配置文件-图标淡出时间
		public static float icon_fadeout_seconds = 1.25f;
		// 配置文件-是否连杀图标优先级高于爆头
		public static bool is_dont_use_headshot_icon_if_combo = false;
		// 上次击杀的引擎启动时间，获取方式是Time.time
		public static float last_kill_time = 0.0f;
		// 连杀数
		public static int combo_count = 0;
		private void Update()
		{
			float delta = Time.time - last_kill_time;
			if (ui_canvasgroup != null && ui_transform != null)
			{
				Vector2 screen_size = ui_transform.parent.position * 2.0f;
				if (delta < icon_drop_seconds)
				{
					float current_delta = Math.Clamp(delta / icon_drop_seconds, 0.0f, 1.0f);
					ui_canvasgroup.alpha = current_delta * icon_alpha;
					//ui_transform.localScale = Vector3.Lerp(IconSizeDrop, IconSizeStay, current_delta) * Math.Min(ui_transform.parent.position.x / IconSizeBaseMulti.x, ui_transform.parent.position.x / IconSizeBaseMulti.y) * icon_size_multi;
					ui_transform.localScale = Vector3.Lerp(IconSizeDrop, IconSizeStay, current_delta) * icon_size_multi;
					Vector3 pos = Vector3.Lerp(IconPosPrctDrop, IconPosPrctStay, current_delta);
					ui_transform.position = new Vector3(pos.x * screen_size.x, pos.y * screen_size.y);
				}
				else if (delta > icon_drop_seconds + icon_stay_seconds)
				{
					//ui_transform.localScale = IconSizeStay * Math.Min(ui_transform.parent.position.x / IconSizeBaseMulti.x, ui_transform.parent.position.x / IconSizeBaseMulti.y) * icon_size_multi;
					ui_transform.localScale = IconSizeStay * icon_size_multi;
					ui_canvasgroup.alpha = (1.0f - Math.Clamp((delta - icon_drop_seconds - icon_stay_seconds) / icon_fadeout_seconds, 0.0f, 1.0f)) * icon_alpha;
					ui_transform.position = new Vector3(IconPosPrctStay.x * screen_size.x, IconPosPrctStay.y * screen_size.y);
				}
				else
				{
					//ui_transform.localScale = IconSizeStay * Math.Min(ui_transform.parent.position.x / IconSizeBaseMulti.x, ui_transform.parent.position.x / IconSizeBaseMulti.y) * icon_size_multi;
					ui_transform.localScale = IconSizeStay * icon_size_multi;
					ui_canvasgroup.alpha = icon_alpha;
					ui_transform.position = new Vector3(IconPosPrctStay.x * screen_size.x, IconPosPrctStay.y * screen_size.y);
				}
				if (disable_icon)
				{
					ui_canvasgroup.alpha = 0.0f;
				}
			}
		}
		public void OnDead(Health health, DamageInfo damageInfo)
		{
			// 防空引用
			if (health == null)
			{
				return;
			}
			// 如果是玩家自己嗝屁了
			if (health.IsMainCharacterHealth)
			{
				if (KillFeedbackAudios_FMOD.ContainsKey("death"))
				{
					RuntimeManager.GetBus("bus:/Master/SFX").getChannelGroup(out ChannelGroup channel_group);
					RuntimeManager.CoreSystem.playSound(KillFeedbackAudios_FMOD["death"], channel_group, false, out _);
				}
				return;
			}
			// 如果伤害来自玩家队
			if (damageInfo.fromCharacter.Team == Teams.player)
			{
				bool headshot = damageInfo.crit > 0;
				bool melee = damageInfo.fromCharacter.GetMeleeWeapon() != null;
				bool explosion = damageInfo.isExplosion;
				bool goldheadshot = damageInfo.finalDamage >= health.MaxHealth * 0.9f;
				PlayKill(headshot, goldheadshot, melee, explosion);
			}
		}
		// 播放击杀，图标优先级为：近战<爆炸<连杀<爆头，音频优先级为：普通<爆头<近战<爆炸<连杀
		public void PlayKill(bool headshot, bool goldheadshot, bool melee, bool explosion)
		{
			if (ui_transform == null)
			{
				CreateUI();
			}
			UpdateCombo();
			// 确定使用的资源
			Texture2D? icon = null;
			Sound audio = new Sound();
			if (combo_count <= 1) //第一杀
			{
				icon = KillFeedbackIcons["kill"];
				audio = KillFeedbackAudios_FMOD["kill"];
				if (headshot) //爆头
				{
					audio = KillFeedbackAudios_FMOD["headshot"];
				}
				if (goldheadshot) //黄金爆头
				{
					audio = KillFeedbackAudios_FMOD["headshot"];
				}
				if (melee) //近战
				{
					audio = KillFeedbackAudios_FMOD["melee_kill"];
					//icon = KillFeedbackIcons["melee_kill"];
				}
				if (explosion) //爆炸
				{
					audio = KillFeedbackAudios_FMOD["grenade_kill"];
					//icon = KillFeedbackIcons["grenade_kill"];
				}
			}
			else if (combo_count <= 8) //8杀以内
			{
				switch (combo_count)
				{
					case 2:
						icon = KillFeedbackIcons["kill2"];
						audio = KillFeedbackAudios_FMOD["kill2"];
						break;
					case 3:
						icon = KillFeedbackIcons["kill3"];
						audio = KillFeedbackAudios_FMOD["kill3"];
						break;
					case 4:
						icon = KillFeedbackIcons["kill4"];
						audio = KillFeedbackAudios_FMOD["kill4"];
						break;
					case 5:
						icon = KillFeedbackIcons["kill5"];
						audio = KillFeedbackAudios_FMOD["kill5"];
						break;
					case 6:
						icon = KillFeedbackIcons["kill6"];
						audio = KillFeedbackAudios_FMOD["kill6"];
						break;
					case 7:
						icon = KillFeedbackIcons["kill6"];
						audio = KillFeedbackAudios_FMOD["kill7"];
						break;
					case 8:
						icon = KillFeedbackIcons["kill6"];
						audio = KillFeedbackAudios_FMOD["kill8"];
						break;
					default:
						break;
				}
			}
			else //8杀以外
			{
				icon = KillFeedbackIcons["kill6"];
				audio = KillFeedbackAudios_FMOD["kill"];
			}
			if (combo_count > 1 && !is_dont_use_headshot_icon_if_combo)
			{
				if (headshot) //爆头
				{
					icon = KillFeedbackIcons["headshot"];
				}
				if (goldheadshot) //黄金爆头
				{
					icon = KillFeedbackIcons["headshot_gold"];
				}
			}
			if (melee) //近战
			{
				icon = KillFeedbackIcons["melee_kill"];
			}
			if (explosion) //爆炸物
			{
                icon = KillFeedbackIcons["grenade_kill"];
            }
			if (simple_sfx)
			{
				if (headshot)
				{
					audio = KillFeedbackAudios_FMOD["headshot"];
				}
				else
				{
					audio = KillFeedbackAudios_FMOD["kill"];
				}
			}
			// 应用资源
			RuntimeManager.GetBus("bus:/Master/SFX").getChannelGroup(out ChannelGroup channel_group);
			RuntimeManager.CoreSystem.playSound(audio, channel_group, false, out Channel channel);
			channel.setVolume(volume);
            if (ui_image != null && icon != null)
			{
				ui_image.sprite = Sprite.Create(icon, new Rect(0.0f, 0.0f, 512.0f, 512.0f), new Vector2(256.0f, 256.0f));
			}
		}
		public static void UpdateCombo()
		{
			float time = Time.time;
			if (time - last_kill_time > (float)combo_seconds || time - last_kill_time <= 0.0f)
			{
				// 重置连杀数为第一杀
				combo_count = 1;
			}
			else
			{
				// 增加连杀数
				combo_count++;
			}
			last_kill_time = time;
		}
		private void Awake()
		{
			DefaultConfig.TryAdd("volume", 1.0f);
			DefaultConfig.TryAdd("simple_sfx", false);
			DefaultConfig.TryAdd("disable_icon", false);
			DefaultConfig.TryAdd("icon_size_multi", 1.0f);
			DefaultConfig.TryAdd("icon_alpha", 0.75f);
			DefaultConfig.TryAdd("combo_seconds", 8.0f);
			DefaultConfig.TryAdd("icon_drop_seconds", 0.1f);
			DefaultConfig.TryAdd("icon_stay_seconds", 1.0f);
			DefaultConfig.TryAdd("icon_fadeout_seconds", 1.25f);
			DefaultConfig.TryAdd("is_dont_use_headshot_icon_if_combo", false);
			Instance = this;
			if (Loaded)
			{
				return;
			}
			if (LoadRes())
			{
				UnityEngine.Debug.Log("CFKillFeedback: 已载入/Loaded");
				Loaded = true;

            }
			else
			{
				UnityEngine.Debug.LogError("CFKillFeedback: 载入资源时出现问题/Something wrong when loading resources");
			}
		}
		private void OnEnable()
		{
			Health.OnDead += OnDead;
			// 读取或创建配置文件
			string config_path = Path.Combine(Application.streamingAssetsPath, "CFKillFeedback.cfg");
            if (File.Exists(config_path))
			{
				string config_content = File.ReadAllText(config_path);
                JObject? config_parsed = JsonConvert.DeserializeObject<JObject>(config_content);
				if (config_parsed != null)
				{
					foreach (JProperty property in config_parsed.Properties())
					{
						if (property.Name == "volume" && property.Value.Type == JTokenType.Float)
						{
							volume = (float)property.Value;
							continue;
						}
                        if (property.Name == "simple_sfx" && property.Value.Type == JTokenType.Boolean)
                        {
                            simple_sfx = (bool)property.Value;
                            continue;
                        }
                        if (property.Name == "disable_icon" && property.Value.Type == JTokenType.Boolean)
                        {
                            disable_icon = (bool)property.Value;
                            continue;
                        }
						if (property.Name == "icon_size_multi" && property.Value.Type == JTokenType.Float)
						{
							icon_size_multi = (float)property.Value;
							continue;
						}
						if (property.Name == "icon_alpha" && property.Value.Type == JTokenType.Float)
						{
							icon_alpha = (float)property.Value;
							continue;
						}
						if (property.Name == "combo_seconds" && property.Value.Type == JTokenType.Float)
						{
							combo_seconds = (float)property.Value;
							continue;
						}
						if (property.Name == "icon_drop_seconds" && property.Value.Type == JTokenType.Float)
						{
							icon_drop_seconds = (float)property.Value;
							continue;
						}
						if (property.Name == "icon_stay_seconds" && property.Value.Type == JTokenType.Float)
						{
							icon_stay_seconds = (float)property.Value;
							continue;
						}
						if (property.Name == "icon_fadeout_seconds" && property.Value.Type == JTokenType.Float)
						{
							icon_fadeout_seconds = (float)property.Value;
							continue;
						}
						if (property.Name == "is_dont_use_headshot_icon_if_combo" && property.Value.Type == JTokenType.Boolean)
						{
							is_dont_use_headshot_icon_if_combo = (bool)property.Value;
							continue;
						}
					}
                }
				else
				{
					UnityEngine.Debug.LogError("CFKillFeedback: 读取配置文件时出错/Failed to read config file");
				}
			}
			else
			{
				File.WriteAllText(config_path, Newtonsoft.Json.JsonConvert.SerializeObject(DefaultConfig, Formatting.Indented));
			}
		}
		private void OnDisable()
		{
			Health.OnDead -= OnDead;
		}
		private void OnDestroy()
		{
			if (ui_transform != null)
			{
				UnityEngine.Object.Destroy(ui_transform.gameObject);
			}
		}
		// 加载资源方法，返回成功与否
		public bool LoadRes()
		{
			UnityEngine.Debug.Log("CFKillFeedback: 开始加载资源/Starting loading resources");
			bool success = true;
			string absolute_path = Path.Combine(Utils.GetDllDirectory(), "CustomPacks", "cf");
			UnityEngine.Debug.Log("CFKillFeedback: Absolute path = " + absolute_path);
			UnityEngine.Debug.Log("CFKillFeedback: 正在遍历图标名称列表/Foreaching IconNames list");
			foreach (string icon_name in IconNames)
			{
				string this_path = Path.Combine(absolute_path, icon_name + ".png");
				UnityEngine.Debug.Log("CFKillFeedback: Now path is " + this_path);
				if (!File.Exists(this_path))
				{
					UnityEngine.Debug.LogError("CFKillFeedback: 文件不存在 = " + this_path);
					success = false;
					continue;
				}
				byte[] icon_bytes = File.ReadAllBytes(this_path);
				Texture2D icon_texture = new Texture2D(256, 256);
				if (icon_texture.LoadImage(icon_bytes))
				{
					KillFeedbackIcons.TryAdd(icon_name, icon_texture);
					success = success && true;
					UnityEngine.Debug.Log("CFKillFeedback: 纹理加载成功 = " + this_path);
					continue;
				}
				success = false;
				UnityEngine.Debug.LogError("CFKillFeedback: 加载纹理失败/Failed to load texture = " + this_path);
			}
			UnityEngine.Debug.Log("CFKillFeedback: 正在遍历音频名称列表/Foreaching AudioNames list");
			foreach (string audio_name in AudioNames)
			{
				string this_path = Path.Combine(absolute_path, audio_name + ".wav");
				UnityEngine.Debug.Log("CFKillFeedback: Now path is " + this_path);
				if (!File.Exists(this_path))
				{
					UnityEngine.Debug.LogError("CFKillFeedback: 文件不存在 = " + this_path);
					success = false;
					continue;
				}
				//UnityEngine.Debug.Log("CFKillFeedback: 将异步读取音频 = " + this_path);
				//StartCoroutine(GetAudioClip("file:///" + this_path.Replace(" ", "%20"), audio_name));
				RESULT fmod_create_result = RuntimeManager.CoreSystem.createSound(this_path, MODE.LOOP_OFF, out Sound sound);
				if (fmod_create_result == RESULT.OK)
				{
					KillFeedbackAudios_FMOD.TryAdd(audio_name, sound);
					success = success && true;
					UnityEngine.Debug.Log("CFKillFeedback: 成功加载音频 = " + this_path);
				}
				else
				{
					UnityEngine.Debug.LogError("CFKillFeedback: 加载音频时出错 = " + fmod_create_result.ToString());
					success = false;
				}
			}
			return success;
		}
		// 创建UI
		public void CreateUI()
		{
			HUDManager hud_manager = UnityEngine.Object.FindObjectOfType<HUDManager>();
			if (hud_manager == null)
			{
				return;
			}
			GameObject game_object = new GameObject("CFKillFeedbackUI");
			ui_transform = game_object.AddComponent<RectTransform>();
			ui_image = game_object.AddComponent<Image>();
			ui_image.preserveAspect = true;
			ui_canvasgroup = game_object.AddComponent<CanvasGroup>();
			ui_transform.SetParent(hud_manager.transform);
			UnityEngine.Debug.Log("CFKillFeedback: 已创建UI/UI created");
		}
    }
}
