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


namespace MissionIterator
{
    class Program
    {
        //static private String fileMission = "C:\\Users\\kevin\\Documents\\Projects\\EH\\XWTC3\\Waistem.XWI"; // XW
        //static private String fileMission = "C:\\Users\\kevin\\Documents\\Projects\\EH\\TIETC195\\b1m1fm.tie"; // TIE
        //static private String fileMission = "C:\\Users\\kevin\\Documents\\Projects\\EH\\XvTTC49\\pac1.tie"; // XvT
        //static private String fileMission = "C:\\Users\\kevin\\Documents\\Projects\\EH\\BoPTC1\\CRUCOMP1.TIE"; // BoP
        //static private String fileMission = "C:\\Users\\kevin\\Documents\\Projects\\EH\\XWATC67\\1B8M1Karana.tie"; // XWA

        static private String dataStream = "C:\\Users\\kevin\\Documents\\Projects\\EH\\files.csv";
        static private String shipFile = "C:\\Users\\kevin\\Documents\\Projects\\EH\\ships.csv";
        class MissionList
        {
            public string Path { get; set; }
        }

        static void Main(string[] args)
        {

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
                    String fileMission = fields[fields.Length - 1];

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
                                    string[] craftType = Idmr.Platform.Xwing.Strings.CraftType;
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
                                    string[] craftType = Idmr.Platform.Tie.Strings.CraftType;
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
                                    string[] craftType = Idmr.Platform.Xvt.Strings.CraftType;
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

                    else if (platform == MissionFile.Platform.BoP)
                    {
                        Idmr.Platform.Xvt.Mission mission = new Idmr.Platform.Xvt.Mission();

                        try
                        {
                            mission.LoadFromStream(fs);

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
                                    string[] craftType = Idmr.Platform.Xvt.Strings.CraftType;
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

                    else if (platform == MissionFile.Platform.XWA)
                    {
                        Idmr.Platform.Xwa.Mission mission = new Idmr.Platform.Xwa.Mission();
                        try
                        {
                            mission.LoadFromStream(fs);

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
                                    string[] craftType = Idmr.Platform.Xwa.Strings.CraftType;
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
