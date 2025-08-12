using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Common.Math;
using System;
using System.Runtime.InteropServices;
using static EasyZoomReborn.Configuration;
using static EasyZoomReborn.EasyZoomRebornPlugin;

namespace EasyZoomReborn
{
	public class SettingsWindow : Window
	{
		private Vector2 _windowPos = Vector2.Zero;
		private static float Scale => ImGuiHelpers.GlobalScale;
		private readonly string imageUrl = "https://raw.githubusercontent.com/FFXIV-CombatReborn/RebornAssets/main/IconAssets/EZR_Icon.png";	
		
		public override void Draw()
		{
			_windowPos = ImGui.GetWindowPos();

            if (ImGui.BeginChild("LeftSide", new Vector2(125, 0), true))  // Left-child window
			{
                if (Utils.TryGetTextureWrap(imageUrl, out var texture) && texture != null)
                {
                    // Calculate padding for ImGui image
                    Vector2 windowSize = ImGui.GetContentRegionAvail();
                    Vector2 imageSize = new(texture.Width / 3 * Scale, texture.Height / 3 * Scale);
                    Vector2 padding = (windowSize - imageSize) * 0.5f;
                    ImGui.Dummy(new Vector2(0, padding.Y - 20.0f));
                    ImGui.SameLine(padding.X);
                    ImGui.Image(texture.Handle, imageSize);
                }

                // Center the ImGui button
                Vector2 buttonSize = new(104 * Scale, 24 * Scale);
				float buttonPosX = (ImGui.GetContentRegionAvail().X - buttonSize.X) * 0.5f;
				ImGui.SetCursorPosX(buttonPosX);
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


        public static void DrawGeneralTab()
        {
            //if (ImGui.Checkbox("Disable camera collision", ref EasyZoomRebornPlugin.Configuration.NoCollision))
            //{
            //	SetCamNoCollision(EasyZoomRebornPlugin.Configuration.NoCollision);
            //	EasyZoomRebornPlugin.Configuration.Save();
            //}

            ImGui.BeginDisabled(true);
            ImGui.Checkbox("Disable camera collision (Currently broken)", ref EasyZoomRebornPlugin.Configuration.NoCollision2);
            ImGui.EndDisabled();

            float fovCurrent = Marshal.PtrToStructure<float>(FovCurrent);
            float fovMin = Marshal.PtrToStructure<float>(FovMin);
            float fovMax = Marshal.PtrToStructure<float>(FovMax);
            if (ImGui.SliderFloat("FOV", ref fovCurrent, fovMin, fovMax, $"{fovCurrent} ({fovCurrent * (180 / Math.PI):F2}°)"))
            {
                Marshal.StructureToPtr(fovCurrent, FovCurrent, true);
            }
            if (ImGui.IsItemHovered() && ImGui.IsMouseDown(ImGuiMouseButton.Right))
                Marshal.StructureToPtr(FovDefault, FovCurrent, true);

            float fovMinLocal = Marshal.PtrToStructure<float>(FovMin);
            float zeroFloat = Marshal.PtrToStructure<float>(ZeroFloat);
            float piFloat = Marshal.PtrToStructure<float>(PiFloat);
            if (ImGui.DragFloat("FOV Min", ref fovMinLocal, 0.005f, zeroFloat, piFloat, $"{fovMinLocal} ({fovMinLocal * (180 / Math.PI):F2}°)"))
            {
                Marshal.StructureToPtr(fovMinLocal, FovMin, true);
                EasyZoomRebornPlugin.Configuration.FovMin = fovMinLocal;
                EasyZoomRebornPlugin.Configuration.Save();
            }
            if (ImGui.IsItemHovered() && ImGui.IsMouseDown(ImGuiMouseButton.Right))
            {
                Marshal.StructureToPtr(FovMinDefault, FovMin, true);
                EasyZoomRebornPlugin.Configuration.FovMin = Marshal.PtrToStructure<float>(FovMin);
                EasyZoomRebornPlugin.Configuration.Save();
            }

            float fovMaxLocal = Marshal.PtrToStructure<float>(FovMax);
            if (ImGui.DragFloat("FOV Max", ref fovMaxLocal, 0.005f, zeroFloat, piFloat, $"{fovMaxLocal} ({fovMaxLocal * (180 / Math.PI):F2}°)"))
            {
                Marshal.StructureToPtr(fovMaxLocal, FovMax, true);
                EasyZoomRebornPlugin.Configuration.FovMax = fovMaxLocal;
                EasyZoomRebornPlugin.Configuration.Save();
            }
            if (ImGui.IsItemHovered() && ImGui.IsMouseDown(ImGuiMouseButton.Right))
            {
                Marshal.StructureToPtr(FovMaxDefault, FovMax, true);
                EasyZoomRebornPlugin.Configuration.FovMax = Marshal.PtrToStructure<float>(FovMax);
                EasyZoomRebornPlugin.Configuration.Save();
            }

            ImGui.Spacing();

            float zoomCurrent = Marshal.PtrToStructure<float>(ZoomCurrent);
            float zoomMin = Marshal.PtrToStructure<float>(ZoomMin);
            float zoomMax = Marshal.PtrToStructure<float>(ZoomMax);
            if (ImGui.SliderFloat("Zoom", ref zoomCurrent, zoomMin, zoomMax, zoomCurrent.ToString(), ImGuiSliderFlags.Logarithmic))
            {
                Marshal.StructureToPtr(zoomCurrent, ZoomCurrent, true);
            }
            if (ImGui.IsItemHovered() && ImGui.IsMouseDown(ImGuiMouseButton.Right))
                Marshal.StructureToPtr(ZoomDefault, ZoomCurrent, true);

            float zoomMinLocal = Marshal.PtrToStructure<float>(ZoomMin);
            float maxFloat = Marshal.PtrToStructure<float>(MaxFloat);
            if (ImGui.DragFloat("Zoom Min", ref zoomMinLocal, 1f, zeroFloat, maxFloat, zoomMinLocal.ToString(), ImGuiSliderFlags.Logarithmic))
            {
                Marshal.StructureToPtr(zoomMinLocal, ZoomMin, true);
                EasyZoomRebornPlugin.Configuration.ZoomMin = zoomMinLocal;
                EasyZoomRebornPlugin.Configuration.Save();
            }
            if (ImGui.IsItemHovered() && ImGui.IsMouseDown(ImGuiMouseButton.Right))
            {
                Marshal.StructureToPtr(ZoomMinDefault, ZoomMin, true);
                EasyZoomRebornPlugin.Configuration.ZoomMin = Marshal.PtrToStructure<float>(ZoomMin);
                EasyZoomRebornPlugin.Configuration.Save();
            }

            float zoomMaxLocal = Marshal.PtrToStructure<float>(ZoomMax);
            if (ImGui.DragFloat("Zoom Max", ref zoomMaxLocal, 1f, zeroFloat, maxFloat, zoomMaxLocal.ToString(), ImGuiSliderFlags.Logarithmic))
            {
                Marshal.StructureToPtr(zoomMaxLocal, ZoomMax, true);
                EasyZoomRebornPlugin.Configuration.ZoomMax = zoomMaxLocal;
                EasyZoomRebornPlugin.Configuration.Save();
            }
            if (ImGui.IsItemHovered() && ImGui.IsMouseDown(ImGuiMouseButton.Right))
            {
                Marshal.StructureToPtr(ZoomMaxDefault, ZoomMax, true);
                EasyZoomRebornPlugin.Configuration.ZoomMax = Marshal.PtrToStructure<float>(ZoomMax);
                EasyZoomRebornPlugin.Configuration.Save();
            }

            float lookAtHeightOffset = Marshal.PtrToStructure<float>(LookAtHeightOffset);
            float minFloatHeight = Marshal.PtrToStructure<float>(MinFloatHeight);
            float maxFloatHeight = Marshal.PtrToStructure<float>(MaxFloatHeight);
            if (ImGui.DragFloat("Look At Height Offset", ref lookAtHeightOffset, 0.05f, minFloatHeight, maxFloatHeight, lookAtHeightOffset.ToString(), ImGuiSliderFlags.Logarithmic))
            {
                Marshal.StructureToPtr(lookAtHeightOffset, LookAtHeightOffset, true);
                EasyZoomRebornPlugin.Configuration.LookAtHeightOffset = lookAtHeightOffset;
                EasyZoomRebornPlugin.Configuration.Save();
            }
            if (ImGui.IsItemHovered() && ImGui.IsMouseDown(ImGuiMouseButton.Right))
            {
                Marshal.StructureToPtr(LookAtHeightOffsetDefault, LookAtHeightOffset, true);
                EasyZoomRebornPlugin.Configuration.LookAtHeightOffset = Marshal.PtrToStructure<float>(LookAtHeightOffset);
                EasyZoomRebornPlugin.Configuration.Save();
            }
            ImGui.Spacing();
        }

        public override void OnClose()
		{
			EasyZoomRebornPlugin.Configuration.Save();
		}

		public SettingsWindow(string name) : base(name)
		{
			Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollWithMouse;
			Size = new Vector2(600 * Scale, 300 * Scale);
            TitleBarButtons.Add(new TitleBarButton()
            {
                Icon = FontAwesomeIcon.Heart,
                ShowTooltip = () =>
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("Support the developer on Ko-fi");
                    ImGui.EndTooltip();
                },
                Priority = 2,
                Click = _ =>
                {
                    try
                    {
                        Util.OpenLink("https://ko-fi.com/ltscombatreborn");
                    }
                    catch
                    {
                        // ignored
                    }
                },
                AvailableClickthrough = true
            });
        }
	}
}