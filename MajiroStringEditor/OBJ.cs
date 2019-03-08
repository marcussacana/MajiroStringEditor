using System;
using System.Collections.Generic;
using static MajiroStringEditor.Constants;
using System.Text;
using System.Linq;

namespace MajiroStringEditor
{
    public class Obj1
    {
        public Encoding Encoding = Encoding.GetEncoding(932);
        static KEY XOR = new KEY();

        byte[] Signature = Encoding.ASCII.GetBytes("Edited With MajiroStringEditor");
        byte[] Script;

        uint HeaderLen = 0;

        uint ScriptLen;
        uint FirstID = 0;
        string[] Strings;

        bool Edited = false;

        Dictionary<int, bool> IsName;
        Dictionary<int, uint> BegMap;
        Dictionary<int, uint> EndMap;
        List<ushort> LinesIds;

        const uint ByteCodeBegin = 0x28;
        public Obj1(byte[] Script) {
            this.Script = Script;
            string Header = ReadStrAt(0);
            if (Header == EncHeader)
                Decrypt();
            
        }

        public string[] Import() {
            BegMap = new Dictionary<int, uint>();
            EndMap = new Dictionary<int, uint>();
            IsName = new Dictionary<int, bool>();
            LinesIds = new List<ushort>();
            

            List<string> Strings = new List<string>();
            bool DialFinished = true;
            int Ind = 0;

            //At 0x18 have a prefixed length array of the script entry point table (functions) and contains 2 dword, HASH + Offset
            HeaderLen = 0x18 + ((ReadU32At(0x18) * (4 * 2)) + 4) + 4;//+4 Lenght DW, +4 = Script Length

            ScriptLen = ReadU32At(HeaderLen - 4) + HeaderLen;
            for (uint i = 0; i < Script.Length; i++) {
                if (EqualsAt(i, Signature)) {
                    ScriptLen = i;
                    Edited = true;
                }
            }

            bool InProxy = false;

            for (uint i = ByteCodeBegin;; ) {
                if (i + 2 >= (InProxy ? (uint)Script.Length : ScriptLen))
                    break;

                ushort Command = ReadU16At(i);
                switch (Command) {
                    default:
                        if (!DialFinished) {
                            if (Strings[Ind].EndsWith("[wait]"))
                                Strings[Ind] = Strings[Ind].Substring(0, Strings[Ind].Length - 6);

                            DialFinished = true;
                            EndMap[Ind++] = i;
                        }

                        i++;
                        break;
                    case ShowText:
                        i += 2;
                        if (!IsValidStr(i))
                            break;

                        if (!BegMap.ContainsKey(Ind))
                            BegMap[Ind] = i - 2;

                        string Str = ReadStrAt(i + 2);

                        //if (Str.Contains("With that, Maya"))
                        //    System.Diagnostics.Debugger.Break();

                        if (Str.StartsWith("「")) {
                            IsName[Ind] = true;
                            EndMap[Ind++] = i - 2;

                            BegMap[Ind] = i - 2;
                        }

                        DialFinished = false;

                        if (!Str.EndsWith("」"))
                            Str = Str.TrimStart('「');

                        if (Ind >= Strings.Count)
                            Strings.Add(string.Empty);

                        Strings[Ind] += Str;
                        i += ReadU16At(i) + 2u;
                        break;
                    case AdvEvent:
                        i += 2;
                        Command = ReadU16At(i);
                        if (Command != AdvEvtType)
                            break;

                        if (Ind >= Strings.Count) {
                            Strings.Add(string.Empty);
                            DialFinished = false;
                        }

                        i += 2;
                        Command = ReadU16At(i);
                        i += 2;
                        switch (Command) {
                            case AdvBrkLine:
                                Strings[Ind] += "\\n";
                                break;

                            case AdvClkWait:
                                Strings[Ind] += "[wait]";
                                break;

                            case AdvDialCls:
                                if (!DialFinished) {
                                    if (Strings[Ind].EndsWith("[wait]"))
                                        Strings[Ind] = Strings[Ind].Substring(0, Strings[Ind].Length - 6);

                                    EndMap[Ind] = i;

                                    DialFinished = true;
                                    Ind++;
                                }
                                break;
                        }
                        break;
                    case StringId:
                        i += 2;
                        ushort ID = ReadU16At(i);
                        if (FirstID == uint.MaxValue)
                            FirstID = ID;

                        LinesIds.Add(ID);
                        i += 2;
                        break;
                    case ParseStr:
                        i += 2;
                        break;
                    case UnconJmp:
                        if (!Edited) {
                            i += 6;
                            break;
                        }

                        i += 2;
                        unchecked {
                            long Jmp = (int)ReadU32At(i) + (int)i + 4;
                            i += 4;

                            //Verify if is a injected Jmp
                            if (i < ScriptLen && Jmp > ScriptLen) {
                                InProxy = true;
                                i = (uint)Jmp;
                            } else if (Jmp < ScriptLen && i > ScriptLen || i == Script.Length) {
                                InProxy = false;
                                i = (uint)Jmp;
                            }
                        }
                        break;
                }
            }

            this.Strings = new string[Strings.Count];
            Strings.CopyTo(this.Strings, 0);

            return Strings.ToArray();//Prevent edit the private array.
        }

