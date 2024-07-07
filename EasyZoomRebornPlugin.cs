using System;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace EasyZoomReborn
{
	public unsafe class EasyZoomRebornPlugin : IDalamudPlugin
	{
		public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
		public static IPluginLog PluginLog { get; private set; } = null!;
		public static IClientState ClientState { get; private set; } = null!;
		public static ICommandManager CommandManager { get; private set; } = null!;
		public static ISigScanner SigScanner { get; private set; } = null!;
		public static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
		public static UiBuilder UiBuilder { get; private set; } = null!;

		public static ITextureProvider TextureProvider { get; private set; } = null!;


        public static Configuration Configuration;
        private static WindowSystem _windowSystem = null!;
		private  SettingsWindow _settingsWindow;

		private static readonly CameraManager* CameraManager = (CameraManager*)FFXIVClientStructs.FFXIV.Client.Game.Control.CameraManager.Instance();
        private static IntPtr _camCollisionJmp;
		private static IntPtr _camDistanceResetFunc;
		private static readonly byte[] CamDistanceOriginalBytes = new byte[8];

        

        public static float ZoomDelta = 0.75f;
        private delegate float GetZoomDeltaDelegate();
        private static Hook<GetZoomDeltaDelegate> _getZoomDeltaHook;
        private static float GetZoomDeltaDetour()
        {
            return Cam->currentZoom * 0.075f;
        }

        public string Name => "EasyZoomReborn";

		public EasyZoomRebornPlugin(
			IClientState clientState,
			ICommandManager commandManager,
			IDalamudPluginInterface pluginInterface,
			ISigScanner sigScanner,
			IGameInteropProvider gameInteropProvider,
			IPluginLog pluginLog,
			ITextureProvider textureProvider
			
		)
		{
			ClientState = clientState;
			CommandManager = commandManager;
			PluginInterface = pluginInterface;
			GameInteropProvider = gameInteropProvider;
			SigScanner = sigScanner;
			UiBuilder = (UiBuilder)PluginInterface.UiBuilder;
			PluginLog = pluginLog;
			TextureProvider = textureProvider;

			ZeroFloat = Marshal.AllocHGlobal(4);
			Marshal.StructureToPtr(0f, ZeroFloat, true);
			MaxFloat = Marshal.AllocHGlobal(4);
			Marshal.StructureToPtr(10000f, MaxFloat, true);
			PiFloat = Marshal.AllocHGlobal(4);
			Marshal.StructureToPtr((float)Math.PI, PiFloat, true);
			MinFloatHeight = Marshal.AllocHGlobal(4);
			Marshal.StructureToPtr(-3f, MinFloatHeight, true);
			MaxFloatHeight = Marshal.AllocHGlobal(4);
			Marshal.StructureToPtr(3f, MaxFloatHeight, true);

			Configuration = (Configuration)pluginInterface.GetPluginConfig() ?? new Configuration();
			Configuration.Initialize(pluginInterface);

			CreateWindows();

			UiBuilder.Draw += Draw;
			UiBuilder.OpenConfigUi += OpenConfig;

			CommandManager.AddHandler(
				"/ez",
				new CommandInfo(CommandSettings)
				{
					HelpMessage = "Opens the EasyZoomReborn configuration window.",

					ShowInHelp = true
				}
			);
			
			
			_camCollisionJmp = sigScanner.ScanText("E8 ?? ?? ?? ?? 4C 8D 45 97 89 83 ?? ?? ?? ??") + 0x1D9; // new
			_camDistanceResetFunc = sigScanner.ScanText("F3 0F 10 15 ?? ?? ?? ?? EB 0F"); // nop 8 bytes
			
			Marshal.Copy(_camDistanceResetFunc, CamDistanceOriginalBytes, 0, 8);


			ClientState.Login += ClientState_OnLogin;

			SetCamDistanceNoReset(true);
			if (Configuration.NoCollision)
			{
				SetCamNoCollision(true);
			}

			Marshal.StructureToPtr(-1.569f, AngleMin, true);
			Marshal.StructureToPtr(1.569f, AngleMax, true);

			Marshal.StructureToPtr(Configuration.FovMin, FovMin, true);
			Marshal.StructureToPtr(Configuration.FovMax, FovMax, true);
			Marshal.StructureToPtr(Configuration.ZoomMin, ZoomMin, true);
			Marshal.StructureToPtr(Configuration.ZoomMax, ZoomMax, true);
			Marshal.StructureToPtr(Configuration.LookAtHeightOffset,LookAtHeightOffset,true);

			Hook();
		}

		private void Hook()
		{
            var vtbl = CameraManager->worldCamera->vtbl;
            _getZoomDeltaHook = GameInteropProvider.HookFromAddress<GetZoomDeltaDelegate>(vtbl[28], GetZoomDeltaDetour);
            _getZoomDeltaHook.Enable();
        }

		private void ClientState_OnLogin()
		{
			SetCamDistanceNoReset(true);
			if (Configuration.NoCollision)
			{
				SetCamNoCollision(true);
			}

			Marshal.StructureToPtr(-1.569f, AngleMin, true);
			Marshal.StructureToPtr(1.569f, AngleMax, true);

			Marshal.StructureToPtr(Configuration.FovMin, FovMin, true);
			Marshal.StructureToPtr(Configuration.FovMax, FovMax, true);
			Marshal.StructureToPtr(Configuration.ZoomMin, ZoomMin, true);
			Marshal.StructureToPtr(Configuration.ZoomMax, ZoomMax, true);
			Marshal.StructureToPtr(Configuration.LookAtHeightOffset,LookAtHeightOffset,true);
		}
        private static GameCamera* Cam => CameraManager->worldCamera;

        public static IntPtr ZoomCurrent => (IntPtr)(&Cam->currentZoom);
		public static IntPtr ZoomMin => (IntPtr)(&Cam->minZoom);
		public static IntPtr ZoomMax => (IntPtr)(&Cam->maxZoom);
		public static IntPtr FovCurrent => (IntPtr)(&Cam->maxFoV);
		public static IntPtr FovMin => (IntPtr)(&Cam->minFoV);
		public static IntPtr FovMax => (IntPtr)(&Cam->currentFoV);
        public static IntPtr AngleMin => (IntPtr)(&Cam->minVRotation);
        public static IntPtr AngleMax => (IntPtr)(&Cam->maxVRotation);

        public static IntPtr LookAtHeightOffset => (IntPtr)(&Cam->lookAtHeightOffset);


        public static IntPtr ZeroFloat;
		public static IntPtr PiFloat;
		public static IntPtr MaxFloat;
		public static IntPtr MinFloatHeight;
		public static IntPtr MaxFloatHeight;

		internal static void SetCamDistanceNoReset(bool on)
		{
			Dalamud.SafeMemory.WriteBytes(_camDistanceResetFunc, @on ? Enumerable.Repeat((byte)0x90, 8).ToArray() : CamDistanceOriginalBytes);
		}

		internal static void SetCamNoCollision(bool on)
		{
			Dalamud.SafeMemory.WriteBytes(_camCollisionJmp, on ? [0x90, 0xE9] : [0x0f, 0x84]);
		}

		
		public void CommandSettings(string command, string args)
		{
			_settingsWindow.IsOpen = true;
		}

		private void CreateWindows()
		{
			_settingsWindow = new SettingsWindow($"{Name} Settings");
			_windowSystem = new WindowSystem("EasyZoomReborn_Windows");
			_windowSystem.AddWindow(_settingsWindow);
		}

		private void OpenConfig()
		{
			_settingsWindow.IsOpen = true;
		}

		private void Draw()
		{
			if (Configuration == null || ClientState.LocalPlayer == null) return;
			_windowSystem?.Draw();
		}

		#region IDisposable Support
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing) return;

			ClientState.Login -= ClientState_OnLogin;

			SetCamDistanceNoReset(false);
			SetCamNoCollision(false);

			ResetFovs();
			ResetZooms();
			Marshal.StructureToPtr(Configuration.AngleMinDefault, AngleMin, true);
			Marshal.StructureToPtr(Configuration.AngleMaxDefault, AngleMax, true);


			PluginInterface.SavePluginConfig(Configuration);
			
			_windowSystem.RemoveAllWindows();

			CommandManager.RemoveHandler("/ez");

			UiBuilder.Draw -= Draw;
			UiBuilder.OpenConfigUi -= OpenConfig;
            
			Marshal.FreeHGlobal(ZeroFloat);
			Marshal.FreeHGlobal(MaxFloat);
			Marshal.FreeHGlobal(PiFloat);
			Marshal.FreeHGlobal(MinFloatHeight);
			Marshal.FreeHGlobal(MaxFloatHeight);
            _getZoomDeltaHook?.Dispose();
            
		}

		private static void ResetZooms(bool resetCurrent = false)
		{
			if (resetCurrent)
			{
				Marshal.StructureToPtr(Configuration.ZoomMaxDefault, ZoomCurrent, true);
			}
			Marshal.StructureToPtr(Configuration.ZoomMinDefault, ZoomMin, true);
			Marshal.StructureToPtr(Configuration.ZoomMaxDefault, ZoomMax, true);
		}

		private static void ResetFovs()
		{
			Marshal.StructureToPtr(Configuration.FovMinDefault, FovMin, true);
			Marshal.StructureToPtr(Configuration.FovMaxDefault, FovMax, true);
			Marshal.StructureToPtr(Configuration.FovMaxDefault, FovCurrent, true);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}


    /// <summary>
    /// https://github.com/UnknownX7/Cammy/blob/master/Structures/CameraManager.cs
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct CameraManager
    {
        [FieldOffset(0x0)] public GameCamera* worldCamera;
        [FieldOffset(0x8)] public GameCamera* idleCamera;
        [FieldOffset(0x10)] public GameCamera* menuCamera;
        [FieldOffset(0x18)] public GameCamera* spectatorCamera;
    }

    /// <summary>
    /// https://github.com/UnknownX7/Cammy/blob/master/Structures/GameCamera.cs
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct GameCamera
    {
        [FieldOffset(0x0)] public nint* vtbl;
        [FieldOffset(0x60)] public float x;
        [FieldOffset(0x64)] public float y;
        [FieldOffset(0x68)] public float z;
        [FieldOffset(0x90)] public float lookAtX; // Position that the camera is focused on (Actual position when zoom is 0)
        [FieldOffset(0x94)] public float lookAtY;
        [FieldOffset(0x98)] public float lookAtZ;
        [FieldOffset(0x114)] public float currentZoom; // 6
        [FieldOffset(0x118)] public float minZoom; // 1.5
        [FieldOffset(0x11C)] public float maxZoom; // 20
        [FieldOffset(0x120)] public float maxFoV; // 0.78
        [FieldOffset(0x124)] public float minFoV; // 0.69
        [FieldOffset(0x128)] public float currentFoV; // 0.78
        [FieldOffset(0x12C)] public float addedFoV; // 0
        [FieldOffset(0x130)] public float currentHRotation; // -pi -> pi, default is pi
        [FieldOffset(0x134)] public float currentVRotation; // -0.349066
        [FieldOffset(0x138)] public float hRotationDelta;
        [FieldOffset(0x148)] public float minVRotation; // -1.483530, should be -+pi/2 for straight down/up but camera breaks so use -+1.569
        [FieldOffset(0x14C)] public float maxVRotation; // 0.785398 (pi/4)
        [FieldOffset(0x160)] public float tilt;
        [FieldOffset(0x170)] public int mode; // Camera mode? (0 = 1st person, 1 = 3rd person, 2+ = weird controller mode? cant look up/down)
        [FieldOffset(0x174)] public int controlType; // 0 first person, 1 legacy, 2 standard, 4 talking to npc in first person (with option enabled), 5 talking to npc (with option enabled), 3/6 ???
        [FieldOffset(0x17C)] public float interpolatedZoom;
        [FieldOffset(0x190)] public float transition; // Seems to be related to the 1st <-> 3rd camera transition
        [FieldOffset(0x1B0)] public float viewX;
        [FieldOffset(0x1B4)] public float viewY;
        [FieldOffset(0x1B8)] public float viewZ;
        [FieldOffset(0x1E4)] public byte isFlipped; // 1 while holding the keybind
        [FieldOffset(0x21C)] public float interpolatedY;
        [FieldOffset(0x224)] public float lookAtHeightOffset; // No idea what to call this (0x230 is the interpolated value)
        [FieldOffset(0x228)] public byte resetLookatHeightOffset; // No idea what to call this
        [FieldOffset(0x230)] public float interpolatedLookAtHeightOffset;
        [FieldOffset(0x2B0)] public byte lockPosition;
        [FieldOffset(0x2C4)] public float lookAtY2;
    }
}
