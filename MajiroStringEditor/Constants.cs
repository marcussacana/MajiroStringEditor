using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MajiroStringEditor {
    class Constants {
        public const string EncHeader = "MajiroObjX1.000";
        public const string DecHeader = "MajiroObjV1.000";

        /// <summary>
        /// Append Dialogue Content
        /// </summary>
        public const ushort ShowText = 0x840;
        /// <summary>
        /// Do Action Related with the ADV
        /// </summary>
        public const ushort AdvEvent = 0x842;
        /// <summary>
        /// Unconditional Jump
        /// </summary>
        public const ushort UnconJmp = 0x82C;
        /// <summary>
        /// Set the last string ID
        /// </summary>
        public const ushort StringId = 0x83A;
        /// <summary>
        /// Parse the current string,
        /// Things like the character name and quotes.
        /// </summary>
        public const ushort ParseStr = 0x841;

        /// <summary>
        /// Magic?
        /// </summary>
        public const ushort AdvEvtType = 0x002;


        /// <summary>
        /// BreakLine
        /// </summary>
        public const ushort AdvBrkLine = 0x06E;
        /// <summary>
        /// Wait Click
        /// </summary>
        public const ushort AdvClkWait = 0x070;
        /// <summary>
        /// Clear Dialogue Text
        /// </summary>
        public const ushort AdvDialCls = 0x077;
    }
}