        private bool EqualsAt(uint Index, byte[] Content) {
            if (Content.Length + Index >= Script.Length)
                return false;

            for (uint i = 0; i < Content.Length; i++) {
                if (Content[i] != Script[Index + i])
                    return false;
            }

            return true;
        }

        public byte[] Export(string[] Strings) => Export(Strings, false);
        public byte[] Export(string[] Strings, bool Encrypt) {
            byte[] Script = new byte[this.Script.Length];
            this.Script.CopyTo(Script, 0);

            List<byte> Injections = new List<byte>();
            Injections.AddRange(new byte[6]);
            Injections.AddRange(Signature);

            List<ushort> IDs = new List<ushort>(LinesIds);
            for (int i = 0; i < Strings.Length; i++) {
                bool FromName = IsName.ContainsKey(i - 1) && IsName[i - 1];
                bool Updated = Strings[i] != this.Strings[i];
                string Str = Strings[i];

                if (!Updated)
                    continue;

                Null(Script, BegMap[i], EndMap[i]);

                BuildJmp(BegMap[i], (uint)(Script.Length + Injections.Count)).CopyTo(Script, BegMap[i]);
                if (IsName.ContainsKey(i) && IsName[i]) {
                    Injections.AddRange(BuildStr(Str, false, GenValidId(IDs)));
                    Injections.AddRange(BuildJmp((uint)(Script.Length + Injections.Count), EndMap[i]));
                    continue;
                }
                if (FromName && !Str.StartsWith("「"))
                    Str = "「「" + Str;

                Injections.AddRange(ParseLine(Str, IDs));

                Injections.AddRange(GetU16(AdvEvent));
                Injections.AddRange(GetU16(AdvEvtType));
                Injections.AddRange(GetU16(AdvClkWait));

                Injections.AddRange(GetU16(AdvEvent));
                Injections.AddRange(GetU16(AdvEvtType));
                Injections.AddRange(GetU16(AdvDialCls));


                Injections.AddRange(BuildJmp((uint)(Script.Length + Injections.Count), EndMap[i]));
            }

            Injections.RemoveRange(0, 6);
            Injections.InsertRange(0, BuildJmp((uint)Injections.Count));

            long NewLen = Script.Length + Injections.Count - HeaderLen;
            GetU32((uint)NewLen).CopyTo(Script, HeaderLen - 4);

            Script = Script.Concat(Injections).ToArray();

            if (Encrypt)
                this.Encrypt(Script);

            return Script;
        }

        private byte[] ParseLine(string Line, List<ushort> IDs) {
            List<byte> Code = new List<byte>();
            string Buffer = string.Empty;
            while (Line != string.Empty) {
                if (Line.ToLower().StartsWith("\\n")) {
                    Code.AddRange(BuildStr(Buffer, true, GenValidId(IDs)));
                    Buffer = string.Empty;

                    Code.AddRange(GetU16(AdvEvent));
                    Code.AddRange(GetU16(AdvEvtType));
                    Code.AddRange(GetU16(AdvBrkLine));

                    Line = Line.Substring(2);
                    continue;
                }
                if (Line.ToLower().StartsWith("[wait]")) {
                    Code.AddRange(BuildStr(Buffer, true, GenValidId(IDs)));
                    Buffer = string.Empty;

                    Code.AddRange(GetU16(AdvEvent));
                    Code.AddRange(GetU16(AdvEvtType));
                    Code.AddRange(GetU16(AdvClkWait));

                    Line = Line.Substring(6);
                    continue;
                }
                if (Line.ToLower().StartsWith("[clear]")) {
                    Code.AddRange(BuildStr(Buffer, true, GenValidId(IDs)));
                    Buffer = string.Empty;

                    Code.AddRange(GetU16(AdvEvent));
                    Code.AddRange(GetU16(AdvEvtType));
                    Code.AddRange(GetU16(AdvDialCls));

                    Line = Line.Substring(7);
                    continue;
                }
                if (Line.ToLower().StartsWith("[line]")) {
                    Code.AddRange(BuildStr(Buffer, false, GenValidId(IDs)));
                    Buffer = string.Empty;

                    Line = "「「" + Line.Substring(6);
                }

                char c = Line.First();
                Line = Line.Substring(1);
                Buffer += c;
            }

            if (Buffer != string.Empty)
                Code.AddRange(BuildStr(Buffer, true, GenValidId(IDs)));
            

            return Code.ToArray();
        }

