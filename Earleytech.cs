using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.NetworkInformation;
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
        public static string Stringify(string? input, StringifyOptions option = StringifyOptions.LongText)
        {
            if (input == null) { return ""; }
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

        /// <summary>
        /// Returns bool stating whether given string is a valid email address or not based on simple rules.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool ValidateEmail(string str) //functional
        {
            if (!str.Contains("@") || !str.Contains(".")) { return false; }
            if (str[0] == '@') { return false; }
            bool foundat = false;
            bool dotafterat = false;
            for (int i = str.IndexOf('@'); i < str.Length; i++)
            {
                if (str[i] == '@') { foundat = true; continue; }
                if (i == str.IndexOf('@') + 1)
                {
                    if (str[i] == '.') { return false; }
                }
                if (i == str.Length - 1)
                {
                    if (str[i] == '@' || str[i] == '.') { return false; }
                }
                if (str[i] == '.' && foundat) { dotafterat = true; }
            }
            if (!dotafterat) { return false; }
            return true;
        }
    }

    public static class Binary
    {
        /// <summary>
        /// Takes string binary representation ("10110110") and returns integer form. Base2 to Base10 Converter.
        /// Does not handle invalid inputs ("01101021", "a01101001")
        /// </summary>
        /// <param name="input">String containing binary representation: '01101101'</param>
        /// <returns>Base10 Int that the given binary refers to.</returns>
        public static int BinToInt(string input)
        {
            int startpower = input.Length - 1;
            int accum = 0;
            for (int i = 0; i < input.Length; i++)
            {
                accum += (int)(Int32.Parse(input[i].ToString()) * Math.Pow(2, startpower));
                startpower--;
            }
            return accum;
        }

        /// <summary>
        /// Returns string representation of a binary number from a given integer. (15) => "1111".
        /// </summary>
        /// <param name="input"></param>
        /// <returns>String representing binary (base 2) notation. '11010110'</returns>
        public static string IntToBin(int input)
        {
            int largestpower = 0;
            for (int i = 1; i <= 32; i++)
            {
                if (Math.Pow(2, i) > input) { largestpower = i - 1; break; }
            }
            string binary = "";
            while (input > 0 || largestpower >= 0)
            {
                if (input - Math.Pow(2, largestpower) >= 0)
                {
                    input -= (int)Math.Pow(2, largestpower);
                    largestpower--;
                    binary += "1";
                }
                else
                {
                    largestpower--;
                    binary += "0";
                }
            }
            return binary;
        }
    }

    public static class Networking
    {
        /// <summary>
        /// Tests string to see if it is a valid IPv4 address.
        /// </summary>
        /// <param name="strInput">IPv4 address as string - "192.168.254.254"</param>
        /// <param name="strIP">OUT - IPAddress object created if input string is a valid IPv4 address.</param>
        /// <returns>True if input was valid, false if not.</returns>
        public static bool IsValidIP(string strInput, out IPAddress? strIP) //functional - tests string to see if it is a valid ipv4 address
        {
            strIP = null;
            //return false if not exactly 4 groups after split operation based on '.' char.
            string[] groups = strInput.Split('.', StringSplitOptions.TrimEntries);
            if (groups.Length != 4) { return false; }
            byte outbyte;
            byte[] octets = new byte[4];
            for (int i = 0; i < groups.Length; i++)
            {
                if (byte.TryParse(groups[i], out outbyte)) { octets[i] = outbyte; } else { return false; }
                //tries to parse each substring as a byte (numerical 0-255), and if any of them fail, return false.
            }
            //if execution reaches here, we have successfully validated 4 bytes in IP format. return true after setting out to new IP.
            strIP = new IPAddress(octets);
            if (strIP == null) { return false; }
            return true;
        }

        /// <summary>
        /// Asynchronously pings a given IP address and returns a Tuple pairing round trip time with the IP.
        /// </summary>
        /// <param name="thisIP">DNS server or computer address to test.</param>
        /// <returns>Task of type Tuple<long, IPAddress> containing the IPAddress that was tested and it's round trip time in mS.</returns>
        static async Task<Tuple<long, IPAddress>> PingDNSAsync(IPAddress thisIP)
        {
            Ping thisping = new Ping();
            PingReply myreply = await thisping.SendPingAsync(thisIP);
            Tuple<long, IPAddress> myresult = new Tuple<long, IPAddress>(myreply.RoundtripTime, thisIP);
            return myresult;
        }
    }

}
namespace Earleytech.Notes
{
    [DefaultValue(NoteState.Active)]
    public enum NoteState
    {
        Active = 1,
        Inactive = 2,
        Completed = 4,
        Deleted = 8,
        Expired = 16,
        Failed = 32
    }
    public class Note : IEquatable<Note>, INotifyPropertyChanged, INote
    {
        #region Fields
        private NoteState state;
        private string? title;
        private string? text;
        private int id;
        #endregion

