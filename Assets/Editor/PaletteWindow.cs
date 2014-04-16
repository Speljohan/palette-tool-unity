using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Reflection;
using System.Linq;
using System.Collections;

public class PaletteWindow : EditorWindow {

	private Texture2D sheet;
	private Vector2 scrollPos;
	private GUIContent[] content;
	private string[] tilesetList;
	private string[] layerList;
	private int[] layerUIDs;
	private GUIStyle selectionGridStyle;


	private int selectedTile = 0;
	private int selectedTileset = 0;
	private int selectedLayer = 0;

	private bool makeSolid = false;

	private Sprite[] sprites;

	[MenuItem("Speljohan/Tile Editor")]
	static void ShowWindow () {
		EditorWindow editorWindow = EditorWindow.GetWindow (typeof(PaletteWindow), false, "Tile Editor");
		editorWindow.autoRepaintOnSceneChange = true;
		editorWindow.Show();
	}

	void OnEnable() {
		Texture2D blue = GenerateBackground (Color.blue);
		Texture2D white = GenerateBackground (Color.white);
		selectionGridStyle = new GUIStyle();
		selectionGridStyle.fixedHeight = 34;
		selectionGridStyle.fixedWidth = 34;
		selectionGridStyle.contentOffset = new Vector2(1, 1);
		selectionGridStyle.normal.background = white;
		selectionGridStyle.onNormal.background = blue;
		selectionGridStyle.hover.background = blue;
		selectionGridStyle.active.background = blue;
		tilesetList = LoadTilesetList ();
		layerList = LoadLayerList();
		layerUIDs = GetSortingLayerUniqueIDs();
		SceneView.onSceneGUIDelegate += OnSceneGUI;

	}

	void OnDisable() {
		SceneView.onSceneGUIDelegate -= OnSceneGUI;
	}

	void OnGUI() {

		GUILayout.BeginHorizontal ();
		EditorGUILayout.LabelField ("Selected Tileset: ");
		selectedTileset = EditorGUILayout.Popup (selectedTileset, tilesetList); 
		GUILayout.EndHorizontal ();
		EditorGUILayout.LabelField ("Properties", EditorStyles.boldLabel);
		GUILayout.BeginHorizontal ();
		EditorGUILayout.LabelField ("Solid: ");
		makeSolid = EditorGUILayout.Toggle (makeSolid);
		GUILayout.EndHorizontal ();
		GUILayout.BeginHorizontal ();
		EditorGUILayout.LabelField ("Layer: ");
		selectedLayer = EditorGUILayout.Popup (selectedLayer, layerList); 
		GUILayout.EndHorizontal();

		if (GUI.changed) {
			sprites = Resources.LoadAll<Sprite>( "Tilesets/" + tilesetList[selectedTileset]).ToArray();
			content = GenerateContent (sprites);
		}

		if (content != null) {
			scrollPos = EditorGUILayout.BeginScrollView (scrollPos, false, true);
			
			selectedTile = GUI.SelectionGrid(new Rect(30, 30, 800, 1000), selectedTile, content, 10, selectionGridStyle);
			EditorGUILayout.EndScrollView ();
		}
	}

	string[] LoadTilesetList() {
		Texture2D[] tex = Resources.LoadAll<Texture2D>("Tilesets");
		return tex.Select(t => t.name).ToArray ();
	}