        private ushort GenValidId(List<ushort> Ids) {
            ushort Current = (ushort)FirstID;
            while (Ids.Contains(Current))
                Current++;

            Ids.Add(Current);
            return Current;
        }

        private byte[] BuildStr(string Str, bool Process, ushort ID) {
            List<byte> Buffer = new List<byte>();
            Buffer.AddRange(GetU16(ShowText));
            Buffer.AddRange(GetU16((ushort)(Encoding.GetByteCount(Str) + 1u)));
            Buffer.AddRange(Encoding.GetBytes(Str));
            Buffer.Add(0x00);
            if (Process)
                Buffer.AddRange(GetU16(ParseStr));
            Buffer.AddRange(GetU16(StringId));
            Buffer.AddRange(GetU16(ID));

            return Buffer.ToArray();
        }

        private void Null(byte[] Script, uint At, uint To) {
            for (uint i = At; i < To; i++)
                Script[i] = 0x00;
        }

        private byte[] BuildJmp(uint From, uint To) {
            long Val = To - (From + 6);

            List<byte> Buffer = new List<byte>();
            Buffer.AddRange(GetU16(UnconJmp));
            Buffer.AddRange(Get32((int)Val));

            return Buffer.ToArray();
        }

        private byte[] BuildJmp(uint Bytes) {
            return BuildJmp(0, Bytes + 6);
        }

        private bool IsValidStr(uint Index) {
            ushort Len = ReadU16At(Index);
            if (Len + Index + 2 >= Script.Length)
                return false;

            //If the string is empty (the length include the string null termination)
            if (Len < 2)
                return false;

            for (uint i = 0; i < Len - 1; i++)
                if (Script[Index + i + 2] == 0x00)
                    return false;

            if (Script[Index + Len + 1] != 0x00)
                return false;

            return true;
        }

        private void Decrypt() {
            for (int i = 0; i < ScriptLen; i++)
                Script[i + ByteCodeBegin] ^= XOR[i];

            WriteStr(Script, DecHeader, 0);
        }
        private void Encrypt(byte[] Script) {
            uint Len = BitConverter.ToUInt32(Script, 0x24);
            for (int i = 0; i < Len; i++)
                Script[i + 0x28] ^= XOR[i];

            WriteStr(Script, EncHeader, 0);
        }


        string ReadStrAt(uint Index) {
            List<byte> Buffer = new List<byte>();
            while (Script[Index] != 0x00)
                Buffer.Add(Script[Index++]);

            return Encoding.GetString(Buffer.ToArray());
        }

        void WriteStr(byte[] Data, string Content, uint At) {
            byte[] Buffer = Encoding.GetBytes(Content + "\x0");
            Buffer.CopyTo(Data, At);
        }

        uint ReadU32At(uint Index) {
            byte[] Arr = new byte[4];
            for (int i = 0; i < Arr.Length; i++)
                Arr[i] = Script[i + Index];

            return BitConverter.ToUInt32(Arr, 0);
        }

        ushort ReadU16At(uint Index) {
            byte[] Arr = new byte[2];
            for (int i = 0; i < Arr.Length; i++)
                Arr[i] = Script[i + Index];

            return BitConverter.ToUInt16(Arr, 0);
        }

        byte[] GetU16(ushort Val) => BitConverter.GetBytes(Val);
        byte[] GetU32(uint Val) => BitConverter.GetBytes(Val);
        byte[] Get32(int Val) => BitConverter.GetBytes(Val);
    }
}
