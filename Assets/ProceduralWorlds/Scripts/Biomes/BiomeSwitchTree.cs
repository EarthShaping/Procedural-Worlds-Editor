﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Linq;
using System;
using PW.Core;
using PW;
using Debug = UnityEngine.Debug;

namespace PW.Biomator
{

	using Node;
	
	public class BiomeSwitchTree
	{

		public bool							isBuilt { private set; get; }

		private BiomeSwitchNode				root = null;
		private short						biomeIdCount = 0;

		private Dictionary< int, PartialBiome >	biomePerId = new Dictionary< int, PartialBiome >();
		private Dictionary< string, PartialBiome >	biomePerName = new Dictionary< string, PartialBiome >();
		private Dictionary< BiomeSwitchMode, float > biomeCoverage = new Dictionary< BiomeSwitchMode, float >();

		private enum SwitchMode
		{
			Unknown,
			Float,
			Bool,
		}

		private class BiomeSwitchNode
		{
			public float				min;
			public float				max;
			public bool					value;
			public SwitchMode			mode;
			public BiomeSwitchMode	biomeSwitchMode;
			public PartialBiome			partialBiome;
			public Color				previewColor;
			public string				biomeName;

			List< BiomeSwitchNode >		childs = new List< BiomeSwitchNode >();
			
			public BiomeSwitchNode()
			{
				mode = SwitchMode.Unknown;
			}

			public void SetSwitchValue(float min, float max, BiomeSwitchMode biomeSwitchMode, string biomeName, Color previewColor)
			{
				this.min = min;
				this.max = max;
				this.mode = SwitchMode.Float;
				this.biomeSwitchMode = biomeSwitchMode;
				this.biomeName = biomeName;
				this.previewColor = previewColor;
				this.partialBiome = null;
			}
			
			public void SetSwitchValue(bool value, BiomeSwitchMode biomeSwitchMode, string biomeName, Color previewColor)
			{
				this.value = value;
				this.mode = SwitchMode.Bool;
				this.biomeSwitchMode = biomeSwitchMode;
				this.biomeName = biomeName;
				this.previewColor = previewColor;
				this.partialBiome = null;
			}

			public void SetPartialBiome(PartialBiome b)
			{
				partialBiome = b;
			}

			public BiomeSwitchNode GetNext(float value)
			{
				//find the value in range in childs and return it
				return null;
			}

			public BiomeSwitchNode GetNext(bool val)
			{
				if (val == value)
					return childs[0];
				return childs[1];
			}

			public void SetChildCount(int count)
			{
				while (childs.Count < count)
					childs.Add(new BiomeSwitchNode());
			}

			public BiomeSwitchNode GetChildAt(int i, bool createIfNotExists = false)
			{
				if (createIfNotExists && i >= childs.Count)
					SetChildCount(i + 1);
				return childs[i];
			}

			public int				GetChildCount()
			{
				return childs.Count;
			}

			public IEnumerable< BiomeSwitchNode > GetChilds()
			{
				return childs;
			}

			public override string ToString()
			{
				if (mode == SwitchMode.Bool)
					return "[" + biomeSwitchMode + "]: " + value + " (" + biomeName + ")";
				else if (mode == SwitchMode.Float)
					return "[" + biomeSwitchMode + "]: " + min + " -> " + max + " (" + biomeName + ")";
				return "non-initialized switch";
			}
		}

		public BiomeSwitchTree()
		{
			isBuilt = false;
		}

