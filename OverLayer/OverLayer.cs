using ICities;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;
using ColossalFramework.Math;
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

public class OverLayerExtension : LoadingExtensionBase, OverLayerTool.Delegate
{
	private const String c_filename = "Files/overlay.png";

	private UIButton m_button;

	public override void OnLevelLoaded(LoadMode p_mode)
	{
		if (p_mode == LoadMode.LoadAsset || p_mode == LoadMode.NewAsset) return;

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

		try
		{
			OverLayerTool.OnLevelLoaded();
			OverLayerTool.SetDelegate(this);
		}
		catch (Exception e)
		{
			DebugLog("Error loading tool: " + e.ToString());
		}

		DebugLog("Loaded");
	}

	public override void OnLevelUnloading()
	{
		OverLayerTool l_overLayerTool = OverLayerTool.instance;

		if (l_overLayerTool != null && l_overLayerTool.GetIsActive())
		{
			l_overLayerTool.ToggleActive();
		}
	}

	private void ButtonClick(UIComponent p_component, UIMouseEventParameter p_eventParam)
	{
		OverLayerTool l_overLayerTool = OverLayerTool.instance;

		if (l_overLayerTool != null)
		{
			l_overLayerTool.ToggleActive();
		}
	}

	public static void DebugLog(String p_message)
	{
		DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, "[OverLayer] " + p_message);
	}

	public void OverLayerToolDidUpdateState(bool p_newState)
	{
		m_button.state = p_newState ? UIButton.ButtonState.Focused : UIButton.ButtonState.Normal;

		if (!p_newState && m_button != null)
		{
			m_button.Unfocus();
		}
	}
}

internal class OverLayerTool : SimulationManagerBase<OverLayerTool, MonoBehaviour>, ISimulationManager, IRenderableManager
{
	private const String c_filename = "Files/overlay.png";
	private static bool m_hasRegistered = false;

	private static WeakReference m_delegate = null;
	private bool m_active = false;
	private bool m_shouldActivate = false;

	private DateTime m_lastBytesWrite;
	private Texture2D m_lastTexture;

	float m_defaultDimension = 8640f;
	float m_dimensionDelta = 0f;

	public static void OnLevelLoaded()
	{
		OverLayerExtension.DebugLog("Is registered: " + m_hasRegistered);

		if (!m_hasRegistered)
		{
			SimulationManager.RegisterManager(instance);
			m_hasRegistered = true;

			OverLayerExtension.DebugLog("Registered Manager");
		}
	}

	protected override void Awake()
	{
		base.Awake();
		enabled = true;
	}

	public static void SetDelegate(Delegate p_delegate)
	{
		if (p_delegate != null)
		{
			m_delegate = new WeakReference(p_delegate);
		}
	}

	public void Update()
	{
		if (m_active != m_shouldActivate)
		{
			ToggleActiveInternal();
		}
	}

	public void ToggleActive()
	{
		m_shouldActivate = !m_active;
	}

	private void ToggleActiveInternal()
	{
		if (!m_active)
		{
			int l_tileSizeX = Singleton<TerrainManager>.instance.m_patches[0].m_surfaceMapA.width;
			int l_tileSizeY = Singleton<TerrainManager>.instance.m_patches[0].m_surfaceMapA.height;
			Texture2D l_overlay = GetOverlayTexture(l_tileSizeX * 9, l_tileSizeY * 9);

			if (l_overlay == null)
			{
				OverLayerExtension.DebugLog("Could not load image. Are you sure it is placed at the correct location?");
				return;
			}

			m_lastTexture = l_overlay;
			m_lastTexture.Apply();
			m_active = true;
			NotifyDelegate();
		}
		else
		{
			m_active = false;
			NotifyDelegate();
		}
	}

	protected override void SimulationStepImpl(int subStep)
	{
		if (!m_active)
		{
			return;
		}

		base.SimulationStepImpl(subStep);

		float l_delta = 10f;

		if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
		{
			l_delta *= 2f;
		}
		else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
		{
			l_delta /= 2f;
		}

		if (Input.GetKey(KeyCode.PageUp))
		{
			m_dimensionDelta += l_delta;
		}
		else if (Input.GetKey(KeyCode.PageDown))
		{
			m_dimensionDelta -= l_delta;
		}
		else if (Input.GetKey(KeyCode.Home))
		{
			m_dimensionDelta = l_delta;
		}

		m_dimensionDelta = Math.Min(Math.Max(m_dimensionDelta, -m_defaultDimension), m_defaultDimension);
	}

	protected override void EndOverlayImpl(RenderManager.CameraInfo cameraInfo)
	{
		base.EndOverlayImpl(cameraInfo);

		if (!m_active) return;

		float l_dimension = m_defaultDimension + m_dimensionDelta;
		RenderManager renderManager = RenderManager.instance;

		Quad3 position = new Quad3(
			new Vector3(-l_dimension, 0, -l_dimension),
			new Vector3(l_dimension, 0, -l_dimension),
			new Vector3(l_dimension, 0, l_dimension),
			new Vector3(-l_dimension, 0, l_dimension)
		);

		renderManager.OverlayEffect.DrawQuad(cameraInfo, m_lastTexture, Color.white, position, -1, 1000, true, true);
	}

	private void NotifyDelegate()
	{
		if (m_delegate != null && m_delegate.Target != null)
		{
			Delegate l_delegate = (Delegate) m_delegate.Target;
			l_delegate.OverLayerToolDidUpdateState(m_active);
		}
	}

	private Texture2D GetOverlayTexture(int p_width, int p_height)
	{
		try
		{
			DateTime l_newBytesWrite = File.GetLastWriteTime(c_filename);

			if (m_lastBytesWrite != null && m_lastTexture != null)
			{
				if (m_lastBytesWrite.Equals(l_newBytesWrite))
				{
					return m_lastTexture;
				}
			}

			byte[] l_bytes = File.ReadAllBytes(c_filename);
			m_lastBytesWrite = l_newBytesWrite;
			
			Texture2D l_texture = new Texture2D(p_width, p_height);
			m_lastTexture = new Texture2D(p_width, p_height);

			l_texture.LoadImage(l_bytes);
			m_lastTexture.LoadImage(l_bytes);

			for (int y = 0; y < l_texture.height; ++y)
			{
				for (int x = 0; x < l_texture.width; ++x)
				{
					Color l_color = l_texture.GetPixel(y, x);
					l_color.a *= 0.75f;
					m_lastTexture.SetPixel(x, y, l_color);
				}
			}

			return m_lastTexture;
		}
		catch (Exception p_exception)
		{
			OverLayerExtension.DebugLog("Got exception: " + p_exception.Message);
			return null;
		}
	}

	public bool GetIsActive()
	{
		return m_active;
	}

	public interface Delegate
	{
		void OverLayerToolDidUpdateState(bool p_newState);
	}
}