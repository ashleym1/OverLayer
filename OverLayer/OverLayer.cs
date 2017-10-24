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

	private UIMultiStateButton m_button;
	private UISprite m_lockImage;

	public override void OnLevelLoaded(LoadMode p_mode)
	{
		if (p_mode == LoadMode.LoadAsset || p_mode == LoadMode.NewAsset) return;

		// Get the UIView object. This seems to be the top-level object for most
		// of the UI.
		var l_uiView = UIView.GetAView();

		// Add a new button to the view.
		m_button = (UIMultiStateButton)l_uiView.AddUIComponent(typeof(UIMultiStateButton));

		// Attach a sprite to the button
		m_lockImage = (UISprite)m_button.AddUIComponent(typeof(UISprite));

		// Set the text to show on the button tooltip.
		m_button.tooltip = "OverLayer Switch - Right Click to lock layer movement (Ctrl+Shift+L).";
		m_button.isTooltipLocalized = false;
		m_button.RefreshTooltip();
		m_button.spritePadding = new RectOffset();

		// Set the button dimensions.
		m_button.width = 36;
		m_button.height = 36;

		// Set the lock image
		m_lockImage.spriteName = "LockIcon";
		m_lockImage.position = new Vector3(18, -18);
		m_lockImage.width = 24;
		m_lockImage.height = 24;
		m_lockImage.Hide();

		if (m_lockImage.atlas == null || m_lockImage.atlas.material == null)
		{
			DebugLog("Could not get reference material!!!");
			return;
		}

		// The sprite for the button can't be added to the InGame atlas, since the sprite data
		// seems to come from the atlas's texture, instead of the texture supplied by the SpriteData.
		// So a whole new atlas with the toggle button base images duplicated is needed.
		// Thanks to https://github.com/onewaycitystreets/StreetDirectionViewer/blob/master/ui/StreetDirectionViewerUI.cs

		String[] iconNames = {
			"RoadArrowIcon",
			"Base",
			"BaseFocused",
			"BaseHovered",
			"BasePressed",
			"BaseDisabled",
		};

		m_button.atlas = CreateTextureAtlas("OverLayer.OverlayIcon.png", "OverLayer Atlas", m_lockImage.atlas.material, 36, 36, iconNames);

		// Background sprites

		// Disabled state
		UIMultiStateButton.SpriteSet backgroundSpriteSet0 = m_button.backgroundSprites[0];
		backgroundSpriteSet0.normal = "Base";
		backgroundSpriteSet0.disabled = "Base";
		backgroundSpriteSet0.hovered = "BaseHovered";
		backgroundSpriteSet0.pressed = "Base";
		backgroundSpriteSet0.focused = "Base";

		// Enabled state
		m_button.backgroundSprites.AddState();
		UIMultiStateButton.SpriteSet backgroundSpriteSet1 = m_button.backgroundSprites[1];
		backgroundSpriteSet1.normal = "BaseFocused";
		backgroundSpriteSet1.disabled = "BaseFocused";
		backgroundSpriteSet1.hovered = "BaseFocused";
		backgroundSpriteSet1.pressed = "BaseFocused";
		backgroundSpriteSet1.focused = "BaseFocused";

		// Forground sprites

		// Disabled state
		UIMultiStateButton.SpriteSet foregroundSpriteSet0 = m_button.foregroundSprites[0];
		foregroundSpriteSet0.normal = "RoadArrowIcon";
		foregroundSpriteSet0.disabled = "RoadArrowIcon";
		foregroundSpriteSet0.hovered = "RoadArrowIcon";
		foregroundSpriteSet0.pressed = "RoadArrowIcon";
		foregroundSpriteSet0.focused = "RoadArrowIcon";

		// Enabled state
		m_button.foregroundSprites.AddState();
		UIMultiStateButton.SpriteSet foregroundSpriteSet1 = m_button.foregroundSprites[1];
		foregroundSpriteSet1.normal = "RoadArrowIcon";
		foregroundSpriteSet1.disabled = "RoadArrowIcon";
		foregroundSpriteSet1.hovered = "RoadArrowIcon";
		foregroundSpriteSet1.pressed = "RoadArrowIcon";
		foregroundSpriteSet1.focused = "RoadArrowIcon";

		// Enable button sounds.
		m_button.playAudioEvents = true;

		// Place the button.
		m_button.transformPosition = new Vector3(-1.11f, 0.98f);

		// Respond to button click.
		m_button.eventMouseUp += ButtonMouseUp;

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

	private void ButtonMouseUp(UIComponent p_component, UIMouseEventParameter p_eventParam)
	{
		OverLayerTool l_overLayerTool = OverLayerTool.instance;

		if (l_overLayerTool == null)
		{
			return;
		}

		if (p_eventParam.buttons == UIMouseButton.Left)
		{
			l_overLayerTool.ToggleActive();
		}
		else
		{
			l_overLayerTool.ToggleLocked();
		}
	}

	public static void DebugLog(String p_message)
	{
		DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, "[OverLayer] " + p_message);
	}

	public void OverLayerToolDidUpdateState(ModState p_newState)
	{
		if (m_button == null)
		{
			return;
		}

		switch (p_newState)
		{
			case ModState.off:
				m_button.activeStateIndex = 0;
				m_button.Unfocus();
				m_lockImage.Hide();
				break;

			case ModState.on:
				m_button.activeStateIndex = 1;
				m_lockImage.Hide();
				break;

			case ModState.locked:
				m_button.activeStateIndex = 1;
				m_lockImage.Show();
				break;
		}
	}

	private static UITextureAtlas CreateTextureAtlas(string textureFile, string atlasName, Material baseMaterial, int spriteWidth, int spriteHeight, string[] spriteNames)
	{
		Texture2D texture = new Texture2D(spriteWidth * spriteNames.Length, spriteHeight, TextureFormat.ARGB32, false);
		texture.filterMode = FilterMode.Bilinear;

		{ // LoadTexture
			System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
			Stream textureStream = assembly.GetManifestResourceStream(textureFile);

			if (textureStream == null)
			{
				DebugLog("Failed loading image!!");
				return null;
			}

			byte[] buf = new byte[textureStream.Length];  //declare arraysize
			textureStream.Read(buf, 0, buf.Length); // read from stream to byte array

			texture.LoadImage(buf);

			texture.Apply(true, true);
		}

		UITextureAtlas atlas = ScriptableObject.CreateInstance<UITextureAtlas>();

		{ // Setup atlas
			Material material = (Material)Material.Instantiate(baseMaterial);
			material.mainTexture = texture;

			atlas.material = material;
			atlas.name = atlasName;
		}

		// Add sprites
		for (int i = 0; i < spriteNames.Length; ++i)
		{
			float uw = 1.0f / spriteNames.Length;

			var spriteInfo = new UITextureAtlas.SpriteInfo()
			{
				name = spriteNames[i],
				texture = texture,
				region = new Rect(i * uw, 0, uw, 1),
			};

			atlas.AddSprite(spriteInfo);
		}

		return atlas;
	}

	public enum ModState
	{
		off,
		on,
		locked
	}
}

