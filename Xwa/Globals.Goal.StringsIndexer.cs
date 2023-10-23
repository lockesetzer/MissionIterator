﻿/*
 * Idmr.Platform.dll, X-wing series mission library file, TIE95-XWA
 * Copyright (C) 2009-2018 Michael Gaisser (mjgaisser@gmail.com)
 * Licensed under the MPL v2.0 or later
 * 
 * Full notice in ../help/Idmr.Platform.chm
 * Version: 2.7
 */

/* CHANGELOG
 * v2.1, 141214
 * [UPD] change to MPL
 */

namespace Idmr.Platform.Xwa
{
	public partial class Globals
	{
		public partial class Goal
		{
			/// <summary>Object to provide array access to the Trigger Strings</summary>
			public class StringsIndexer
			{
				readonly Goal _owner;
				
				/// <summary>Initialize the indexer</summary>
				/// <param name="parent">Parent Global Goal</param>
				public StringsIndexer(Goal parent) { _owner = parent; }

				/// <summary>Length of the array</summary>
				public int Length => _owner._strings.Length;

				/// <summary>Gets or sets the Trigger Status strings</summary>
				/// <remarks><i>value</i> is limited to 63 characters</remarks>
				/// <param name="trigger">The Trigger index, 0-3</param>
				/// <param name="state"><see cref="GoalState"/> index</param>
				/// <exception cref="System.IndexOutOfRangeException">Invalid <i>trigger</i> or <i>state</i> value</exception>
				public string this[int trigger, int state]
				{
					get => _owner._strings[trigger, state];
					set => _owner._strings[trigger, state] = Common.StringFunctions.GetTrimmed(value, 63);
				}

				/// <summary>Gets or sets the Trigger Status strings</summary>
				/// <remarks><i>value</i> is limited to 63 characters</remarks>
				/// <param name="trigger">The Trigger index, 0-3</param>
				/// <param name="state"><see cref="GoalState"/> value</param>
				/// <exception cref="System.IndexOutOfRangeException">Invalid <i>trigger</i> value</exception>
				public string this[int trigger, GoalState state]
				{
					get => _owner._strings[trigger, (int)state];
					set => _owner._strings[trigger, (int)state] = Common.StringFunctions.GetTrimmed(value, 63);
				}
			}
		}
	}
}
