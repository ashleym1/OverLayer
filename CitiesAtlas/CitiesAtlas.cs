using ICities;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;
using System.Collections;
using ColossalFramework.Plugins;
using System;
using System.IO;

public class OverLayer : IUserMod {
	
	public string Name {
		get { return "OverLayer"; }
	}
	
	public string Description {
		get { return "Overlayer draws a high resolution picture over your map which follows the terrain height."; }
	}
}

public class OverLayerExtension : LoadingExtensionBase
{
	private bool active;
    private UIButton button;
	private Texture2D[] originalMaps;
	
	public override void OnLevelLoaded(LoadMode mode)
	{
		// Get the UIView object. This seems to be the top-level object for most
		// of the UI.
		var uiView = UIView.GetAView();
		
		// Add a new button to the view.
		button = (UIButton)uiView.AddUIComponent(typeof(UIButton));

        // Set the text to show on the button tooltip.
        button.tooltip = "Terrain Contour";
        button.tooltipAnchor = UITooltipAnchor.Floating;
        button.RefreshTooltip();

        // Set the button dimensions.
        button.width = 42;
        button.height = 42;

        // Style the button to look like a menu button.
        button.normalBgSprite = "OptionBase";
        button.disabledBgSprite = "OptionBaseDisabled";
        button.hoveredBgSprite = "OptionBaseHovered";
        button.focusedBgSprite = "OptionBaseFocused";
        button.pressedBgSprite = "OptionBasePressed";
        button.normalFgSprite = "RoadOptionUpgrade";
        button.hoveredFgSprite = "RoadOptionUpgradeHovered";
        button.focusedFgSprite = "RoadOptionUpgradeFocused";
        button.pressedFgSprite = "RoadOptionUpgradePressed";

        button.textColor = new Color32(255, 255, 255, 255);
        button.disabledTextColor = new Color32(7, 7, 7, 255);
        button.hoveredTextColor = new Color32(7, 132, 255, 255);
        button.focusedTextColor = new Color32(255, 255, 255, 255);
        button.pressedTextColor = new Color32(30, 30, 44, 255);

        // Enable button sounds.
        button.playAudioEvents = true;
		
		// Place the button.
		button.transformPosition = new Vector3(-1.11f, 0.98f);
		
		// Respond to button click.
		button.eventClicked += ButtonClick;
	}

    private void ButtonClick(UIComponent component, UIMouseEventParameter eventParam)
	{
		if (!active)
        {
            int l_tileSize = Singleton<TerrainManager>.instance.m_patches[0].m_surfaceMapA.width;

            byte[] bytes = File.ReadAllBytes("Files/overlay.png");
            if (bytes == null)
            {
                return;
            }

            Texture2D l_overlay = new Texture2D(l_tileSize * 9, l_tileSize * 9);
            l_overlay.LoadImage(bytes);

            originalMaps = new Texture2D[Singleton<TerrainManager>.instance.m_patches.Length];
			int i = 0;
			foreach(TerrainPatch terrainPatch in Singleton<TerrainManager>.instance.m_patches)
			{
                debug("Tilesize: (" + terrainPatch.m_surfaceMapA.width + ";" + terrainPatch.m_surfaceMapA.height + ")");

                originalMaps[i] = terrainPatch.m_surfaceMapB;

				terrainPatch.m_surfaceMapB = getSubOverlay(l_overlay, terrainPatch.m_x, terrainPatch.m_z);
				i++;
			}
			active = true;
            button.state = UIButton.ButtonState.Focused;
		}
        else
        {
			int i = 0;
			foreach(TerrainPatch terrainPatch in Singleton<TerrainManager>.instance.m_patches)
			{
				terrainPatch.m_surfaceMapB = originalMaps[i];
				i++;
			}
			active = false;
            button.state = UIButton.ButtonState.Normal;
            button.Unfocus();
        }
		
	}
	
	Texture2D getSubOverlay(Texture2D p_overlayImage, int p_X, int p_Y)
	{
        int l_amplitudeX = (int) Math.Floor(p_overlayImage.width / 9.0);
        int l_amplitudeY = (int) Math.Floor(p_overlayImage.height / 9.0);

        Texture2D l_newTexture = new Texture2D(l_amplitudeX, l_amplitudeY);

        debug("Tile from (" + (p_X * l_amplitudeX) + "," + (p_Y * l_amplitudeY) + ") to (" + (p_X * l_amplitudeX + l_amplitudeX - 1) + "," + (p_Y * l_amplitudeY + l_amplitudeY - 1) + ")");

        for (int x = 0; x < l_amplitudeX; x++)
        {
			for(int y = 0; y < l_amplitudeY; y++)
            {
				l_newTexture.SetPixel(x, y, p_overlayImage.GetPixel(p_X * l_amplitudeX + x, p_Y * l_amplitudeY + y));
			}
		}
        l_newTexture.Apply();

        return l_newTexture;
	}
	
	public void debug(String message) 
	{
		DebugOutputPanel.AddMessage (ColossalFramework.Plugins.PluginManager.MessageType.Message, message);
	}
}
