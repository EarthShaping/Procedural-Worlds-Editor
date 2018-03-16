﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ProceduralWorlds;

namespace ProceduralWorlds.Editor
{
	[CustomEditor(typeof(TerrainGenericBase))]
	public abstract class TerrainBaseInspector : UnityEditor.Editor
	{
		TerrainGenericBase terrain;

		public void OnEnable()
		{
			terrain = target as TerrainGenericBase;
			OnEditorEnable();
		}

		public override void OnInspectorGUI()
		{
			terrain.renderDistance = EditorGUILayout.IntSlider("Render distance", terrain.renderDistance, 0, 24);
			terrain.loadPatternMode = (ChunkLoadPatternMode)EditorGUILayout.EnumPopup("Load pattern mode", terrain.loadPatternMode);
			terrain.terrainStorage = EditorGUILayout.ObjectField(terrain.terrainStorage, typeof(TerrainStorage), false) as TerrainStorage;
			terrain.terrainScale = EditorGUILayout.Slider("Terrain scale", terrain.terrainScale, 0.01f, 10f);

			OnEditorGUI();
		}

		public abstract void OnEditorGUI();
		public abstract void OnEditorEnable();
	}
}