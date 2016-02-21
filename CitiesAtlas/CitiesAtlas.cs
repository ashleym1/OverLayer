using ICities;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;
using System.Collections;
using ColossalFramework.Plugins;
using System;

public class CitiesAtlas : IUserMod {
	
	public string Name {
		get { return "Cities Atlas"; }
	}
	
	public string Description {
		get { return "Cities Atlas adds a height map overlay to Cities:Skylines. Now you can plan your cities around those hills:)."; }
	}
}

public class HeightMapExtension : LoadingExtensionBase
{
    private UIButton button;

	public override void OnLevelLoaded(LoadMode mode)
	{
		// Get the UIView object. This seems to be the top-level object for most
		// of the UI.
		var uiView = UIView.GetAView();
		
		// Add a new button to the view.
		button = (UIButton)uiView.AddUIComponent(typeof(UIButton));

        // Set the text to show on the button tooltip.
        button.tooltip = "Terrain Height";
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
        button.normalFgSprite = "InfoIconTerrainHeight";
        button.hoveredFgSprite = "InfoIconTerrainHeightHovered";
        button.focusedFgSprite = "InfoIconTerrainHeightFocused";
        button.pressedFgSprite = "InfoIconTerrainHeightPressed";

        button.textColor = new Color32(255, 255, 255, 255);
        button.disabledTextColor = new Color32(7, 7, 7, 255);
        button.hoveredTextColor = new Color32(7, 132, 255, 255);
        button.focusedTextColor = new Color32(255, 255, 255, 255);
        button.pressedTextColor = new Color32(30, 30, 44, 255);

        // Enable button sounds.
        button.playAudioEvents = true;
		
		// Place the button.
		button.transformPosition = new Vector3(-1.2f, 0.98f);
		
		// Respond to button click.
		button.eventClicked += ButtonClick;
	}
	
	private void ButtonClick(UIComponent component, UIMouseEventParameter eventParam)
	{
		if (Singleton<InfoManager>.instance.CurrentMode != InfoManager.InfoMode.TerrainHeight) {
			Singleton<InfoManager>.instance.SetCurrentMode (InfoManager.InfoMode.TerrainHeight, InfoManager.SubInfoMode.Default);
            button.state = UIButton.ButtonState.Focused;
		} else {
			Singleton<InfoManager>.instance.SetCurrentMode (InfoManager.InfoMode.None, InfoManager.SubInfoMode.Default);
            button.state = UIButton.ButtonState.Normal;
            button.Unfocus();
        }
	}


}

public class contourExtension : LoadingExtensionBase
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
        button.normalFgSprite = "SubBarMonumentLandmarks";
        button.hoveredFgSprite = "SubBarMonumentLandmarksHovered";
        button.focusedFgSprite = "SubBarMonumentLandmarksFocused";
        button.pressedFgSprite = "SubBarMonumentLandmarksPressed";

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
		if (!active) {
			originalMaps = new Texture2D[Singleton<TerrainManager>.instance.m_patches.Length];
			int i = 0;
			foreach(TerrainPatch terrainPatch in Singleton<TerrainManager>.instance.m_patches)
			{
				originalMaps[i] = terrainPatch.m_surfaceMapB;
				
				terrainPatch.m_surfaceMapB = toColoredHeightMap(terrainPatch.m_heightMap);
				i++;
			}
			active = true;
            button.state = UIButton.ButtonState.Focused;
		}else{
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
	
	Texture2D toColoredHeightMap (Texture2D m_heightMap)
	{
		Texture2D coloredHeightMap = new Texture2D (m_heightMap.width, m_heightMap.height);
		
		for(int x  = 0; x < m_heightMap.width; x ++){
			for(int y  = 0; y < m_heightMap.height; y ++){
				coloredHeightMap.SetPixel(x,y,new Color(m_heightMap.GetPixel(x,y).g, m_heightMap.GetPixel(x,y).g, m_heightMap.GetPixel(x,y).g, m_heightMap.GetPixel(x,y).a));
			}
		}
		coloredHeightMap.Apply ();
		return coloredHeightMap;
	}
	
	public void debug(String message) 
	{
		DebugOutputPanel.AddMessage (ColossalFramework.Plugins.PluginManager.MessageType.Message, message);
	}
}
