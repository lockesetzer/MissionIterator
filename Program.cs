using Idmr.Common;
using Idmr.Platform;
using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic.FileIO;
using System.Linq;
using System.Collections.Generic;


namespace MissionIterator
{

    class Patch
    {
        public string Name { get; set; }
        public int ID { get; set; }
        public int Slot { get; set; }
    }

    class Program
    {
        //static private String fileMission = "C:\\Users\\kevin\\Documents\\Projects\\EH\\XWTC3\\Waistem.XWI"; // XW
        //static private String fileMission = "C:\\Users\\kevin\\Documents\\Projects\\EH\\TIETC195\\b1m1fm.tie"; // TIE
        //static private String fileMission = "C:\\Users\\kevin\\Documents\\Projects\\EH\\XvTTC49\\pac1.tie"; // XvT
        //static private String fileMission = "C:\\Users\\kevin\\Documents\\Projects\\EH\\BoPTC1\\CRUCOMP1.TIE"; // BoP
        //static private String fileMission = "C:\\Users\\kevin\\Documents\\Projects\\EH\\XWATC67\\1B8M1Karana.tie"; // XWA

        static private String dataStream = "..\\..\\..\\files.csv";
        static private String patchStream = "..\\..\\..\\patch.csv";
        static private String shipFile = "..\\..\\..\\ships.csv";

        static private int patchIDField = 0;
        static private int patchNameField = 1;
        static private int patchSlotField = 2;

        static private int missionPathField = 20;
        static private int missionPatchIdsField = 15;

