using ICities;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;
using System;
using System.IO;

public class OverLayer : IUserMod
{
	public string Name
	{
		get { return "OverLayer"; }
	}

	public string Description
	{
		get { return "Draws a high resolution picture over your map which follows the terrain height."; }
	}
}

public class OverLayerExtension : LoadingExtensionBase
{
	private const String c_filename = "Files/overlay.png";

	private bool m_active;
	private UIButton m_button;
	private Texture2D[] m_originalMaps;

	private DateTime m_lastBrytesWrite;
	private byte[] m_lastBytes;

	public override void OnLevelLoaded(LoadMode p_mode)
	{
		// Get the UIView object. This seems to be the top-level object for most
		// of the UI.
		var l_uiView = UIView.GetAView();

		// Add a new button to the view.
		m_button = (UIButton)l_uiView.AddUIComponent(typeof(UIButton));

		// Set the text to show on the button tooltip.
		m_button.tooltip = "Terrain Contour";
		m_button.tooltipAnchor = UITooltipAnchor.Floating;
		m_button.RefreshTooltip();

		// Set the button dimensions.
		m_button.width = 42;
		m_button.height = 42;

		// Style the button to look like a menu button.
		m_button.normalBgSprite = "OptionBase";
		m_button.disabledBgSprite = "OptionBaseDisabled";
		m_button.hoveredBgSprite = "OptionBaseHovered";
		m_button.focusedBgSprite = "OptionBaseFocused";
		m_button.pressedBgSprite = "OptionBasePressed";
		m_button.normalFgSprite = "RoadOptionUpgrade";
		m_button.hoveredFgSprite = "RoadOptionUpgradeHovered";
		m_button.focusedFgSprite = "RoadOptionUpgradeFocused";
		m_button.pressedFgSprite = "RoadOptionUpgradePressed";

		m_button.textColor = new Color32(255, 255, 255, 255);
		m_button.disabledTextColor = new Color32(7, 7, 7, 255);
		m_button.hoveredTextColor = new Color32(7, 132, 255, 255);
		m_button.focusedTextColor = new Color32(255, 255, 255, 255);
		m_button.pressedTextColor = new Color32(30, 30, 44, 255);

		// Enable button sounds.
		m_button.playAudioEvents = true;

		// Place the button.
		m_button.transformPosition = new Vector3(-1.11f, 0.98f);

		// Respond to button click.
		m_button.eventClicked += ButtonClick;
	}

	public override void OnLevelUnloading()
	{
		if (m_active)
		{
			ToggleActive();
		}
	}

	private void ButtonClick(UIComponent p_component, UIMouseEventParameter p_eventParam)
	{
		ToggleActive();
	}

	private void ToggleActive()
	{
		if (!m_active)
		{
			int l_tileSize = Singleton<TerrainManager>.instance.m_patches[0].m_surfaceMapA.width;
			byte[] l_bytes = GetOverlayBytes();

			if (l_bytes == null) return;

			Texture2D l_overlay = new Texture2D(l_tileSize * 9, l_tileSize * 9);
			l_overlay.LoadImage(l_bytes);

			m_originalMaps = new Texture2D[Singleton<TerrainManager>.instance.m_patches.Length];

			int i = 0;
			foreach (TerrainPatch terrainPatch in Singleton<TerrainManager>.instance.m_patches)
			{
				m_originalMaps[i] = terrainPatch.m_surfaceMapB;
				terrainPatch.m_surfaceMapB = GetSubOverlay(l_overlay, terrainPatch.m_x, terrainPatch.m_z);
				i++;
			}

			m_active = true;
			m_button.state = UIButton.ButtonState.Focused;
		}
		else
		{
			int i = 0;
			foreach (TerrainPatch terrainPatch in Singleton<TerrainManager>.instance.m_patches)
			{
				terrainPatch.m_surfaceMapB = m_originalMaps[i];
				i++;
			}
			m_active = false;
			m_button.state = UIButton.ButtonState.Normal;
			m_button.Unfocus();
		}
	}

	private byte[] GetOverlayBytes()
	{
		try
		{
			DateTime l_newBytesWrite = File.GetLastWriteTime(c_filename);


			if (m_lastBrytesWrite != null && m_lastBytes != null)
			{
				if (m_lastBrytesWrite.Equals(l_newBytesWrite))
				{
					return m_lastBytes;
				}
			}

			m_lastBrytesWrite = l_newBytesWrite;
			m_lastBytes = File.ReadAllBytes(c_filename);

			return m_lastBytes;
		}
		catch (Exception p_exception)
		{
			DebugLog("Got exception: " + p_exception.Message);
			return null;
		}
	}

	Texture2D GetSubOverlay(Texture2D p_overlayImage, int p_X, int p_Y)
	{
		int l_amplitudeX = (int)Math.Floor(p_overlayImage.width / 9.0);
		int l_amplitudeY = (int)Math.Floor(p_overlayImage.height / 9.0);
		int l_offsetX = (int)Math.Floor(l_amplitudeX * 0.0625);
		int l_offsetY = (int)Math.Floor(l_amplitudeY * 0.0625);

		Texture2D l_newTexture = new Texture2D(l_amplitudeX + l_offsetX * 2, l_amplitudeY + l_offsetY * 2);

		for (int x = 0; x < l_amplitudeX + l_offsetX * 2; x++)
		{
			for (int y = 0; y < l_amplitudeY + l_offsetY * 2; y++)
			{
				l_newTexture.SetPixel(x, y, p_overlayImage.GetPixel(	p_X * l_amplitudeX + x - l_offsetX,
																		p_Y * l_amplitudeY + y - l_offsetY));
			}
		}
		l_newTexture.Apply();

		return l_newTexture;
	}

	public void DebugLog(String p_message)
	{
		DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, "[OverLayer] " + p_message);
	}
}