        #region Properties
        public NoteState State 
        { 
            get { return state; } 
            set 
            {
                if (state != value) 
                { 
                    state = value; 
                    OnNoteChanged(nameof(State)); 
                } 
            } 
        }
        public string? Title 
        { 
            get { return title; } 
            set 
            { 
                if (title != value) 
                { 
                    title = value; 
                    OnNoteChanged(nameof(Title)); 
                } 
            } 
        }
        public string? Text
        {
            get { return text; }
            set
            {
                if (text != value)
                {
                    text = value;
                    OnNoteChanged(nameof(Text));
                }
            }
        }
        public int ID 
        { 
            get { return id; } 
            private set 
            { 
                if (id != value) 
                { 
                    id = value; 
                    OnNoteChanged(nameof(ID)); 
                } 
            } 
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create empty note with default state and ID=-1.
        /// </summary>
        public Note()
        {
            this.state = default;
            this.title = "";
            this.text = "";
            this.id = -1;
        }
        /// <summary>
        /// Create empty note with default state and given ID.
        /// </summary>
        /// <param name="ID">ID to assign to the new note.</param>
        public Note(int ID)
        {
            this.state = default;
            this.title = "";
            this.text = "";
            this.id = ID;
        }
        /// <summary>
        /// Create new note containing Text with default state, empty title, and ID==-1.
        /// </summary>
        /// <param name="Text"></param>
        public Note(String Text)
        {
            this.state = default;
            this.title = "";
            this.text = Text;
            this.id = -1;
        }
        /// <summary>
        /// Create new note containing Text and Title with default state and ID==-1.
        /// </summary>
        /// <param name="Text"></param>
        /// <param name="Title"></param>
        public Note(String Text, String Title)
        {
            this.state = default;
            this.title = Title;
            this.text = Text;
            this.id = -1;
        }
        /// <summary>
        /// Create new note containing Text and State with empty title and ID==-1.
        /// </summary>
        /// <param name="Text"></param>
        /// <param name="State"></param>
        public Note(String Text, NoteState State)
        {
            this.state = State;
            this.text = Text;
            this.title = "";
            this.id = -1;
        }
        /// <summary>
        /// Create new note containing Text and ID with default state and empty title.
        /// </summary>
        /// <param name="Text"></param>
        /// <param name="ID"></param>
        public Note(String Text, int ID)
        {
            this.state = default;
            this.text = Text;
            this.title = "";
            this.id = ID;
        }
        /// <summary>
        /// Create new note containing Text, State and ID with empty title.
        /// </summary>
        /// <param name="Text"></param>
        /// <param name="State"></param>
        /// <param name="ID"></param>
        public Note(String Text, NoteState State, int ID)
        {
            this.state = State;
            this.text = Text;
            this.title = "";
            this.id = ID;
        }
        /// <summary>
        /// Create new note containing Text, Title and State with ID==-1.
        /// </summary>
        /// <param name="Text"></param>
        /// <param name="Title"></param>
        /// <param name="State"></param>
        public Note(String Text, String Title, NoteState State)
        {
            this.state = State;
            this.title = Title;
            this.text = Text;
            this.id = -1;
        }
        /// <summary>
        /// Create new note containing Text, Title and ID with default state.
        /// </summary>
        /// <param name="Text"></param>
        /// <param name="Title"></param>
        /// <param name="ID"></param>
        public Note(String Text, String Title, int ID)
        {
            this.state = default;
            this.title = Title;
            this.text = Text;
            this.id = ID;
        }
        /// <summary>
        /// Create new note containing Text, Title, State and ID.
        /// </summary>
        /// <param name="Text"></param>
        /// <param name="Title"></param>
        /// <param name="State"></param>
        /// <param name="ID"></param>
        public Note(String Text, String Title, NoteState State, int ID)
        {
            this.state = State;
            this.title = Title;
            this.text = Text;
            this.id = ID;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Set the ID property of this note.
        /// </summary>
        /// <param name="ID"></param>
        public void SetID(int ID)
        {
            this.ID = ID;
        }
        /// <summary>
        /// Compares this note to another note by all properties, and returns whether they match or not.
        /// </summary>
        /// <param name="b">Note to compare to</param>
        /// <returns><bool>true</bool> if they match, <bool>false</bool> if not.</returns>
        public virtual bool Equals(Note? b)
        {
            return (this != null && b != null && this.State == b.State && this.Title == b.Title && this.Text == b.Text && this.ID == b.ID);
        }
        public override bool Equals(object? obj)
        {
            return (obj is Note) && Equals((Note)obj);
        }
        public override int GetHashCode()
        {
            int accum = 0;
            accum ^= (int)this.State;
            accum ^= this.ID;
            if (this.Title != null)
            {
                foreach (char c in this.Title)
                {
                    accum ^= (int)c;
                }
            }
            if (this.Text != null)
            {
                foreach (char c in this.Text)
                {
                    accum ^= (int)c;
                }
            }
            return accum;
        }
        #endregion

        #region Events
        /// <summary>
        /// Occurs when one of Note's properties change. 'e' contains PropertyName.
        /// EventHandler is raised by 'Note.OnNoteChanged()' if a property is changed (the set function calls OnNoteChanged) and if a
        /// 'method is subscribed' (NoteChanged.Add(delegate void) has been called).
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;
        #endregion

        #region EventCallers
        private void OnNoteChanged(string PropertyName)
        {
            if (PropertyChanged != null && PropertyName != null) 
            { 
                PropertyChangedEventArgs e = new PropertyChangedEventArgs(PropertyName);
                PropertyChanged(this, e); 
            }
        }
        #endregion
    }

    public class NoteStore : IEnumerable<Note>, INotifyPropertyChanged
    {
        private List<Note> notes { get; set; }
        /// <summary>
        /// Default constructor - creates an empty NoteStore.
        /// </summary>
        public NoteStore()
        {
            this.notes = new List<Note>();
        }
        /// <summary>
        /// Creates a NoteStore containing one object 'note'.
        /// </summary>
        /// <param name="note">The one note for the new NoteStore to contain.</param>
        public NoteStore(Note note)
        {
            this.notes = new List<Note>() { note };
            OnPropertyChanged(note.ID);
        }
        /// <summary>
        /// Creates a NoteStore from a list or array of notes.
        /// </summary>
        /// <param name="notes">The IEnumberable collection of type Note.</param>
        public NoteStore(IEnumerable<Note> notes)
        {
            this.notes = notes.ToList();
            foreach (Note note in notes)
            {
                OnPropertyChanged(note.ID);
            }
        }
        /// <summary>
        /// Returns the full internal list of notes in this NoteStore.
        /// </summary>
        /// <returns>(List<Note>)this.notes</returns>
        public List<Note> GetAll()
        {
            return this.notes;
        }
        public Note? GetNoteByTitle(String title)
        {
            return notes.Find(x => x.Title == title);
        }
        public Note? GetNoteByText(String text)
        {
            return notes.Find(x => x.Text == text);
        }
        public Note? GetNoteByState(NoteState state)
        {
            return notes.Find(x => x.State == state);
        }
        public Note? GetNoteByID(int ID)
        {
            return notes.Find(x => x.ID == ID);
        }
        /// <summary>
        /// Returns a list of notes that match the given NoteState.
        /// </summary>
        /// <param name="state">NoteState enum param to match.</param>
        /// <returns>List of type Note of all notes matching the given NoteState.</returns>
        public List<Note> GetNotesByState(NoteState state)
        {
            return this.notes.Where(x => x.State == state).ToList();
        }
        /// <summary>
        /// Returns a list of notes that match the given Title.
        /// </summary>
        /// <param name="title">Title string param to match.</param>
        /// <returns>List of type Note of all notes matching the given Title.</returns>
        public List<Note> GetNotesByTitle(String title)
        {
            return this.notes.Where(x => x.Title == title).ToList();
        }
        /// <summary>
        /// Returns a list of notes that match the given Text.
        /// </summary>
        /// <param name="text">Text string param to match.</param>
        /// <returns>List of type Note of all notes matching the given Text.</returns>
        public List<Note> GetNotesByText(String text)
        {
            return this.notes.Where(x => x.Text == text).ToList();
        }
        /// <summary>
        /// Adds a single note to the NoteStore.
        /// </summary>
        /// <param name="note">Note to add.</param>
        public void Add(Note note)
        {
            this.notes.Add(note);
            OnPropertyChanged(note.ID);
        }
        /// <summary>
        /// Adds a List of notes to the end of the NoteStore.
        /// </summary>
        /// <param name="notes">List of notes to append.</param>
        public void Add(List<Note> notes)
        {
            this.notes.AddRange(notes);
            foreach (Note note in notes) { OnPropertyChanged(note.ID); }
        }
        /// <summary>
        /// Inserts the given note at the given index within this NoteStore.
        /// </summary>
        /// <param name="index">0-based index for where to insert note.</param>
        /// <param name="note">The note to be inserted.</param>
        public void AddAt(int index, Note note)
        {
            this.notes.Insert(index, note);
            OnPropertyChanged(note.ID);
        }
        /// <summary>
        /// Inserts the given List of notes at the given index within this NoteStore.
        /// </summary>
        /// <param name="index">0-based index for where to insert the list.</param>
        /// <param name="notesToAdd">The list of notes to add.</param>
        public void AddAt(int index, List<Note> notesToAdd)
        {
            List<Note> firstList = notes.GetRange(0, index);
            List<Note> secondList = notes.GetRange(index, notes.Count - index);
            firstList.AddRange(notesToAdd);
            firstList.AddRange(secondList);
            notes = firstList;
            foreach (Note note in notesToAdd)
            {
                OnPropertyChanged(note.ID);
            }
        }
        /// <summary>
        /// Deletes all notes and reinitializes this NoteStore's internal List<Note>.
        /// </summary>
        public void DeleteAll()
        {
            this.notes = new List<Note>();
        }
        /// <summary>
        /// Deletes one Note from the specified index.
        /// </summary>
        /// <param name="index">Index of the note to delete.</param>
        public void DeleteAt(int index)
        {
            this.notes.RemoveAt(index);
        }
        /// <summary>
        /// Deletes all occurrences of notes matching the given note. Note.Equals() compares State, Title and Text to determine equality.
        /// </summary>
        /// <param name="note">The note to look for and delete.</param>
        public void Delete(Note note)
        {
            this.notes.RemoveAll(x => x.Equals(note));
        }
        /// <summary>
        /// Deletes the first occurrence of the given note, by using Note.Equals.
        /// </summary>
        /// <param name="note">The note to look for and delete.</param>
        public void DeleteFirst(Note note)
        {
            this.notes.Remove(note);
        }
        /// <summary>
        /// Deletes all notes with the given title.
        /// </summary>
        /// <param name="Title">Title string to look for and delete.</param>
        /// <returns>Count of deleted items.</returns>
        public int DeleteByTitle(String Title)
        {
            int delcount = this.notes.RemoveAll(x => x.Title == Title);
            return delcount;
        }
        /// <summary>
        /// Deletes all notes with the given text.
        /// </summary>
        /// <param name="Text">Text string to look for and delete.</param>
        /// <returns>Count of deleted items.</returns>
        public int DeleteByText(String Text)
        {
            int delcount = this.notes.RemoveAll(x => x.Text == Text);
            return delcount;
        }
        /// <summary>
        /// Deletes all notes with the given state.
        /// </summary>
        /// <param name="State">NoteState parameter to look for and delete.</param>
        /// <returns>Count of deleted items.</returns>
        public int DeleteByState(NoteState State)
        {
            int delcount = this.notes.RemoveAll(x => x.State == State);
            return delcount;
        }
        /// <summary>
        /// Deletes the first note matching the given ID, if found.
        /// </summary>
        /// <param name="ID">ID of note to delete from this NoteStore.</param>
        /// <returns><bool>true</bool> if a matching ID is found and deleted, <bool>false</bool> otherwise.</returns>
        public bool DeleteByID(int ID)
        {
            Note? mynote = notes.Find(x => x.ID == ID);
            if (mynote != default) { return notes.Remove(mynote); } else { return false; }
        }
        public bool UpdateNote(Note note)
        {
            Note? searchResult = notes.Find(x => x.ID == note.ID);
            if (searchResult is not null)
            {
                searchResult = note;
                OnPropertyChanged(note.ID);
                return true;
            }
            else { return false; }

        }
        public IEnumerator<Note> GetEnumerator()
        {
            return notes.GetEnumerator();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return notes.GetEnumerator();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(int NoteID)
        {
            if (PropertyChanged != null && NoteID >= 0)
            {
                PropertyChangedEventArgs e = new PropertyChangedEventArgs(NoteID.ToString());
                PropertyChanged(this, e);
            }
        }
    }
    /// <summary>
    /// Defines minimum requrements for creating a 'Note' object.
    /// </summary>
    public interface INote : IEquatable<Note>
    {
        /// <summary>
        /// All Notes must have an 'ID' Property. This is to be a public property referencing a private backing field (public get, private set).
        /// </summary>
        int ID { get; }
        /// <summary>
        /// No point making a 'Note' if it can't store any content...
        /// </summary>
        string? Text { get; set; }
        /// <summary>
        /// Void method which sets the internal private _id field after instantiation. Must exist or ID is not changeable.
        /// </summary>
        /// <param name="ID">New ID value to set to.</param>
        public void SetID(int ID);

    }
}

namespace Earleytech.Huffman
{
    public static class Compression
    {
        /// <summary>
        /// Counts and categorizes ascii characters by frequency of appearance in string inputString,
        /// and returns an array of type int containing the frequencies, indexed as cast ascii value.
        /// </summary>
        /// <param name="inputString">Input string to categorize</param>
        /// <returns>ASCII char frequency map, indexed by value of ascii char.</returns>
        public static int[][] Categorize(string inputString)
        {
            //valid ascii chars(that we care about) range from 32-126.
            int[][] frequencies = new int[94][];
            //initialize each subarray.
            for (int i = 0; i < frequencies.Length; i++)
            {
                frequencies[i] = new int[2];
            }
            foreach (char c in inputString)
            {
                if ((int)c >= 32 && (int)c <= 126)
                {
                    //in each subarray, [0] is the char code, [1] is it's frequency of appearance. This makes it order-invariant.
                    frequencies[(int)c - 32][0] = (int)c;
                    frequencies[(int)c - 32][1]++;
                }

            }
            return frequencies;
        }
        public static string ReadFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                TextReader tReader = new StreamReader(fs);
                string outputString = tReader.ReadToEnd();
                fs.Dispose();
                return outputString;
            }
            else { throw new FileNotFoundException(); }
        }
        public static void BuildTree(int[][] inputFrequencies)
        {
            //sort jagged array in order of character appearance, lowest to highest.
            Array.Sort(inputFrequencies, Comparer<int[]>.Create(new Comparison<int[]>((x, y) => (x[1].CompareTo(y[1])))));
            //We're going to use a list to contain relevant entries, which is if value > 0.
            List<int[]> validFrequencies = new List<int[]>();
            validFrequencies = inputFrequencies.SkipWhile(x => x[1] == 0).ToList();
            
        }

        public interface IBaseNode
        {
            public char MyChar { get; }
            public bool isLeaf();
        }
        public class InternalNode : IBaseNode
        {
            public char MyChar { get; set; }
            public IBaseNode? LeftNode { get; }
            public IBaseNode? RightNode { get; }

            public bool isLeaf() { return false; }

        }
        public class LeafNode : IBaseNode
        {
            public char MyChar { get; set; }
            public int Weight { get; set; }
            public LeafNode(char myChar)
            {

            }
            public bool isLeaf() { return true; }
        }
    }
    
