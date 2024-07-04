using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;

namespace EasyZoomReborn
{
	public class Configuration : IPluginConfiguration
	{
		[JsonIgnore] public static readonly float FovDefault = 0.7799999714f;
		[JsonIgnore] public static readonly float FovMinDefault = 0.6899999976f;
		[JsonIgnore] public static readonly float FovMaxDefault = 0.7799999714f;
		[JsonIgnore] public static readonly float ZoomDefault = 6f;
		[JsonIgnore] public static readonly float ZoomMinDefault = 1.5f;
		[JsonIgnore] public static readonly float ZoomMaxDefault = 20f;
		[JsonIgnore] public static readonly float AngleMinDefault = -1.483529806f;
		[JsonIgnore] public static readonly float AngleMaxDefault = 0.7853981853f;

		public int Version { get; set; }

		public bool Enabled = true;
		public bool NoCollision;

		public float Fov = FovDefault;
		public float FovMin = FovMinDefault;
		public float FovMax = FovMaxDefault;

		public float Zoom = ZoomDefault;
		public float ZoomMin = ZoomMinDefault;
		public float ZoomMax = ZoomMaxDefault;


		// Add any other properties or methods here.
		[JsonIgnore] private IDalamudPluginInterface _pluginInterface;

		public void Initialize(IDalamudPluginInterface dalamudPluginInterface)
		{
			this._pluginInterface = dalamudPluginInterface;
		}

		public void Save()
		{
			this._pluginInterface.SavePluginConfig(this);
		}
	}
}