		void BuildTreeInternal(PWNode node, BiomeSwitchNode currentNode, int depth, BiomeSwitchNode parent = null)
		{
			if (node == null)
				return ;
			
			if (node.GetType() == typeof(PWNodeBiomeSwitch))
			{
				PWNodeBiomeSwitch	bSwitch = node as PWNodeBiomeSwitch;
				int					outputLinksCount = bSwitch.GetOutputLinks().Count();
				int					childIndex = 0;
				List< PWNode >		outputNodeList = new List< PWNode >();

				currentNode.SetChildCount(outputLinksCount);
				switch (bSwitch.switchMode)
				{
					case BiomeSwitchMode.Water:
						PWAnchor	terrestrialAnchor = node.GetAnchor("outputBiomes", 0);
						PWAnchor	aquaticAnchor = node.GetAnchor("outputBiomes", 1);

						if (terrestrialAnchor != null)
						{
							var nodes = node.GetNodesAttachedToAnchor(terrestrialAnchor);
							outputNodeList.AddRange(nodes);
							// Debug.Log("terrestrialNodes: " + nodes[0].nodeId);
							for (int i = 0; i < nodes.Count(); i++)
								currentNode.GetChildAt(childIndex++).SetSwitchValue(false, bSwitch.switchMode, "terrestrial", Color.black);
							biomeCoverage[BiomeSwitchMode.Water] += 0.5f;
						}
						//get all nodes on the first anchor:
						if (aquaticAnchor != null)
						{
							var nodes = node.GetNodesAttachedToAnchor(aquaticAnchor);
							// Debug.Log("aquaticNodes: " + nodes[0].nodeId);
							outputNodeList.AddRange(nodes);
							for (int i = 0; i < nodes.Count(); i++)
								currentNode.GetChildAt(childIndex++).SetSwitchValue(true, bSwitch.switchMode, "aquatic", Color.blue);
							biomeCoverage[BiomeSwitchMode.Water] += 0.5f;
						}

						break ;
					default:
						// Debug.Log("swicth data count for node " + node.nodeId + ": " + bSwitch.switchDatas.Count);

						for (int anchorIndex = 0; anchorIndex < bSwitch.switchList.Count; anchorIndex++)
						{
							PWAnchor anchor = node.GetAnchor("outputBiomes", anchorIndex);
							var sData = bSwitch.switchList[anchorIndex];

							if (anchor == null)
							{
								Debug.Log("NULL: " + anchorIndex);
								continue ;
							}

							var attachedNodesToAnchor = node.GetNodesAttachedToAnchor(anchor);
							outputNodeList.AddRange(attachedNodesToAnchor);

							// if (attachedNodesToAnchor.Count == 0)
								// Debug.LogWarning("nothing attached to the biome switch output " + anchorIndex);

							foreach (var attachedNode in attachedNodesToAnchor)
							{
								var child = currentNode.GetChildAt(childIndex++);

								child.SetSwitchValue(sData.min, sData.max, bSwitch.switchMode, sData.name, sData.color);
							}
							
							biomeCoverage[bSwitch.switchMode] += (sData.max - sData.min) / (sData.absoluteMax - sData.absoluteMin);
						}
						break ;
				}
				childIndex = 0;
				foreach (var outNode in outputNodeList)
				{
					BuildTreeInternal(outNode, currentNode.GetChildAt(childIndex, true), depth + 1, currentNode);
					// Type outNodeType = outNode.GetType();
					// if (outNodeType == typeof(PWNodeBiomeSwitch) || outNodeType == typeof(PWNodeBiome))
						childIndex++;
				}
			}
			else if (node.GetType() == typeof(PWNodeBiome))
			{
				PWNodeBiome biomeNode = node as PWNodeBiome;

				string biomeName = currentNode.biomeName;

				if (String.IsNullOrEmpty(biomeName))
				{
					Debug.LogWarning("Biome name null or empty for biomeNode: " + node);
					return ;
				}

				//Biome biomeNode detected, assign the biome to the current Node:
				currentNode.partialBiome = biomeNode.outputBiome;

				// Debug.Log("current node: " + currentNode + ", preview color: " + currentNode.previewColor);

				//set the partialBiome ID and name:
				currentNode.partialBiome.name = biomeName;
				currentNode.partialBiome.id = biomeIdCount++;

				//set the color and the id of the partialBiome in the biomeNode
				biomeNode.outputBiome.previewColor = currentNode.previewColor;
				biomeNode.outputBiome.id = currentNode.partialBiome.id;
				biomeNode.outputBiome.name = currentNode.partialBiome.name;

				//store the partialBiome in dictionaries for fast access
				biomePerId[currentNode.partialBiome.id] = currentNode.partialBiome;
				biomePerName[biomeName] = currentNode.partialBiome;
			}
			else
			{
				foreach (var outNode in node.GetOutputNodes())
					BuildTreeInternal(outNode, currentNode, depth++, parent);
			}
			return ;
		}

		public void BuildTree(PWNode node)
		{
			biomeIdCount = 0;
			Stopwatch st = new Stopwatch();
			st.Start();
			root = new BiomeSwitchNode();
			biomeCoverage.Clear();
			foreach (var switchMode in Enum.GetValues(typeof(BiomeSwitchMode)))
				biomeCoverage[(BiomeSwitchMode)switchMode] = 0;
			BuildTreeInternal(node, root, 0);
			st.Stop();

			Debug.Log("built tree time: " + st.ElapsedMilliseconds + "ms");
			
			// DumpBuiltTree();

			isBuilt = true;
		}

		void DumpBiomeTree(BiomeSwitchNode node, int depth = 0)
		{
			Debug.Log("node at depth " + depth + ": " + node);

			foreach (var child in node.GetChilds())
				DumpBiomeTree(child, depth + 1);
		}