    public static class Decompression
    {

    }
}

namespace Earleytech.Search
{
    public interface INode
    {
        public int Value { get; }
    }
    public class Node : INode
    {
        public int Value { get; }
        public object? Payload { get; set; }
        public Node? Left { get; set; }
        public Node? Right { get; set; }
        public Node(int _value)
        {
            Value = _value;
        }
        public bool Search(int value)
        {
            if (value == this.Value) 
            { 
                return true; 
            }
            else
            {
                if (value < this.Value)
                {
                    if (this.Left != null)
                    {
                        return this.Left.Search(value);
                    }
                    else
                    {
                        this.Left = new Node(value);
                        return false;
                    }
                }
                else 
                {
                    if (this.Right != null)
                    {
                        return this.Right.Search(value);
                    }
                    else
                    {
                        this.Right = new Node(value);
                        return false;
                    }
                }
            }
        }
    }
    public class BinarySearchTree
    {
        public Node? rootNode;
        public bool FindValue(int value)
        {
            if (rootNode == null) { rootNode = new Node(value); }
            return rootNode.Search(value);
        }
    }
}
namespace Earleytech.Extensions
{
    public static class ArrayExtensions
    {
        /// <summary>
        /// Checks whether or not an int[] is in descending order.
        /// </summary>
        /// <param name="inputArr">The array to check</param>
        /// <param name="StrictMode">Optional parameter - If set to true, repeating values will cause a return of 'false'. Default is false.</param>
        /// <returns>Bool representing whether or not the input array is in descending order.</returns>
        public static bool IsDescending(this int[] inputArr, bool StrictMode = false)
        {
            //if length is less than 2, return true - single values can be seen as descending with themselves...
            if (inputArr.Length < 2) { return true; }
            if (StrictMode)
            {
                for (int i = 0; i < inputArr.Length - 1; i++)
                {
                    if (inputArr[i+1] >= inputArr[i]) { return false; }
                }
                return true;
            }
            //if strictmode is false, do the same thing, just don't pay attention if the value is the same as it's preceding.
            else
            {
                for (int i = 0; i < inputArr.Length - 1; i++)
                {
                    if (inputArr[i + 1] > inputArr[i]) { return false; }
                }
                return true;
            }
        }
        public static bool IsDescending(this uint[] inputArr, bool StrictMode = false)
        {
            //if length is less than 2, return true - single values can be seen as descending with themselves...
            if (inputArr.Length < 2) { return true; }
            if (StrictMode)
            {
                for (int i = 0; i < inputArr.Length - 1; i++)
                {
                    if (inputArr[i + 1] >= inputArr[i]) { return false; }
                }
                return true;
            }
            //if strictmode is false, do the same thing, just don't pay attention if the value is the same as it's preceding.
            else
            {
                for (int i = 0; i < inputArr.Length - 1; i++)
                {
                    if (inputArr[i + 1] > inputArr[i]) { return false; }
                }
                return true;
            }
        }
        public static bool IsDescending(this double[] inputArr, bool StrictMode = false)
        {
            //if length is less than 2, return true - single values can be seen as descending with themselves...
            if (inputArr.Length < 2) { return true; }
            if (StrictMode)
            {
                for (int i = 0; i < inputArr.Length - 1; i++)
                {
                    if (inputArr[i + 1] >= inputArr[i]) { return false; }
                }
                return true;
            }
            //if strictmode is false, do the same thing, just don't pay attention if the value is the same as it's preceding.
            else
            {
                for (int i = 0; i < inputArr.Length - 1; i++)
                {
                    if (inputArr[i + 1] > inputArr[i]) { return false; }
                }
                return true;
            }
        }
        public static bool IsDescending(this float[] inputArr, bool StrictMode = false)
        {
            //if length is less than 2, return true - single values can be seen as descending with themselves...
            if (inputArr.Length < 2) { return true; }
            if (StrictMode)
            {
                for (int i = 0; i < inputArr.Length - 1; i++)
                {
                    if (inputArr[i + 1] >= inputArr[i]) { return false; }
                }
                return true;
            }
            //if strictmode is false, do the same thing, just don't pay attention if the value is the same as it's preceding.
            else
            {
                for (int i = 0; i < inputArr.Length - 1; i++)
                {
                    if (inputArr[i + 1] > inputArr[i]) { return false; }
                }
                return true;
            }
        }
        public static bool IsDescending(this short[] inputArr, bool StrictMode = false)
        {
            //if length is less than 2, return true - single values can be seen as descending with themselves...
            if (inputArr.Length < 2) { return true; }
            if (StrictMode)
            {
                for (int i = 0; i < inputArr.Length - 1; i++)
                {
                    if (inputArr[i + 1] >= inputArr[i]) { return false; }
                }
                return true;
            }
            //if strictmode is false, do the same thing, just don't pay attention if the value is the same as it's preceding.
            else
            {
                for (int i = 0; i < inputArr.Length - 1; i++)
                {
                    if (inputArr[i + 1] > inputArr[i]) { return false; }
                }
                return true;
            }
        }
        public static bool IsDescending(this ushort[] inputArr, bool StrictMode = false)
        {
            //if length is less than 2, return true - single values can be seen as descending with themselves...
            if (inputArr.Length < 2) { return true; }
            if (StrictMode)
            {
                for (int i = 0; i < inputArr.Length - 1; i++)
                {
                    if (inputArr[i + 1] >= inputArr[i]) { return false; }
                }
                return true;
            }
            //if strictmode is false, do the same thing, just don't pay attention if the value is the same as it's preceding.
            else
            {
                for (int i = 0; i < inputArr.Length - 1; i++)
                {
                    if (inputArr[i + 1] > inputArr[i]) { return false; }
                }
                return true;
            }
        }
        public static bool IsDescending(this long[] inputArr, bool StrictMode = false)
        {
            //if length is less than 2, return true - single values can be seen as descending with themselves...
            if (inputArr.Length < 2) { return true; }
            if (StrictMode)
            {
                for (int i = 0; i < inputArr.Length - 1; i++)
                {
                    if (inputArr[i + 1] >= inputArr[i]) { return false; }
                }
                return true;
            }
            //if strictmode is false, do the same thing, just don't pay attention if the value is the same as it's preceding.
            else
            {
                for (int i = 0; i < inputArr.Length - 1; i++)
                {
                    if (inputArr[i + 1] > inputArr[i]) { return false; }
                }
                return true;
            }
        }
        public static bool IsDescending(this ulong[] inputArr, bool StrictMode = false)
        {
            //if length is less than 2, return true - single values can be seen as descending with themselves...
            if (inputArr.Length < 2) { return true; }
            if (StrictMode)
            {
                for (int i = 0; i < inputArr.Length - 1; i++)
                {
                    if (inputArr[i + 1] >= inputArr[i]) { return false; }
                }
                return true;
            }
            //if strictmode is false, do the same thing, just don't pay attention if the value is the same as it's preceding.
            else
            {
                for (int i = 0; i < inputArr.Length - 1; i++)
                {
                    if (inputArr[i + 1] > inputArr[i]) { return false; }
                }
                return true;
            }
        }
        public static bool IsDescending(this decimal[] inputArr, bool StrictMode = false)
        {
            //if length is less than 2, return true - single values can be seen as descending with themselves...
            if (inputArr.Length < 2) { return true; }
            if (StrictMode)
            {
                for (int i = 0; i < inputArr.Length - 1; i++)
                {
                    if (inputArr[i + 1] >= inputArr[i]) { return false; }
                }
                return true;
            }
            //if strictmode is false, do the same thing, just don't pay attention if the value is the same as it's preceding.
            else
            {
                for (int i = 0; i < inputArr.Length - 1; i++)
                {
                    if (inputArr[i + 1] > inputArr[i]) { return false; }
                }
                return true;
            }
        }
        public static bool IsDescending(this byte[] inputArr, bool StrictMode = false)
        {
            //if length is less than 2, return true - single values can be seen as descending with themselves...
            if (inputArr.Length < 2) { return true; }
            if (StrictMode)
            {
                for (int i = 0; i < inputArr.Length - 1; i++)
                {
                    if (inputArr[i + 1] >= inputArr[i]) { return false; }
                }
                return true;
            }
            //if strictmode is false, do the same thing, just don't pay attention if the value is the same as it's preceding.
            else
            {
                for (int i = 0; i < inputArr.Length - 1; i++)
                {
                    if (inputArr[i + 1] > inputArr[i]) { return false; }
                }
                return true;
            }
        }
        public static bool IsDescending(this sbyte[] inputArr, bool StrictMode = false)
        {
            //if length is less than 2, return true - single values can be seen as descending with themselves...
            if (inputArr.Length < 2) { return true; }
            if (StrictMode)
            {
                for (int i = 0; i < inputArr.Length - 1; i++)
                {
                    if (inputArr[i + 1] >= inputArr[i]) { return false; }
                }
                return true;
            }
            //if strictmode is false, do the same thing, just don't pay attention if the value is the same as it's preceding.
            else
            {
                for (int i = 0; i < inputArr.Length - 1; i++)
                {
                    if (inputArr[i + 1] > inputArr[i]) { return false; }
                }
                return true;
            }
        }
        public static bool IsDescending(this char[] inputArr, bool StrictMode = false)
        {
            //if length is less than 2, return true - single values can be seen as descending with themselves...
            if (inputArr.Length < 2) { return true; }
            if (StrictMode)
            {
                for (int i = 0; i < inputArr.Length - 1; i++)
                {
                    if (inputArr[i + 1] >= inputArr[i]) { return false; }
                }
                return true;
            }
            //if strictmode is false, do the same thing, just don't pay attention if the value is the same as it's preceding.
            else
            {
                for (int i = 0; i < inputArr.Length - 1; i++)
                {
                    if (inputArr[i + 1] > inputArr[i]) { return false; }
                }
                return true;
            }
        }

