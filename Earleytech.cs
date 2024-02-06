using System.ComponentModel;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Earleytech
{
    public static class Xml
    {
        /// <summary>
        /// Uses xml.serialization to deserialize an object from an xml file.
        /// </summary>
        /// <param name="filePath">The fully qualified path to load from.</param>
        /// <returns>Nullable object containing data loaded from xml file.</returns>
        /// <exception cref="ArgumentException">Thrown if filePath is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the file can't be located.</exception>
        /// <exception cref="SerializationException">Thrown if the input is not XML data.</exception>
        /// <exception cref="InvalidCastException">Thrown if XML was loaded but could not parse to an object.</exception>"
        public static object? GetXMLObject(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("Invalid Path - Path can not be null or empty");
            }
            if (File.Exists(filePath))
            {
                XmlSerializer mySer = new(typeof(object));
                FileStream fs = new(filePath, FileMode.Open);
                object? obj = null;
                try
                {
                    obj = mySer.Deserialize(fs);
                }
                catch (Exception ex)
                {
                    if (ex is SerializationException)
                    {
                        throw new SerializationException("Loaded data does not appear to be xml", ex);
                    }
                    else if (ex is InvalidCastException)
                    {
                        throw new InvalidCastException("Could not cast xml data as type <object> or as null", ex);
                    }
                }
                fs.Dispose();
                return obj;
            }
            else { throw new FileNotFoundException("File could not be located.", filePath); }
        }

        /// <summary>
        /// Serializes and saves a given object to xml in a given file.
        /// </summary>
        /// <param name="obj">The Object to serialize.</param>
        /// <param name="filePath">The fully qualified file path and name to save the xml into - 'C:\Example.xml'.</param>
        /// <param name="createFile">Optional parameter to specity if the file should be created if it doesn't exist. Default is true.</param>
        /// <exception cref="ArgumentException">Thrown if obj is null or can't be cast to type object.</exception>
        /// <exception cref="FileNotFoundException">Thrown if createFile == false and the file can't be found.</exception>"
        public static void SaveXMLObject(object obj, string filePath, bool createFile = true)
        {
            if (!File.Exists(filePath) && createFile)
            {
                //Create the file if it isn't found and the caller hasn't specified not to.
                File.Create(filePath);
            }
            if (obj == null || obj is not object)
            {
                throw new ArgumentException("obj does not refer to a valid item of type <object>.");
            }
            XmlSerializer mySer = new(obj.GetType());
            FileStream fs = new FileStream(filePath, createFile ? FileMode.OpenOrCreate : FileMode.Open);
            mySer.Serialize(fs, obj);
            fs.Dispose();
            return;
        }
    }

    public static class Strings 
    {
        /// <summary>
        /// Returns the string version of a number, eg."1328000000" => "1.328 Billion", all the way up to the Uncentillion range.
        /// Handles all the way through double.MaxValue. Works with decimal points as well.
        /// Also supports conversion of an amount of seconds to Min:Sec or Hour:Min:Sec. Use enum Strings.StringifyOptions to select output type.
        /// </summary>
        /// <param name="input">String representation of number to convert.</param>
        /// <param name="option">Default is '.LongText'. Determines how output will be formatted ('1.328 Billion', '1.328B', '1.328E+9', etc).</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static string Stringify(string input, StringifyOptions option = StringifyOptions.LongText)
        {
            if (input == double.PositiveInfinity.ToString())
            {
                throw new ArgumentOutOfRangeException(nameof(input), input, "Positive infinity reached!");
            }
            else if (input == double.NegativeInfinity.ToString())
            {
                throw new ArgumentOutOfRangeException(nameof(input), input, "Negative Infinity reached!");
            }

            //IMPORTANT - Will not handle non-numerical input, just return it. Intended for 'lblMoney.Text = Stringify(myMoney.ToString(), StringifyOptions.LongText);' or similar!
            switch (option)
            {
                case (StringifyOptions.LongText):
                    {
                        double testparser = 0;
                        bool tryparseresult = false;
                        tryparseresult = double.TryParse(input, out testparser);
                        if (!tryparseresult)
                        {
                            //input is not parseable.
                            return input;
                        }
                        if (testparser < 1000000.0d)
                        {
                            //tryparseresult was true, so valid parse.
                            return testparser.ToString("N");
                        }
                        if (input.Contains("E")) { input = ReBig(input); }
                        //1,000.00
                        if (input.Length > 5 && input.Contains("."))
                        {
                            input = input.Split('.')[0];
                            //if the input is longer than 4 digits plus a decimal then we don't need the decimal for display.
                        }
                        //return string using classic long notation (#.### followed by Million, Billion, Trillion, etc)
                        string myOutput = "";

                        //index 0 = 7-9 length, index 1 = 10-12 length, index 2 = 13-15 length, etc
                        int digitcount = 0;
                        for (int i = 0; i < input.Length; i++)
                        {
                            if (input[i] != ',') { digitcount++; }
                        }
                        //input 1,935,342.35 => split: 1,935,342
                        if (digitcount < 7) { return input; }
                        string trimmedinput = "";
                        int index = 0;
                        int digits = 0;
                        while (digits < 4)
                        {
                            if (input[index] != ',')
                            {
                                trimmedinput += input[index];
                                digits++;
                            }
                            index++;
                        }   //1935
                        //digitcount % 3 gives period placement, unless it's 0. If 0, put period at index 3.
                        trimmedinput = trimmedinput.Insert((digitcount % 3 == 0 ? 3 : digitcount % 3), ".");    //1.935
                        trimmedinput += " ";
                        //7/3=2.333, 9/3=3, 10/3=3.333, 12/3=4, cast to double, do division, round up to nearest integer, subtract 3.
                        int wordindex = ((int)double.Round((double)digitcount / 3, MidpointRounding.ToPositiveInfinity) - 3);
                        myOutput = trimmedinput + Strings.TextStrings[wordindex];
                        return myOutput;
                    }
                case (StringifyOptions.SecondsToHourMinSec):
                    {
                        //input should be a number representing an amount of seconds, as a double. eg.'13528.6'. 
                        //calibrated for at least 999999 seconds.
                        //Note: Exception handling not in place yet!
                        int houraccum = 0;
                        int minaccum = 0;
                        double parsedinput = double.Parse(input);
                        while (parsedinput >= 3600.0d)
                        {
                            parsedinput -= 3600.0d;
                            houraccum++;
                        }
                        while (parsedinput >= 60.0d)
                        {
                            parsedinput -= 60.0d;
                            minaccum++;
                        }
                        return $"{houraccum.ToString("#00")}:{minaccum.ToString("00")}:{parsedinput.ToString("00.0")}";
                    }
                case (StringifyOptions.SecondsToMinSec):
                    {
                        //Calibrated for at least 999999 seconds.
                        int minaccum = 0;
                        double parsedinput = double.Parse(input);
                        while (parsedinput >= 60.0d)
                        {
                            parsedinput -= 60.0d;
                            minaccum++;
                        }
                        return $"{minaccum.ToString("###00")}:{parsedinput.ToString("00.0")}";
                    }
                case (StringifyOptions.ShortText):
                    {
                        double testparser = 0;
                        bool tryparseresult = false;
                        tryparseresult = double.TryParse(input, out testparser);
                        if (!tryparseresult)
                        {
                            //input is not parseable.
                            return input;
                        }
                        if (testparser < 1000000.0d)
                        {
                            //tryparseresult was true, so valid parse.
                            return testparser.ToString("N");
                        }
                        if (input.Contains("E")) { input = ReBig(input); }
                        //1,000.00
                        if (input.Length > 5 && input.Contains("."))
                        {
                            input = input.Split('.')[0];
                            //if the input is longer than 4 digits plus a decimal then we don't need the decimal for display.
                        }
                        //return string using classic long notation (#.### followed by Million, Billion, Trillion, etc)
                        string myOutput = "";

                        //index 0 = 7-9 length, index 1 = 10-12 length, index 2 = 13-15 length, etc
                        int digitcount = 0;
                        for (int i = 0; i < input.Length; i++)
                        {
                            if (input[i] != ',') { digitcount++; }
                        }
                        //input 1,935,342.35 => split: 1,935,342
                        if (digitcount < 7) { return input; }
                        string trimmedinput = "";
                        int index = 0;
                        int digits = 0;
                        while (digits < 4)
                        {
                            if (input[index] != ',')
                            {
                                trimmedinput += input[index];
                                digits++;
                            }
                            index++;
                        }   //1935
                        //digitcount % 3 gives period placement, unless it's 0. If 0, put period at index 3.
                        trimmedinput = trimmedinput.Insert((digitcount % 3 == 0 ? 3 : digitcount % 3), ".");    //1.935
                        trimmedinput += " ";
                        //7/3=2.333, 9/3=3, 10/3=3.333, 12/3=4, cast to double, do division, round up to nearest integer, subtract 3.
                        int wordindex = ((int)double.Round((double)digitcount / 3, MidpointRounding.ToPositiveInfinity) - 3);
                        myOutput = trimmedinput + Strings.ShortStrings[wordindex];
                        return myOutput;
                    }
                case (StringifyOptions.ScientificNotation):
                {
                        if (input.ToLower().Contains('e')) 
                        {
                            //may already be scientific notation. If it is, filter out the '+' - nobody wants to see that crap.
                            string output;
                            output = input.Replace("+", "");
                            output = input.Replace("E", "e");
                            int eIndex = output.ToLower().IndexOf('e');
                            if (!output.Contains("."))
                            {
                                //no decimal place

                                output = output.Insert(eIndex, ".000");
                            }
                            else if (eIndex < 5)
                            {
                                for (int i = eIndex; i < 5; i++)
                                {
                                    output = output.Insert(i, "0");
                                }
                            }
                            return output; 
                        }
                        double testparser = 0;
                        bool tryparseresult = false;
                        tryparseresult = double.TryParse(input, out testparser);
                        if (!tryparseresult)
                        {
                            //input is not parseable.
                            return input;
                        }
                        if (testparser < 1000000.0d)
                        {
                            //tryparseresult was true, so valid parse.
                            return testparser.ToString("N");
                        }
                        //filter out any commas and decimals. break the loop if you find a decimal.
                        string filteredInput = "";
                        for (int i = 0; i < input.Length; i++)
                        {
                            if (input[i] != ',') { filteredInput += input[i]; }
                            if (input[i] == '.') { break; }
                        }
                        int myExponent = filteredInput.Length - 1;
                        filteredInput = filteredInput.Substring(0, 4);
                        filteredInput = filteredInput.Insert(1, ".");
                        filteredInput += $"e{myExponent}";
                        return filteredInput;
                    }
                default:
                    {
                        return input;
                    }
            }
        }
        /// <summary>
        /// Takes a string input as scientific notation, '1.6E+28', and returns it's full version as a string: '16000000000000000000000000000'.
        /// </summary>
        /// <param name="input">Scientific notation string to 'standard notate'.</param>
        /// <returns>Literal representation of input number.</returns>
        /// <exception cref="ArgumentException">The input string did not contain 'E' or 'e', and so is not valid scientific notation.</exception>
        public static string ReBig(string input)
        {
            if (!input.ToLower().Contains("e"))
            {
                throw new ArgumentException("Param 'input' is not valid scientific notation.", input);
            }
            //Removes scientific notation and returns a string as a literal representation of the number.
            if (input[0] == '-') { input = input.Remove(0, 1); }
            string[] parts = input.Split('E');  //7.5, +27
            if (parts[1].Contains('+')) { parts[1] = parts[1].Remove(0, 1); }   //7.5, 27

            //remove decimal if it exists, and subtract it's distance to end of string from parts[1]: {7.5, 27} => {75, 26}
            if (parts[0].Contains('.'))
            {
                int subtractor = (parts[0].Length - 1) - parts[0].IndexOf("."); //7.5 gives 2-1=1, 4.23 gives 3-1=2, 2.331 gives 4-1=3
                parts[1] = (Int32.Parse(parts[1]) - subtractor).ToString();     //27-1=26
                parts[0] = parts[0].Remove(parts[0].IndexOf("."), 1);           //7.5 => 75
            }
            string output = parts[0];
            for (int i = 0; i < Int32.Parse(parts[1]); i++) { output += "0"; }
            return output;
        }
        public readonly static string[] TextStrings = { "Million", "Billion", "Trillion", "Quadrillion", "Quintillion", "Sextillion", "Septillion", "Octillion", "Nonillion", "Decillion", "Undecillion", "Duodecillion", "Tredecillion", "Quattordecillion", "Quindecillion", "Sexdecillion", "Septendecillion", "Octodecillion", "Novemdecillion", "Vigintillion",
                                                "Unvigintillion", "Duovigintillion", "Tresvigintillion", "Quattorvigintillion", "Quinvigintillion", "Sexvigintillion", "Septenvigintillion", "Octovigintillion", "Novemvigintillion", "Trigintillion", "Untrigintillion", "Duotrigintillion", "Tretrigintillion", "Quattortrigintillion", "Quintrigintillion",
                                                "Sextrigintillion", "Septentrigintillion", "Octotrigintillion", "Novemtrigintillion", "Quadragintillion", "Unquadragintillion", "Duoquadragintillion", "Trequadragintillion", "Quattorquadragintillion", "Quinquadragintillion", "Sexquadragintillion", "Septenquadragintillion", "Octoquadragintillion", "Novemquadragintillion",
                                                "Quinquagintillion", "Unquinquagintillion", "Duoquinquagintillion", "Trequinquagintillion", "Quattorquinquagintillion", "Quinquinquagintillion", "Sexquinquagintillion", "Septenquinquagintillion", "Octoquinquagintillion", "Novemquinquagintillion", "Sexagintillion", "Unsexagintillion", "Duosexagintillion",
                                                "Tresexagintillion", "Quattorsexagintillion", "Quinsexagintillion", "Sexsexagintillion", "Septensexagintillion", "Octosexagintillion", "Novemsexagintillion", "Septuagintillion", "Unseptuagintillion", "Duoseptuagintillion", "Treseptuagintillion", "Quattorseptuagintillion", "Quinseptuagintillion", "Sexseptuagintillion",
                                                "Septseptuagintillion", "Octoseptuagintillion", "Novemseptuagintillion", "Octogintillion", "Unoctogintillion", "Duooctogintillion", "Treoctogintillion", "Quattoroctogintillion", "Quinoctogintillion", "Sexoctogintillion", "Septoctogintillion", "Octoctogintillion", "Novemoctogintillion", "Nonagintillion",
                                                "Unnonagintillion", "Duononagintillion", "Trenonagintillion", "Quattornonagintillion", "Quinonagintillion", "Sexnonagintillion", "Septenonagintillion", "Octononagintillion", "Novemnonagintillion", "Centillion", "Uncentillion"};  //handles full size of type 'double'

        public readonly static string[] ShortStrings = { "M", "B","T","Qu","Qi","Sx","Sp","Oc","No","Dc","UnDc","DDc","TDc","QuDc","QiDc","SxDc","SpDc","OcDc","NoDc","Vi","UnVi","DVi","TVi","QuVi","QiVi","SxVi","SpVi","OcVi","NoVi","Tri","UnTri","DTri","TTri","QuTri","QiTri","SxTri","SpTri","OcTri","NoTri","Qd","UnQd","DQdi","TQdi","QuQdi","QiQdi","SxQdi","SpQdi","OcQdi","NoQdi","Qiq","UnQiq","DQiq","TQiq","QuQiq",
                                                           "QiQiq","SxQiq","SpQiq","OcQiq","NoQiq","Sxg","UnSxg","DSxg","TSxg","QuSxg","QiSxg","SxSxg","SpSxg","OcSxg","NoSxg","Spg","UnSpg","DSpg","TSpg","QuSpg","QiSpg","SxSpg","SpSpg","OcSpg","NoSpg","Ocg","UnOcg","DOcg","TOcg","QuOcg","QiOcg","SxOcg","SpOcg","OcOcg","NoOcg","Non","UnNon","DNon","TNon","QuNon","QiNon","SxNon","SpNon","OcNon","NoNon","Cn","UnCn"};
        [DefaultValue(StringifyOptions.LongText)]
        public enum StringifyOptions
        {
            LongText = 32,
            ShortText = 64,
            ScientificNotation = 128,
            SecondsToMinSec = 256,
            SecondsToHourMinSec = 512
        }
    }

}
