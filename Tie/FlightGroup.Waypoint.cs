﻿/*
 * Idmr.Platform.dll, X-wing series mission library file, TIE95-XWA
 * Copyright (C) 2009-2014 Michael Gaisser (mjgaisser@gmail.com)
 * Licensed under the MPL v2.0 or later
 * 
 * Full notice in ../help/Idmr.Platform.chm
 * Version: 2.1
 */

/* CHANGELOG
 * v2.1, 141214
 * [UPD] change to MPL
 * v2.0, 120525
 * [NEW] conversions
 */

using System;
using Idmr.Common;

namespace Idmr.Platform.Tie
{
	public partial class FlightGroup : BaseFlightGroup
	{
		/// <summary>Object for a single Waypoint</summary>
		[Serializable] public class Waypoint : BaseWaypoint
		{
			/// <summary>Default constructor</summary>
			public Waypoint() : base(new short[4]) { }
			
			/// <summary>Initialize a new Waypoint using raw data</summary>
			/// <remarks><i>raw</i> must have Length of 4</remarks>
			/// <param name="raw">Raw data</param>
			/// <exception cref="ArgumentException">Incorrect <i>raw</i>.Length</exception>
			public Waypoint(short[] raw) : base(raw) { if (raw.Length != 4) throw new ArgumentException("raw does not have the correct size"); }
			
			/// <summary>Converts a Waypoint for XvT</summary>
			/// <param name="wp">The Waypoint to convert</param>
			/// <returns>A copy of <i>wp</i> for XvT</returns>
			public static implicit operator Xvt.FlightGroup.Waypoint(Waypoint wp) { return new Xvt.FlightGroup.Waypoint((short[])wp); }
			/// <summary>Converts a Waypoint for XWA</summary>
			/// <param name="wp">The Waypoint to convert</param>
			/// <returns>A copy of <i>wp</i> for XWA. The Region will be <b>zero</b></returns>
			public static implicit operator Xwa.FlightGroup.Waypoint(Waypoint wp) { return new Xwa.FlightGroup.Waypoint((short[])wp); }
		}
	}
}
