using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Common.Math;
using ImGuiNET;
using static EasyZoomReborn.EasyZoomRebornPlugin;
using static EasyZoomReborn.Configuration;

namespace EasyZoomReborn
{
	public class SettingsWindow : Window
	{
		private Vector2 _windowPos = Vector2.Zero;
		private float _scale => ImGuiHelpers.GlobalScale;
		
		
		public override void Draw()
		{
			_windowPos = ImGui.GetWindowPos();
        
			if (ImGui.BeginChild("LeftSide", new Vector2(125, 0), true))  // Left-child window
			{
				if (ImGui.Button("Support on Ko-fi", new Vector2(104 * _scale, 24 * _scale)))
				{
					OpenUrl("https://ko-fi.com/incognitowater");
				}
			}
			ImGui.EndChild();

			ImGui.SameLine();  // Important for drawing on the same line

			if (ImGui.BeginChild("RightSide", new Vector2(0, 0), true))  // Right-child window (tab bar)
			{
				if (ImGui.BeginTabBar("Settings"))
				{
					if (ImGui.BeginTabItem("General"))
					{
						DrawGeneralTab();
						ImGui.EndTabItem();
					}

					ImGui.EndTabBar();
				}
			}
			ImGui.EndChild();        
		}


		public void DrawGeneralTab()
		{
			if (ImGui.Checkbox("Disable camera collision", ref EasyZoomRebornPlugin.Configuration.NoCollision))
			{
				SetCamNoCollision(EasyZoomRebornPlugin.Configuration.NoCollision);
				EasyZoomRebornPlugin.Configuration.Save();
			}
			ImGui.SliderScalar("FOV", ImGuiDataType.Float, FovCurrent, FovMin, FovMax, $"{Marshal.PtrToStructure<float>(FovCurrent)} ({Marshal.PtrToStructure<float>(FovCurrent) * (180 / Math.PI):F2}°)");
			if (ImGui.IsItemHovered() && ImGui.IsMouseDown(ImGuiMouseButton.Right))
				Marshal.StructureToPtr(FovDefault, FovCurrent, true);

			if (ImGui.DragScalar("FOV Min", ImGuiDataType.Float, FovMin, 0.005f, ZeroFloat, PiFloat, $"{Marshal.PtrToStructure<float>(FovMin)} ({Marshal.PtrToStructure<float>(FovMin) * (180 / Math.PI):F2}°)"))
			{
				EasyZoomRebornPlugin.Configuration.FovMin = Marshal.PtrToStructure<float>(FovMin);
				EasyZoomRebornPlugin.Configuration.Save();
			}
			if (ImGui.IsItemHovered() && ImGui.IsMouseDown(ImGuiMouseButton.Right))
			{
				Marshal.StructureToPtr(FovMinDefault, FovMin, true);
				EasyZoomRebornPlugin.Configuration.FovMin = Marshal.PtrToStructure<float>(FovMin);
				EasyZoomRebornPlugin.Configuration.Save();
			}
			if (ImGui.DragScalar("FOV Max", ImGuiDataType.Float, FovMax, 0.005f, ZeroFloat, PiFloat, $"{Marshal.PtrToStructure<float>(FovMax)} ({Marshal.PtrToStructure<float>(FovMax) * (180 / Math.PI):F2}°)"))
			{
				EasyZoomRebornPlugin.Configuration.FovMax = Marshal.PtrToStructure<float>(FovMax);
				EasyZoomRebornPlugin.Configuration.Save();
			}
			if (ImGui.IsItemHovered() && ImGui.IsMouseDown(ImGuiMouseButton.Right))
			{
				Marshal.StructureToPtr(FovMaxDefault, FovMax, true);
				EasyZoomRebornPlugin.Configuration.FovMax = Marshal.PtrToStructure<float>(FovMax);
				EasyZoomRebornPlugin.Configuration.Save();
			}
			ImGui.Spacing();
			ImGui.SliderScalar("Zoom", ImGuiDataType.Float, ZoomCurrent, ZoomMin, ZoomMax, Marshal.PtrToStructure<float>(ZoomCurrent).ToString(), ImGuiSliderFlags.Logarithmic);
			if (ImGui.IsItemHovered() && ImGui.IsMouseDown(ImGuiMouseButton.Right))
				Marshal.StructureToPtr(ZoomDefault, ZoomCurrent, true);

			if (ImGui.DragScalar("Zoom Min", ImGuiDataType.Float, ZoomMin, 1f, ZeroFloat, MaxFloat, Marshal.PtrToStructure<float>(ZoomMin).ToString(), ImGuiSliderFlags.Logarithmic))
			{
				EasyZoomRebornPlugin.Configuration.ZoomMin = Marshal.PtrToStructure<float>(ZoomMin);
				EasyZoomRebornPlugin.Configuration.Save();
			}
			if (ImGui.IsItemHovered() && ImGui.IsMouseDown(ImGuiMouseButton.Right))
			{
				Marshal.StructureToPtr(ZoomMinDefault, ZoomMin, true);
				EasyZoomRebornPlugin.Configuration.ZoomMin = Marshal.PtrToStructure<float>(ZoomMin);
				EasyZoomRebornPlugin.Configuration.Save();
			}
			if (ImGui.DragScalar("Zoom Max", ImGuiDataType.Float, ZoomMax, 1f, ZeroFloat, MaxFloat, Marshal.PtrToStructure<float>(ZoomMax).ToString(), ImGuiSliderFlags.Logarithmic))
			{
				EasyZoomRebornPlugin.Configuration.ZoomMax = Marshal.PtrToStructure<float>(ZoomMax);
				EasyZoomRebornPlugin.Configuration.Save();
			}
			if (ImGui.IsItemHovered() && ImGui.IsMouseDown(ImGuiMouseButton.Right))
			{
				Marshal.StructureToPtr(ZoomMaxDefault, ZoomMax, true);
				EasyZoomRebornPlugin.Configuration.ZoomMax = Marshal.PtrToStructure<float>(ZoomMax);
				EasyZoomRebornPlugin.Configuration.Save();
			}
			ImGui.Spacing();
		}

		public override void OnClose()
		{
			EasyZoomRebornPlugin.Configuration.Save();
		}
		
		public static void OpenUrl(string url)
		{
			try
			{
				Process.Start(url);
			}
			catch
			{
				try
				{
					// hack because of this: https://github.com/dotnet/corefx/issues/10361
					if (RuntimeInformation.IsOSPlatform(osPlatform: OSPlatform.Windows))
					{
						url = url.Replace("&", "^&");
						Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
					}
					else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
					{
						Process.Start("xdg-open", url);
					}
				}
				catch (Exception e)
				{
					PluginLog.Error("Error trying to open url: " + e.Message);
				}
			}
		}

		public SettingsWindow(string name) : base(name)
		{
			Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollWithMouse;
			Size = new Vector2(400, 250);
		}
	}
}