	string[] LoadLayerList() {
		Type internalEditorUtilityType = typeof(InternalEditorUtility);
		PropertyInfo sortingLayersProperty = internalEditorUtilityType.GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);
		return (string[])sortingLayersProperty.GetValue(null, new object[0]);	
	}

	public int[] GetSortingLayerUniqueIDs() {
		Type internalEditorUtilityType = typeof(InternalEditorUtility);
		PropertyInfo sortingLayerUniqueIDsProperty = internalEditorUtilityType.GetProperty("sortingLayerUniqueIDs", BindingFlags.Static | BindingFlags.NonPublic);
		return (int[])sortingLayerUniqueIDsProperty.GetValue(null, new object[0]);
	}

	GUIContent[] GenerateContent(Sprite[] sprites) {
		GUIContent[] o = new GUIContent[sprites.Length];
		for (int i = 0; i < sprites.Length; i++) {
			o[i] = new GUIContent(GetCroppedTexture (sprites[i]));
		}
		return o;
	}

	void Update() {
		Repaint ();
	}

	void OnSceneGUI(SceneView view) 
	{
		view.wantsMouseMove = true;
		Event e = Event.current;

		int controlID = GUIUtility.GetControlID(FocusType.Passive);
		HandleUtility.AddDefaultControl(controlID);

		switch (e.type) {
			case EventType.MouseDown:
				placeTile (e.mousePosition);
			break;
			case EventType.MouseDrag:
				placeTile (e.mousePosition);
			break;
			default:
			Event.current.Use();
			break;
			}
	}

	void placeTile(Vector2 position) {
		GameObject obj = HandleUtility.PickGameObject(position, true);
		if (obj != null) {
			Debug.Log (obj.GetComponent<SpriteRenderer>().sortingLayerID + "," + layerUIDs[selectedLayer]);
			if (obj.GetComponent<SpriteRenderer>().sortingLayerID == layerUIDs[selectedLayer]) {
				Debug.Log ("FOO");
				DestroyImmediate (obj);
			}
		}
		createTile (HandleUtility.GUIPointToWorldRay(position).GetPoint (0.0f));
	}

	GameObject createTile(Vector3 position) {
		Vector3 target = Snap (position, 1);
		GameObject obj = new GameObject();
		obj.GetComponent<Transform>().position = target;
		SpriteRenderer r = obj.AddComponent<SpriteRenderer>();
		r.sprite = sprites[selectedTile];
		r.sortingLayerName = layerList[selectedLayer];
		if (makeSolid) {
			obj.AddComponent<BoxCollider>();
		}
		return obj;
	}

	Vector3 Snap(Vector3 input, int g)
	{
		return(new Vector3(g * Mathf.Round((input.x / g)), g * Mathf.Round((input.y / g)), 0));
	}

	Texture2D GetCroppedTexture(Sprite sprite) {
		Rect bounds = sprite.rect;
		Texture2D texture = new Texture2D((int) bounds.width, (int)bounds.height);
		Color[] pixels = sprite.texture.GetPixels ((int)bounds.x, (int)bounds.y, (int)bounds.width, (int)bounds.height);
		texture.SetPixels (pixels);
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.alphaIsTransparency = true;
		texture.anisoLevel = 0;
		texture.filterMode = FilterMode.Point;
		texture.Apply (false, false);
		return ScaleTexture(texture, 32, 32);
	}

	private Texture2D ScaleTexture(Texture2D source,int targetWidth,int targetHeight) {
		Texture2D result=new Texture2D(targetWidth,targetHeight,source.format,true);
		Color[] rpixels=result.GetPixels(0);
		float incX=(1.0f / (float)targetWidth);
		float incY=(1.0f / (float)targetHeight); 
		for(int px=0; px<rpixels.Length; px++) { 
			rpixels[px] = source.GetPixelBilinear(incX*((float)px%targetWidth), incY*((float)Mathf.Floor(px/targetWidth))); 
		} 
		result.SetPixels(rpixels,0); 
		result.Apply(true, true); 
		return result; 
	}

	private Texture2D GenerateBackground(Color color) {
		Texture2D texture = new Texture2D(34, 34);
		Color[] pixels = new Color[34 * 34];
		for (int x = 0; x < 34; x++) {
			for (int y = 0; y < 34; y++) {
				pixels[x * 34 + y] = color;
			}
		}
		texture.SetPixels (pixels);

		texture.Apply (true, true);
		return texture;
	}
}