		void DumpBuiltTree()
		{
			BiomeSwitchNode current = root;
			
			Debug.Log("built tree:");
			DumpBiomeTree(root);
			string	childs = "";
			foreach (var child in current.GetChilds())
				childs += child + "(" + child.partialBiome + ") | ";
			Debug.Log("swicth line1: " + childs);

			childs = "";
			foreach (var child in current.GetChilds())
				foreach (var childOfChild in child.GetChilds())
					childs += childOfChild + "(" + childOfChild.partialBiome + ") | ";
			Debug.Log("swicth line2: " + childs);
		}

		public void FillBiomeMap(int biomeBlendCount, BiomeData biomeData)
		{
			bool			is3DBiomes = false;
			bool			is3DTerrain = biomeData.terrain3D != null;
			// Biome[]			nearestBiomes = new Biome[biomeBlendCount];

			//TODO: partialBiome blend count > 1 management
			
			//TODO: biomeData.datas3D null check
			if (biomeData.air3D != null || biomeData.wind3D != null || biomeData.wetness3D != null || biomeData.temperature3D != null)
				is3DBiomes = true;
			
			if (biomeData.terrainRef == null)
				return ;
			
			int		terrainSize = (is3DTerrain) ? biomeData.terrain3D.size : biomeData.terrain.size;
			float	terrainStep = (is3DTerrain) ? biomeData.terrain3D.step : biomeData.terrain.step;
			if (is3DBiomes)
				biomeData.biomeIds3D = new BiomeMap3D(terrainSize, terrainStep);
			else
				biomeData.biomeIds = new BiomeMap2D(terrainSize, terrainStep);

			if (is3DBiomes)
			{
				//TODO
			}
			else
			{
				//clear the id list
				biomeData.ids.Clear();

				for (int x = 0; x < terrainSize; x++)
				for (int y = 0; y < terrainSize; y++)
				{
					bool water = (biomeData.isWaterless) ? false : biomeData.waterHeight[x, y] > 0;
					float temp = (biomeData.temperature != null) ? biomeData.temperature[x, y] : 0;
					float wet = (biomeData.wetness != null) ? biomeData.wetness[x, y] : 0;
					// Debug.Log("map [" + x + "/" + y + "] = " + biomeData.terrain[x, y]);
					//TODO: 3D terrain management
					float height = (is3DTerrain) ? 0 : biomeData.terrain[x, y];
					BiomeSwitchNode current = root;
					while (true)
					{
						nextChild:
						if (current.partialBiome != null)
							break ;
						int childCount = current.GetChildCount();
						for (int i = 0; i < childCount; i++)
						{
							var child = current.GetChildAt(i);
							switch (child.biomeSwitchMode)
							{
								case BiomeSwitchMode.Water:
									if (child.value == water)
										{ current = child; goto nextChild; }
									break ;
								case BiomeSwitchMode.Height:
									if (height > child.min && height <= child.max)
										{ current = child; goto nextChild; }
									break ;
								case BiomeSwitchMode.Temperature:
									if (temp > child.min && temp <= child.max)
										{ current = child; goto nextChild; }
									break ;
								case BiomeSwitchMode.Wetness:
									if (wet > child.min && wet <= child.max)
										{ current = child; goto nextChild; }
									break ;
							}
						}
						//if flow reach this part, values are missing in the partialBiome graph so partialBiome can't be chosen.
						break ;
					}

					if (current.partialBiome == null)
						continue ;

					//add our id to the list of ids if it's not already added
					if (!biomeData.ids.Contains(current.partialBiome.id))
						biomeData.ids.Add(current.partialBiome.id);

					//TODO: blending with second partialBiome
					if (current.partialBiome != null)
						biomeData.biomeIds.SetFirstBiomeId(x, y, current.partialBiome.id);
					else
					{
						//FIXME!!!!
						biomeData.biomeIds.SetFirstBiomeId(x, y, -1);
						// PWUtils.LogWarningMax("Can't choose partialBiome with water:" + water + ", temp: " + temp + ", wet: " + wet + ", height: " + height, 200);
						PWUtils.LogWarningMax("Can't choose biome with water:" + water + ", temp: " + temp + ", wet: " + wet + ", height: " + height, 300);
						continue ;
					}
				}
			}
		}

		public int GetBiomeCount()
		{
			return biomePerId.Count;
		}

		public PartialBiome GetBiome(string name)
		{
			if (biomePerName.ContainsKey(name))
				return biomePerName[name];
			return null;
		}

		public PartialBiome GetBiome(int id)
		{
			if (biomePerId.ContainsKey(id))
				return biomePerId[id];
			return null;
		}

		public Dictionary< int, PartialBiome > GetBiomes()
		{
			return biomePerId;
		}

		public Dictionary< BiomeSwitchMode, float > GetBiomeCoverage()
		{
			return biomeCoverage;
		}
	}
}