        static void Main(string[] args)
        {

            Dictionary<int, Patch> patches = new Dictionary<int, Patch>();

            FileStream ps = File.OpenRead(patchStream);

            using TextFieldParser patchParser = new TextFieldParser(ps)
            {
                Delimiters = new[] { "," },
                HasFieldsEnclosedInQuotes = true,
                TextFieldType = FieldType.Delimited,
                TrimWhiteSpace = true,
            };

            while (!patchParser.EndOfData)
            {
                var fields = patchParser.ReadFields() ?? throw new InvalidOperationException("Parser unexpectedly returned no data");

                try
                {
                    int patchID = Int32.Parse(fields[patchIDField]);
                    string patchName = fields[patchNameField];
                    int patchSlot = Int32.Parse(fields[patchSlotField]);

                    Patch patch = new Patch();
                    patch.ID = patchID;
                    patch.Name = patchName;
                    patch.Slot = patchSlot;

                    patches[patchID] = patch;
                }

                catch (Exception e)
                {
                    Console.WriteLine("Skipping patch ID " + fields[0]);
                    Console.WriteLine(e);
                }


            }

            Boolean first = true;

            FileStream ds = File.OpenRead(dataStream);
            var ships = new StringBuilder();

            using TextFieldParser parser = new TextFieldParser(ds)
            {
                Delimiters = new[] { "," },
                HasFieldsEnclosedInQuotes = true,
                TextFieldType = FieldType.Delimited,
                TrimWhiteSpace = true,
            };

            while (!parser.EndOfData)
            {
                var fields = parser.ReadFields() ?? throw new InvalidOperationException("Parser unexpectedly returned no data");

                String line = "";

                foreach (String field in fields)
                {
                    if (line == "")
                    {
                        line = field;
                    }
                    else
                    {
                        line = line + "," + field;
                    }
                }

                if (first)
                {
                    first = false;
                    line = line + ",Ship Type,Flight Group Name";
                    ships.AppendLine(line);
                }

                else
                {
                    String filePatches = fields[missionPatchIdsField];
                    String fileMission = fields[missionPathField];

                    Console.WriteLine(fileMission);

                    CodePagesEncodingProvider.Instance.GetEncoding(437);
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    FileStream fs = File.OpenRead(fileMission);
                    MissionFile.Platform platform = MissionFile.GetPlatform(fs);

                    String playerShip = "";
                    String shipName = "";

                    if (platform == MissionFile.Platform.Xwing)
                    {
                        Idmr.Platform.Xwing.Mission mission = new Idmr.Platform.Xwing.Mission();

                        try
                        {
                            mission.LoadFromStream(fs);

                            string[] craftType = Idmr.Platform.Xwing.Strings.CraftType;

                            if (filePatches != null && filePatches.Length > 0)
                            {
                                string[] missionPatches = filePatches.Split('|');

                                foreach (var missionPatch in missionPatches)
                                {
                                    try
                                    {
                                        int patchID = Int32.Parse(missionPatch);
                                        Patch patch = patches[patchID];
                                        Console.WriteLine("Applying " + patch.Name + "," + patch.Slot);
                                        craftType[patch.Slot] = patch.Name;
        
                            }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine("Could not parse " + missionPatch);
                                        Console.WriteLine(e);
                                    }
                                }
                            }

                            Idmr.Platform.Xwing.FlightGroupCollection fgs = mission.FlightGroups;

                            foreach (Idmr.Platform.Xwing.FlightGroup fg in fgs)
                            {

                                byte playerCraft = fg.PlayerCraft;
                                bool isPlayer = false;

                                if (playerCraft > 0)
                                {
                                    isPlayer = true;
                                }

                                if (isPlayer)
                                {
                                    Console.WriteLine(fg.ToString());

                                    byte shipType = fg.CraftType;
                                    //Console.WriteLine(craftType[shipType]);
                                    playerShip = craftType[shipType];
                                    shipName = fg.Name;
                                    String shipLine = line + "," + playerShip + "," + shipName;
                                    ships.AppendLine(shipLine);

                                    //Console.WriteLine(fg.Name);
                                }

                            }
                        }
                        catch (Exception e)
                        {
                            String error = e.ToString();
                            error = error.Replace("\n", String.Empty);
                            error = error.Replace("\r", String.Empty);
                            error = error.Replace("\t", String.Empty);
                            error = error.Replace(",", String.Empty);

                            Console.WriteLine(error);

                            line = line + ",ERROR," + error;
                            ships.AppendLine(line);
                        }
                    }

                    else if (platform == MissionFile.Platform.TIE)
                    {
                        Idmr.Platform.Tie.Mission mission = new Idmr.Platform.Tie.Mission();
                        try
                        {
                            mission.LoadFromStream(fs);

                            string[] craftType = Idmr.Platform.Tie.Strings.CraftType;

                            if (filePatches != null && filePatches.Length > 0)
                            {
                                string[] missionPatches = filePatches.Split('|');

                                foreach (var missionPatch in missionPatches)
                                {
                                    try
                                    {
                                        int patchID = Int32.Parse(missionPatch);
                                        Patch patch = patches[patchID];
                                        Console.WriteLine("Applying " + patch.Name + "," + patch.Slot);
                                        craftType[patch.Slot] = patch.Name;
        
                            }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine("Could not parse " + missionPatch);
                                        Console.WriteLine(e);
                                    }
                                }
                            }

                            Idmr.Platform.Tie.FlightGroupCollection fgs = mission.FlightGroups;

                            foreach (Idmr.Platform.Tie.FlightGroup fg in fgs)
                            {

                                byte playerCraft = fg.PlayerCraft;
                                bool isPlayer = false;

                                if (playerCraft > 0)
                                {
                                    isPlayer = true;
                                }

                                if (isPlayer)
                                {
                                    Console.WriteLine(fg.ToString());

                                    byte shipType = fg.CraftType;
                                    //Console.WriteLine(craftType[shipType]);
                                    //Console.WriteLine(fg.Name);

                                    playerShip = craftType[shipType];
                                    shipName = fg.Name;
                                    String shipLine = line + "," + playerShip + "," + shipName;
                                    ships.AppendLine(shipLine);
                                }

                            }
                        }
                        catch (Exception e)
                        {
                            String error = e.ToString();
                            error = error.Replace("\n", String.Empty);
                            error = error.Replace("\r", String.Empty);
                            error = error.Replace("\t", String.Empty);
                            error = error.Replace(",", String.Empty);

                            Console.WriteLine(error);

                            line = line + ",ERROR," + error;
                            ships.AppendLine(line);
                        }
                    }

                    else if (platform == MissionFile.Platform.XvT)
                    {
                        Idmr.Platform.Xvt.Mission mission = new Idmr.Platform.Xvt.Mission();

                        try
                        {
                            mission.LoadFromStream(fs);

                            string[] craftType = Idmr.Platform.Xvt.Strings.CraftType;

                            if (filePatches != null && filePatches.Length > 0)
                            {
                                string[] missionPatches = filePatches.Split('|');

                                foreach (var missionPatch in missionPatches)
                                {
                                    try
                                    {
                                        int patchID = Int32.Parse(missionPatch);
                                        Patch patch = patches[patchID];
                                        Console.WriteLine("Applying " + patch.Name + "," + patch.Slot);
                                        craftType[patch.Slot] = patch.Name;
        
                            }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine("Could not parse " + missionPatch);
                                        Console.WriteLine(e);
                                    }
                                }
                            }

                            Idmr.Platform.Xvt.FlightGroupCollection fgs = mission.FlightGroups;

                            foreach (Idmr.Platform.Xvt.FlightGroup fg in fgs)
                            {

                                byte playerCraft = fg.PlayerNumber;
                                bool isPlayer = false;

                                if (playerCraft > 0)
                                {
                                    isPlayer = true;
                                }

                                if (isPlayer)
                                {
                                    Console.WriteLine(fg.ToString());

                                    byte shipType = fg.CraftType;
                                    //Console.WriteLine(craftType[shipType]);
                                    //Console.WriteLine(fg.Name);

                                    playerShip = craftType[shipType].Replace("*", "");
                                    shipName = fg.Name;
                                    String shipLine = line + "," + playerShip + "," + shipName;
                                    ships.AppendLine(shipLine);
                                }

                            }
                        }
                        catch (Exception e)
                        {
                            String error = e.ToString();
                            error = error.Replace("\n", String.Empty);
                            error = error.Replace("\r", String.Empty);
                            error = error.Replace("\t", String.Empty);
                            error = error.Replace(",", String.Empty);

                            Console.WriteLine(error);

                            line = line + ",ERROR," + error;
                            ships.AppendLine(line);
                        }
                    }