        #region BubbleSort
        public static double[] BubbleSort(this double[] inputArr, bool Descending = false) 
        {
            //should I be recursive or awaitable? We don't care about blocking the main thread, so don't need awaitable. Method can probably be written recursively and not
            //Step through array, swapping element b with a if b is less than a, keeping track of whether any swaps are needed.

            bool swapCheck;
            do
            {
                swapCheck = false;
                for (int i = 0; i < inputArr.Length - 1; i++) //loop through array checking elements a and b
                {
                    var Temp = inputArr[i];
                    if (!Descending)
                    {
                        if (Temp > inputArr[i + 1]) //swap position of the 2 elements if a is greater than b
                        {
                            inputArr[i] = inputArr[i + 1];
                            inputArr[i + 1] = Temp;
                            swapCheck = true; //enable swap flag
                        }
                    }
                    else
                    {
                        if (Temp < inputArr[i + 1])
                        {
                            inputArr[i] = inputArr[i + 1];
                            inputArr[i + 1] = Temp;
                            swapCheck = true;
                        }
                    }
                }
            }
            while (swapCheck);
            //while (swapCheck); //will loop until swapCheck makes it through as false
            return inputArr;

        }
        public static float[] BubbleSort(this float[] inputArr, bool Descending = false) 
        {
            //should I be recursive or awaitable? We don't care about blocking the main thread, so don't need awaitable. Method can probably be written recursively and not
            //Step through array, swapping element b with a if b is less than a, keeping track of whether any swaps are needed.

            bool swapCheck;
            do
            {
                swapCheck = false;
                for (int i = 0; i < inputArr.Length - 1; i++) //loop through array checking elements a and b
                {
                    var Temp = inputArr[i];
                    if (!Descending)
                    {
                        if (Temp > inputArr[i + 1]) //swap position of the 2 elements if a is greater than b
                        {
                            inputArr[i] = inputArr[i + 1];
                            inputArr[i + 1] = Temp;
                            swapCheck = true; //enable swap flag
                        }
                    }
                    else
                    {
                        if (Temp < inputArr[i + 1])
                        {
                            inputArr[i] = inputArr[i + 1];
                            inputArr[i + 1] = Temp;
                            swapCheck = true;
                        }
                    }
                }
            }
            while (swapCheck);
            //while (swapCheck); //will loop until swapCheck makes it through as false
            return inputArr;

        }
        public static int[] BubbleSort(this int[] inputArr, bool Descending = false)
        {
            //should I be recursive or awaitable? We don't care about blocking the main thread, so don't need awaitable. Method can probably be written recursively and not
            //Step through array, swapping element b with a if b is less than a, keeping track of whether any swaps are needed.

            bool swapCheck;
            do
            {
                swapCheck = false;
                for (int i = 0; i < inputArr.Length - 1; i++) //loop through array checking elements a and b
                {
                    var Temp = inputArr[i];
                    if (!Descending)
                    {
                        if (Temp > inputArr[i + 1]) //swap position of the 2 elements if a is greater than b
                        {
                            inputArr[i] = inputArr[i + 1];
                            inputArr[i + 1] = Temp;
                            swapCheck = true; //enable swap flag
                        }
                    }
                    else
                    {
                        if (Temp < inputArr[i + 1])
                        {
                            inputArr[i] = inputArr[i + 1];
                            inputArr[i + 1] = Temp;
                            swapCheck = true;
                        }
                    }
                }
            }
            while (swapCheck);
            //while (swapCheck); //will loop until swapCheck makes it through as false
            return inputArr;

        }
        public static uint[] BubbleSort(this uint[] inputArr, bool Descending = false)
        {
            //should I be recursive or awaitable? We don't care about blocking the main thread, so don't need awaitable. Method can probably be written recursively and not
            //Step through array, swapping element b with a if b is less than a, keeping track of whether any swaps are needed.

            bool swapCheck;
            do
            {
                swapCheck = false;
                for (int i = 0; i < inputArr.Length - 1; i++) //loop through array checking elements a and b
                {
                    var Temp = inputArr[i];
                    if (!Descending)
                    {
                        if (Temp > inputArr[i + 1]) //swap position of the 2 elements if a is greater than b
                        {
                            inputArr[i] = inputArr[i + 1];
                            inputArr[i + 1] = Temp;
                            swapCheck = true; //enable swap flag
                        }
                    }
                    else
                    {
                        if (Temp < inputArr[i + 1])
                        {
                            inputArr[i] = inputArr[i + 1];
                            inputArr[i + 1] = Temp;
                            swapCheck = true;
                        }
                    }
                }
            }
            while (swapCheck);
            //while (swapCheck); //will loop until swapCheck makes it through as false
            return inputArr;

        }
        public static long[] BubbleSort(this long[] inputArr, bool Descending = false)
        {
            //should I be recursive or awaitable? We don't care about blocking the main thread, so don't need awaitable. Method can probably be written recursively and not
            //Step through array, swapping element b with a if b is less than a, keeping track of whether any swaps are needed.

            bool swapCheck;
            do
            {
                swapCheck = false;
                for (int i = 0; i < inputArr.Length - 1; i++) //loop through array checking elements a and b
                {
                    var Temp = inputArr[i];
                    if (!Descending)
                    {
                        if (Temp > inputArr[i + 1]) //swap position of the 2 elements if a is greater than b
                        {
                            inputArr[i] = inputArr[i + 1];
                            inputArr[i + 1] = Temp;
                            swapCheck = true; //enable swap flag
                        }
                    }
                    else
                    {
                        if (Temp < inputArr[i + 1])
                        {
                            inputArr[i] = inputArr[i + 1];
                            inputArr[i + 1] = Temp;
                            swapCheck = true;
                        }
                    }
                }
            }
            while (swapCheck);
            //while (swapCheck); //will loop until swapCheck makes it through as false
            return inputArr;

        }
        public static ulong[] BubbleSort(this ulong[] inputArr, bool Descending = false)
        {
            //should I be recursive or awaitable? We don't care about blocking the main thread, so don't need awaitable. Method can probably be written recursively and not
            //Step through array, swapping element b with a if b is less than a, keeping track of whether any swaps are needed.

            bool swapCheck;
            do
            {
                swapCheck = false;
                for (int i = 0; i < inputArr.Length - 1; i++) //loop through array checking elements a and b
                {
                    var Temp = inputArr[i];
                    if (!Descending)
                    {
                        if (Temp > inputArr[i + 1]) //swap position of the 2 elements if a is greater than b
                        {
                            inputArr[i] = inputArr[i + 1];
                            inputArr[i + 1] = Temp;
                            swapCheck = true; //enable swap flag
                        }
                    }
                    else
                    {
                        if (Temp < inputArr[i + 1])
                        {
                            inputArr[i] = inputArr[i + 1];
                            inputArr[i + 1] = Temp;
                            swapCheck = true;
                        }
                    }
                }
            }
            while (swapCheck);
            //while (swapCheck); //will loop until swapCheck makes it through as false
            return inputArr;

        }
        public static char[] BubbleSort(this char[] inputArr, bool Descending = false)
        {
            //should I be recursive or awaitable? We don't care about blocking the main thread, so don't need awaitable. Method can probably be written recursively and not
            //Step through array, swapping element b with a if b is less than a, keeping track of whether any swaps are needed.

            bool swapCheck;
            do
            {
                swapCheck = false;
                for (int i = 0; i < inputArr.Length - 1; i++) //loop through array checking elements a and b
                {
                    var Temp = inputArr[i];
                    if (!Descending)
                    {
                        if (Temp > inputArr[i + 1]) //swap position of the 2 elements if a is greater than b
                        {
                            inputArr[i] = inputArr[i + 1];
                            inputArr[i + 1] = Temp;
                            swapCheck = true; //enable swap flag
                        }
                    }
                    else
                    {
                        if (Temp < inputArr[i + 1])
                        {
                            inputArr[i] = inputArr[i + 1];
                            inputArr[i + 1] = Temp;
                            swapCheck = true;
                        }
                    }
                }
            }
            while (swapCheck);
            //while (swapCheck); //will loop until swapCheck makes it through as false
            return inputArr;

        }
        public static sbyte[] BubbleSort(this sbyte[] inputArr, bool Descending = false)
        {
            //should I be recursive or awaitable? We don't care about blocking the main thread, so don't need awaitable. Method can probably be written recursively and not
            //Step through array, swapping element b with a if b is less than a, keeping track of whether any swaps are needed.

            bool swapCheck;
            do
            {
                swapCheck = false;
                for (int i = 0; i < inputArr.Length - 1; i++) //loop through array checking elements a and b
                {
                    var Temp = inputArr[i];
                    if (!Descending)
                    {
                        if (Temp > inputArr[i + 1]) //swap position of the 2 elements if a is greater than b
                        {
                            inputArr[i] = inputArr[i + 1];
                            inputArr[i + 1] = Temp;
                            swapCheck = true; //enable swap flag
                        }
                    }
                    else
                    {
                        if (Temp < inputArr[i + 1])
                        {
                            inputArr[i] = inputArr[i + 1];
                            inputArr[i + 1] = Temp;
                            swapCheck = true;
                        }
                    }
                }
            }
            while (swapCheck);
            //while (swapCheck); //will loop until swapCheck makes it through as false
            return inputArr;

        }
        public static byte[] BubbleSort(this byte[] inputArr, bool Descending = false)
        {
            //should I be recursive or awaitable? We don't care about blocking the main thread, so don't need awaitable. Method can probably be written recursively and not
            //Step through array, swapping element b with a if b is less than a, keeping track of whether any swaps are needed.

            bool swapCheck;
            do
            {
                swapCheck = false;
                for (int i = 0; i < inputArr.Length - 1; i++) //loop through array checking elements a and b
                {
                    var Temp = inputArr[i];
                    if (!Descending)
                    {
                        if (Temp > inputArr[i + 1]) //swap position of the 2 elements if a is greater than b
                        {
                            inputArr[i] = inputArr[i + 1];
                            inputArr[i + 1] = Temp;
                            swapCheck = true; //enable swap flag
                        }
                    }
                    else
                    {
                        if (Temp < inputArr[i + 1])
                        {
                            inputArr[i] = inputArr[i + 1];
                            inputArr[i + 1] = Temp;
                            swapCheck = true;
                        }
                    }
                }
            }
            while (swapCheck);
            //while (swapCheck); //will loop until swapCheck makes it through as false
            return inputArr;

        }
        public static ushort[] BubbleSort(this ushort[] inputArr, bool Descending = false)
        {
            //should I be recursive or awaitable? We don't care about blocking the main thread, so don't need awaitable. Method can probably be written recursively and not
            //Step through array, swapping element b with a if b is less than a, keeping track of whether any swaps are needed.

            bool swapCheck;
            do
            {
                swapCheck = false;
                for (int i = 0; i < inputArr.Length - 1; i++) //loop through array checking elements a and b
                {
                    var Temp = inputArr[i];
                    if (!Descending)
                    {
                        if (Temp > inputArr[i + 1]) //swap position of the 2 elements if a is greater than b
                        {
                            inputArr[i] = inputArr[i + 1];
                            inputArr[i + 1] = Temp;
                            swapCheck = true; //enable swap flag
                        }
                    }
                    else
                    {
                        if (Temp < inputArr[i + 1])
                        {
                            inputArr[i] = inputArr[i + 1];
                            inputArr[i + 1] = Temp;
                            swapCheck = true;
                        }
                    }
                }
            }
            while (swapCheck);
            //while (swapCheck); //will loop until swapCheck makes it through as false
            return inputArr;

        }
        public static short[] BubbleSort(this short[] inputArr, bool Descending = false)
        {
            //should I be recursive or awaitable? We don't care about blocking the main thread, so don't need awaitable. Method can probably be written recursively and not
            //Step through array, swapping element b with a if b is less than a, keeping track of whether any swaps are needed.

            bool swapCheck;
            do
            {
                swapCheck = false;
                for (int i = 0; i < inputArr.Length - 1; i++) //loop through array checking elements a and b
                {
                    var Temp = inputArr[i];
                    if (!Descending)
                    {
                        if (Temp > inputArr[i + 1]) //swap position of the 2 elements if a is greater than b
                        {
                            inputArr[i] = inputArr[i + 1];
                            inputArr[i + 1] = Temp;
                            swapCheck = true; //enable swap flag
                        }
                    }
                    else
                    {
                        if (Temp < inputArr[i + 1])
                        {
                            inputArr[i] = inputArr[i + 1];
                            inputArr[i + 1] = Temp;
                            swapCheck = true;
                        }
                    }
                }
            }
            while (swapCheck);
            //while (swapCheck); //will loop until swapCheck makes it through as false
            return inputArr;

        }
        public static decimal[] BubbleSort(this decimal[] inputArr, bool Descending = false)
        {
            //should I be recursive or awaitable? We don't care about blocking the main thread, so don't need awaitable. Method can probably be written recursively and not
            //Step through array, swapping element b with a if b is less than a, keeping track of whether any swaps are needed.

            bool swapCheck;
            do
            {
                swapCheck = false;
                for (int i = 0; i < inputArr.Length - 1; i++) //loop through array checking elements a and b
                {
                    var Temp = inputArr[i];
                    if (!Descending)
                    {
                        if (Temp > inputArr[i + 1]) //swap position of the 2 elements if a is greater than b
                        {
                            inputArr[i] = inputArr[i + 1];
                            inputArr[i + 1] = Temp;
                            swapCheck = true; //enable swap flag
                        }
                    }
                    else
                    {
                        if (Temp < inputArr[i + 1])
                        {
                            inputArr[i] = inputArr[i + 1];
                            inputArr[i + 1] = Temp;
                            swapCheck = true;
                        }
                    }
                }
            }
            while (swapCheck);
            //while (swapCheck); //will loop until swapCheck makes it through as false
            return inputArr;

        }
        #endregion //BubbleSort
    }
}