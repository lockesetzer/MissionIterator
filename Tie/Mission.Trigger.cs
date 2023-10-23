/*
 * Idmr.Platform.dll, X-wing series mission library file, XW95-XWA
 * Copyright (C) 2009-2023 Michael Gaisser (mjgaisser@gmail.com)
 * Licensed under the MPL v2.0 or later
 * 
 * Full notice in ../help/Idmr.Platform.chm
 * Version: 5.7.5
 */

/* CHANGELOG
 * v5.7.5, 230116
 * [UPD #12] ToString update for AI Rating, Status and All Craft
 * v5.7, 220127
 * [NEW] cloning ctor [JB]
 * v4.0, 200809
 * [UPD] SafeString implemented [JB]
 * [FIX] ToString now prevents "of of" [JB]
 * v2.1, 141214
 * [UPD] change to MPL
 * [FIX] _checkValues reverts TargetType 0A to 00 (LA sloppiness, caused load fail on certain missions)
 * v2.0, 120525
 * [NEW] conversions, validation, exceptions, ToString() override
 */

using Idmr.Common;
using System;

namespace Idmr.Platform.Tie
{
	public partial class Mission : MissionFile
	{
		/// <summary>Object for a single Trigger</summary>
		[Serializable] public class Trigger : BaseTrigger
		{
			/// <summary>Initializes a blank Trigger</summary>
			public Trigger() : base(new byte[4]) { }

			/// <summary>Initializes a new Trigger from an existing Trigger. If null, a blank Trigger is created.</summary>
			/// <param name="other">Existing Trigger to clone. If <b>null</b>, Trigger will be blank.</param>
			public Trigger(Trigger other) : this()
			{
				if (other != null)
					Array.Copy(other._items, _items, _items.Length);
			}
			
			/// <summary>Initializes a new Trigger from raw data</summary>
			/// <param name="raw">Raw data, minimum Length of 4</param>
			/// <exception cref="ArgumentException">Invalid <paramref name="raw"/>.Length value<br/><b>-or-</b><br/>Invalid member values</exception>
			public Trigger(byte[] raw)
			{
				if (raw.Length < 4) throw new ArgumentException("Minimum length of raw is 4", "raw");
				_items = new byte[4];
				ArrayFunctions.TrimArray(raw, 0, _items);
				checkValues(this);
			}

			/// <summary>Initializes a new Trigger from raw data</summary>
			/// <param name="raw">Raw data</param>
			/// <param name="startIndex">Offset within <paramref name="raw"/> to begin reading</param>
			/// <exception cref="ArgumentException">Invalid <paramref name="raw"/>.Length value<br/><b>-or-</b><br/>Invalid member values</exception>
			/// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> results in reading outside the bounds of <paramref name="raw"/></exception>
			public Trigger(byte[] raw, int startIndex)
			{
				if (raw.Length < 4) throw new ArgumentException("Minimum length of raw is 4", "raw");
				if (raw.Length - startIndex < 4 || startIndex < 0)
					throw new ArgumentOutOfRangeException("For provided value of raw, startIndex must be 0-" + (raw.Length - 4));
				_items = new byte[4];
				ArrayFunctions.TrimArray(raw, startIndex, _items);
				checkValues(this);
            }
			
			static void checkValues(Trigger t)
			{
				string error = "";
				//Console.WriteLine(t.Condition);
				if (t.Condition > 24) error = "Condition (" + t.Condition + ")";
				
				if (t.VariableType == 10)
				{
					t.VariableType = 0;
					t.Variable = 0;
				}
				// NOTE:	This basically forces these bad types to be handled like what TFW did
				//			Is that right? Probably not! There's probably some extra text that isn't getting processed right.
				//			But it's not anything that would be popping up in the mission, so we're going to ignore this for now
                // kcc handling issue with type 89 & type 85
                else if (t.VariableType == 89 || t.VariableType == 85) 
				{
					t.VariableType = 1;
				}
                // kcc handling issue with type 70 (ship type, 100% of all unks, must always)
                else if (t.VariableType == 70) 
                {
                    t.VariableType = 2;
                }
				// kcc handling when should just be fg and first fg
                else if (t.VariableType == 12 || t.VariableType == 243 || t.VariableType == 228)
                {
                    t.VariableType = 1;
                    t.Variable = 0;
                }
                // kcc handling when should be ship type and first ship type (x-wing)
                else if (t.VariableType == 248 || t.VariableType == 255 || t.VariableType == 43 || t.VariableType == 66) // ship type, 100%, x-wings, always
                {
                    t.VariableType = 2;
					t.Variable = 0;
                }

                byte tempVar = t.Variable;


                CheckTarget(t.VariableType, ref tempVar, out string msg);
                //Console.WriteLine(t.VariableType + "," + t.Variable + "," + msg);
                t.Variable = tempVar;
				if (msg != "") error += (error != "" ? ", " : "") + msg;
				if (error != "") throw new ArgumentException("Invalid values detected: " + error + ".");
				// 66% to 75%, 33% to 50%, "each" to 100%, "each special" to "100% special"
				t.Amount = (byte)(t.Amount == 16 ? 1 : (t.Amount == 17 ? 2 : (t.Amount == 18 ? 0 : (t.Amount == 19 ? 6 : t.Amount))));
			}
			
			static byte[] craftUpgrade(Trigger t)
			{
				byte[] b = (byte[])t;
				if (b[1] == 2)
				{
					if (b[2] == 10) b[2] = 89;	// SHPYD
					else if (b[2] == 11) b[2] = 90;	// REPYD
					else if (b[2] == 31) b[2] = 77;	// G/PLT
					else if (b[2] == 39) b[2] = 91;	// M/SC
				}
				return b;
			}
			
			/// <summary>Returns a representative string of the Trigger</summary>
			/// <remarks>Flightgroups are identified as <b>"FG:#"</b> for later substitution if required</remarks>
			/// <returns>Description of the trigger and targets if applicable</returns>
			public override string ToString()
			{
				string trig = "";
				if (Condition != 0 /*TRUE*/ && Condition != 10 /*FALSE*/)
				{
					trig = BaseStrings.SafeString(Strings.Amount, Amount);
					trig += (trig.IndexOf(" of") >= 0 || trig.IndexOf(" in") >= 0) ? " " : " of ";
					switch (VariableType) //TODO: should make an enum for this...
					{
						case 1:
							trig += "FG:" + Variable;
							break;
						case 2:
							trig += "Ship type " + BaseStrings.SafeString(Strings.CraftType, Variable + 1);
							break;
						case 3:
							trig += "Ship class " + BaseStrings.SafeString(Strings.ShipClass, Variable);
							break;
						case 4:
							trig += "Object type " + BaseStrings.SafeString(Strings.ObjectType, Variable);
							break;
						case 5:
							trig += "IFF:" + Variable;
							break;
						case 6:
							trig += "Ship orders " + BaseStrings.SafeString(Strings.Orders, Variable);
							break;
						case 7:
							trig += "Craft When " + BaseStrings.SafeString(Strings.CraftWhen, Variable);
							break;
						case 8:
							trig += "Global Group " + Variable;
							break;
						case 9:
							trig += "AI Rating " + BaseStrings.SafeString(Strings.Rating, Variable);
							break;
                        case 0xA:
                            trig += "Craft with status: " + BaseStrings.SafeString(Strings.Status, Variable);
                            break;
                        case 0xB:
                            trig += "All craft";
                            break;
                        default:
							trig += VariableType + " " + Variable;
							break;
					}
					trig += " must ";
				}
				trig += BaseStrings.SafeString(Strings.Trigger, Condition);
				return trig;
			}
			
			/// <summary>Converts a Trigger to a byte array</summary>
			/// <param name="trig">The Trigger to convert</param>
			/// <returns>A byte array with a length of 4 containing the trigger data</returns>
			public static explicit operator byte[](Trigger trig)
			{
				byte[] b = new byte[4];
				for (int i = 0; i < 4; i++) b[i] = trig[i];
				return b;
			}
			/// <summary>Converts a Trigger for use in XvT</summary>
			/// <remarks>CraftType indexes for SHPYD, REPYD, G/PLT and M/SC will be updated</remarks>
			/// <param name="trig">The Trigger to convert</param>
			/// <returns>A copy of <paramref name="trig"/> for XvT</returns>
			public static implicit operator Xvt.Mission.Trigger(Trigger trig) { return new Xvt.Mission.Trigger(craftUpgrade(trig)); }
			/// <summary>Converts a Trigger for use in XWA</summary>
			/// <remarks>CraftType indexes for SHPYD, REPYD, G/PLT and M/SC will be updated</remarks>
			/// <param name="trig">The Trigger to convert</param>
			/// <returns>A copy of <paramref name="trig"/> for XWA</returns>
			public static implicit operator Xwa.Mission.Trigger(Trigger trig) { return new Xwa.Mission.Trigger(craftUpgrade(trig)); }
		}
	}
}
