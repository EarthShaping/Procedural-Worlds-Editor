﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PW.Biomator;

namespace PW.Biomator.SwitchGraph
{
	public class BiomeSwitchCell
	{
		public List< BiomeSwitchCell >	links = new List< BiomeSwitchCell >();
		public float					weight;
		public string					name;
		public Color					color;
		public short					id;

		public BiomeSwitchCellParams	switchParams = new BiomeSwitchCellParams();

		public bool		Overlaps(BiomeSwitchCellParams cellParams)
		{
			for (int i = 0; i < (int)BiomeSwitchMode.Count; i++)
				if (cellParams[i].enabled && switchParams[i].enabled
					&& !PWUtils.Overlap(switchParams[i].min, switchParams[i].max, cellParams[i].min, cellParams[i].max))
						return false;
			return true;
		}

		public float	GetWeight(Dictionary< BiomeSwitchMode, Vector2 > paramRanges)
		{
			float	weight = 0;

			for (int i = 0; i < switchParams.Length; i++)
			{
				BiomeSwitchMode			mode = (BiomeSwitchMode)i;
				BiomeSwitchCellParam	param = switchParams[i];

				if (param.enabled && paramRanges[mode].magnitude != 0)
					weight += param.max - param.min / paramRanges[mode].magnitude;
				else
					weight += 1;
			}

			return weight;
		}

		public override string ToString()
		{
			string s = name + " = ";

			for (int i = 0; i < switchParams.Length; i++)
				if (switchParams[i].enabled)
					s += ((BiomeSwitchMode)i + ": " + switchParams[i].min + "->" + switchParams[i].max);

			return s;
		}

		public bool Matches(BiomeSwitchValues bsv)
		{
			for (int i = 0; i < (int)BiomeSwitchMode.Count; i++)
			{
				var  p = this.switchParams[i];

				if (p.enabled && (bsv[i] < p.min || bsv[i] > p.max ))
						return false;
			}

			return true;
		}

		public float	GapWidth(BiomeSwitchCell c2)
		{
			float gap = 0;

			for (int i = 0; i < (int)BiomeSwitchMode.Count; i++)
				if (switchParams[i].enabled)
				{
					var s1 = switchParams[i];
					var s2 = c2.switchParams[i];
					gap += PWUtils.GapWidth(s1.min, s1.max, s2.min, s2.max);
				}
			
			return gap;
		}
	}
}