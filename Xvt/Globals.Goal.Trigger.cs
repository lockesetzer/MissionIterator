﻿/*
 * Idmr.Platform.dll, X-wing series mission library file, XW95-XWA
 * Copyright (C) 2009-2020 Michael Gaisser (mjgaisser@gmail.com)
 * Licensed under the MPL v2.0 or later
 * 
 * Full notice in ../help/Idmr.Platform.chm
 * Version: 4.0
 */

/* CHANGELOG
 * v4.0, 200809
 * [UPD] auto-properties
 * v2.3, 150405
 * - Release
 */

using System;
using Idmr.Common;

namespace Idmr.Platform.Xvt
{
	public partial class Globals
	{
		public partial class Goal
		{
			/// <summary>Object for individual Triggers</summary>
			[Serializable]
			public class Trigger
			{
				readonly Goal _owner;	//TODO: why do I have this?

				/// <summary>Initializes a new Trigger</summary>
				/// <param name="parent">The <see cref="Goal"/> to which this object belongs.</param>
				/// <remarks>Trigger set to <b>10</b>, "never (FALSE)".</remarks>
				public Trigger(Goal parent)
				{
					_owner = parent;
					GoalTrigger = new Mission.Trigger
					{
						Condition = 10
					};
					for (int i = 0; i < 3; i++) GoalStrings[i] = "";
				}

				/// <summary>Gets or sets the Trigger component values</summary>
				/// <param name="index">Trigger component, 0-3</param>
				/// <exception cref="IndexOutOfRangeException">Invalid <i>index</i> value</exception>
				public byte this[int index]
				{
					get => GoalTrigger[index];
					set => GoalTrigger[index] = value;
				}

				/// <summary>Gets or sets the Trigger component values</summary>
				/// <param name="index">Trigger component</param>
				public byte this[BaseTrigger.TriggerIndex index]
				{
					get => GoalTrigger[(int)index];
					set => GoalTrigger[(int)index] = value;
				}

				/// <summary>Gets or sets the Trigger Status strings</summary>
				/// <remarks>Limited to 63 characters</remarks>
				/// <param name="state">GoalState value, 0-2</param>
				/// <exception cref="IndexOutOfRangeException">Invalid <i>state</i> value</exception>
				public string this[GoalState state]
				{
					get => GoalStrings[(int)state];
					set => GoalStrings[(int)state] = StringFunctions.GetTrimmed(value, 63);
				}

				/// <summary>Gets or sets the Trigger</summary>
				public Mission.Trigger GoalTrigger { get; set; }

				/// <summary>Gets the array for the GoalString values</summary>
				public string[] GoalStrings { get; } = new string[3];
			}
		}
	}
}