                    else if (platform == MissionFile.Platform.BoP)
                    {
                        Idmr.Platform.Xvt.Mission mission = new Idmr.Platform.Xvt.Mission();

                        try
                        {
                            mission.LoadFromStream(fs);

                            string[] craftType = Idmr.Platform.Xvt.Strings.CraftType;

                            if (filePatches != null && filePatches.Length > 0)
                            {
                                string[] missionPatches = filePatches.Split('|');

                                foreach (var missionPatch in missionPatches)
                                {
                                    try
                                    {
                                        int patchID = Int32.Parse(missionPatch);
                                        Patch patch = patches[patchID];
                                        Console.WriteLine("Applying " + patch.Name + "," + patch.Slot);
                                        craftType[patch.Slot] = patch.Name;
        
                            }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine("Could not parse " + missionPatch);
                                        Console.WriteLine(e);
                                    }
                                }
                            }

                            Idmr.Platform.Xvt.FlightGroupCollection fgs = mission.FlightGroups;
                            
                            foreach (Idmr.Platform.Xvt.FlightGroup fg in fgs)
                            {

                                byte playerCraft = fg.PlayerNumber;
                                bool isPlayer = false;

                                if (playerCraft > 0)
                                {
                                    isPlayer = true;
                                }

                                if (isPlayer)
                                {
                                    Console.WriteLine(fg.ToString());

                                    byte shipType = fg.CraftType;
                                    //Console.WriteLine(craftType[shipType]);
                                    //Console.WriteLine(fg.Name);
                                    playerShip = craftType[shipType].Replace("*", "");
                                    shipName = fg.Name;
                                    String shipLine = line + "," + playerShip + "," + shipName;
                                    ships.AppendLine(shipLine);
                                }

                            }
                        }
                        catch (Exception e)
                        {
                            String error = e.ToString();
                            error = error.Replace("\n", String.Empty);
                            error = error.Replace("\r", String.Empty);
                            error = error.Replace("\t", String.Empty);
                            error = error.Replace(",", String.Empty);

                            Console.WriteLine(error);

                            line = line + ",ERROR," + error;
                            ships.AppendLine(line);
                        }

                    }

                    else if (platform == MissionFile.Platform.XWA)
                    {
                        Idmr.Platform.Xwa.Mission mission = new Idmr.Platform.Xwa.Mission();
                        try
                        {
                            mission.LoadFromStream(fs);

                            string[] craftType = Idmr.Platform.Xwa.Strings.CraftType;

                            if (filePatches != null && filePatches.Length > 0)
                            {
                                string[] missionPatches = filePatches.Split('|');

                                foreach (var missionPatch in missionPatches)
                                {
                                    try
                                    {
                                        int patchID = Int32.Parse(missionPatch);
                                        Patch patch = patches[patchID];
                                        Console.WriteLine("Applying " + patch.Name + "," + patch.Slot);
                                        craftType[patch.Slot] = patch.Name;
        
                            }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine("Could not parse " + missionPatch);
                                        Console.WriteLine(e);
                                    }
                                }
                            }

                            Idmr.Platform.Xwa.FlightGroupCollection fgs = mission.FlightGroups;

                            foreach (Idmr.Platform.Xwa.FlightGroup fg in fgs)
                            {

                                byte playerCraft = fg.PlayerNumber;
                                bool isPlayer = false;

                                if (playerCraft > 0)
                                {
                                    isPlayer = true;
                                }

                                if (isPlayer)
                                {
                                    Console.WriteLine(fg.ToString());

                                    byte shipType = fg.CraftType;
                                    //Console.WriteLine(craftType[shipType]);
                                    //Console.WriteLine(fg.Name);

                                    playerShip = craftType[shipType];
                                    shipName = fg.Name;
                                    String shipLine = line + "," + playerShip + "," + shipName;
                                    ships.AppendLine(shipLine);
                                }

                            }
                        }
                        catch (Exception e)
                        {
                            String error = e.ToString();
                            error = error.Replace("\n", String.Empty);
                            error = error.Replace("\r", String.Empty);
                            error = error.Replace("\t", String.Empty);
                            error = error.Replace(",", String.Empty);

                            Console.WriteLine(error);

                            line = line + ",ERROR," + error;
                            ships.AppendLine(line);
                        }

                    }


                }

            }

            File.WriteAllText(shipFile, ships.ToString());

        }

    }
}