internal class OverLayerTool : SimulationManagerBase<OverLayerTool, MonoBehaviour>, ISimulationManager, IRenderableManager
{
	private const String c_filename = "Files/overlay.png";
	private static bool m_hasRegistered = false;

	private static WeakReference m_delegate = null;
	private bool m_active = false;
	private bool m_shouldActivate = false;
	private bool m_locked = false;
	private bool m_shouldLock = false;

	private DateTime m_lastBytesWrite;
	private Texture2D m_lastTexture;

	const float m_defaultDimension = 8640f;
	float m_dimensionDelta = 0f;

	Vector2 m_translation = new Vector2(0f, 0f);

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

		if (m_locked != m_shouldLock)
		{
			ToggleLockInternal();
		}
	}

	public void ToggleActive()
	{
		m_shouldActivate = !m_active;
	}

	public void ToggleLocked()
	{
		m_shouldLock = !m_locked;
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

	private void ToggleLockInternal()
	{
		m_locked = !m_locked;
		NotifyDelegate();
	}

	private bool isShiftHeld()
	{
		return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
	}

	private bool isControlHeld()
	{
		return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
	}

	protected override void SimulationStepImpl(int subStep)
	{
		base.SimulationStepImpl(subStep);

		if (!m_active)
		{
			return;
		}

		if (isControlHeld() && isShiftHeld() && Input.GetKeyUp(KeyCode.L))
		{
			m_locked = !m_locked;
			m_shouldLock = m_locked;
			NotifyDelegate();
		}

		if (m_locked)
		{
			return;
		}

		float l_delta = 5f;

		if (isShiftHeld())
		{
			l_delta *= 10f;
		}
		else if (isControlHeld())
		{
			l_delta /= 10f;
		}

		if (Input.GetKey(KeyCode.RightArrow))
		{
			m_translation.x += l_delta;
		}
		else if (Input.GetKey(KeyCode.LeftArrow))
		{
			m_translation.x -= l_delta;
		}
		
		if (Input.GetKey(KeyCode.UpArrow))
		{
			m_translation.y += l_delta;
		}
		else if (Input.GetKey(KeyCode.DownArrow))
		{
			m_translation.y -= l_delta;
		}

		if (Input.GetKey(KeyCode.Period))
		{
			m_dimensionDelta += l_delta;
		}
		else if (Input.GetKey(KeyCode.Comma))
		{
			m_dimensionDelta -= l_delta;
		}
		else if (Input.GetKey(KeyCode.Home))
		{
			m_dimensionDelta = l_delta;
			m_translation.x = 0f;
			m_translation.y = 0f;
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
			new Vector3(-l_dimension + m_translation.x, 0, -l_dimension + m_translation.y),
			new Vector3(l_dimension + m_translation.x, 0, -l_dimension + m_translation.y),
			new Vector3(l_dimension + m_translation.x, 0, l_dimension + m_translation.y),
			new Vector3(-l_dimension + m_translation.x, 0, l_dimension + m_translation.y)
		);

		renderManager.OverlayEffect.DrawQuad(cameraInfo, m_lastTexture, Color.white, position, -1f, 1800f, false, true);
	}

	private void NotifyDelegate()
	{
		if (m_delegate != null && m_delegate.Target != null)
		{
			Delegate l_delegate = (Delegate) m_delegate.Target;
			OverLayerExtension.ModState l_state;

			if (m_active)
			{
				if (m_locked)
				{
					l_state = OverLayerExtension.ModState.locked;
				}
				else
				{
					l_state = OverLayerExtension.ModState.on;
				}
			}
			else
			{
				l_state = OverLayerExtension.ModState.off;
			}

			l_delegate.OverLayerToolDidUpdateState(l_state);
		}
	}

	private Texture2D GetOverlayTexture(int p_width, int p_height)
	{
		try
		{
			DateTime l_newBytesWrite = File.GetLastWriteTime(c_filename);

			if (m_lastBytesWrite != null && m_lastTexture != null && !Input.GetKey(KeyCode.LeftShift))
			{
				if (m_lastBytesWrite.Equals(l_newBytesWrite))
				{
					OverLayerExtension.DebugLog("Using cached image. Activate while holding shift to force reload.");
					return m_lastTexture;
				}
			}

			OverLayerExtension.DebugLog("Loading image.");

			byte[] l_bytes = File.ReadAllBytes(c_filename);
			m_lastBytesWrite = l_newBytesWrite;
			
			Texture2D l_texture = new Texture2D(p_width, p_height);
			m_lastTexture = new Texture2D(p_width, p_height);

			l_texture.LoadImage(l_bytes);
			m_lastTexture.LoadImage(l_bytes);

			// We need to flip the image for some reason?
			for (int y = 0; y < l_texture.height; ++y)
			{
				for (int x = 0; x < l_texture.width; ++x)
				{
					m_lastTexture.SetPixel(x, y, l_texture.GetPixel(y, x));
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
		void OverLayerToolDidUpdateState(OverLayerExtension.ModState p_newState);
	}
}