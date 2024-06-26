﻿namespace UglyToad.PdfPig.Fonts.Encodings
{
    /// <summary>
    /// Symbol encoding.
    /// </summary>
    public sealed class SymbolEncoding : Encoding
    {
        /// <summary>
        /// EncodingTable for Symbol
        /// PDF Spec 1.7 Page 1013 https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page1013
        /// Note spec has code values as octal (base 8) with leading zero (supported in 'C' and 'Java') but not by C#
        /// Code values are already converted to base 10 prior to compile.
        /// </summary>
        private static readonly (int, string)[] EncodingTable = {
            ( 65, "Alpha"),                     // for char 'A' using  65 as base10 equivilant (for C# source). Spec has 0101 octal.              ( 65,"Alpha")             <=> (0101, "Alpha")          ,
            ( 66, "Beta"),                      // for char 'B' using  66 as base10 equivilant (for C# source). Spec has 0102 octal.              ( 66,"Beta")              <=> (0102, "Beta")           ,
            ( 67, "Chi"),                       // for char 'C' using  67 as base10 equivilant (for C# source). Spec has 0103 octal.              ( 67,"Chi")               <=> (0103, "Chi")            ,
            ( 68, "Delta"),                     // for char 'D' using  68 as base10 equivilant (for C# source). Spec has 0104 octal.              ( 68,"Delta")             <=> (0104, "Delta")          ,
            ( 69, "Epsilon"),                   // for char 'E' using  69 as base10 equivilant (for C# source). Spec has 0105 octal.              ( 69,"Epsilon")           <=> (0105, "Epsilon")        ,
            ( 72, "Eta"),                       // for char 'H' using  72 as base10 equivilant (for C# source). Spec has 0110 octal.              ( 72,"Eta")               <=> (0110, "Eta")            ,
            (160, "Euro"),                      // for char ' ' using 160 as base10 equivilant (for C# source). Spec has 0240 octal.              (160,"Euro")              <=> (0240, "Euro")           ,
            ( 71, "Gamma"),                     // for char 'G' using  71 as base10 equivilant (for C# source). Spec has 0107 octal.              ( 71,"Gamma")             <=> (0107, "Gamma")          ,
            (193, "Ifraktur"),                  // for char 'Á' using 193 as base10 equivilant (for C# source). Spec has 0301 octal.              (193,"Ifraktur")          <=> (0301, "Ifraktur")       ,
            ( 73, "Iota"),                      // for char 'I' using  73 as base10 equivilant (for C# source). Spec has 0111 octal.              ( 73,"Iota")              <=> (0111, "Iota")           ,
            ( 75, "Kappa"),                     // for char 'K' using  75 as base10 equivilant (for C# source). Spec has 0113 octal.              ( 75,"Kappa")             <=> (0113, "Kappa")          ,
            ( 76, "Lambda"),                    // for char 'L' using  76 as base10 equivilant (for C# source). Spec has 0114 octal.              ( 76,"Lambda")            <=> (0114, "Lambda")         ,
            ( 77, "Mu"),                        // for char 'M' using  77 as base10 equivilant (for C# source). Spec has 0115 octal.              ( 77,"Mu")                <=> (0115, "Mu")             ,
            ( 78, "Nu"),                        // for char 'N' using  78 as base10 equivilant (for C# source). Spec has 0116 octal.              ( 78,"Nu")                <=> (0116, "Nu")             ,
            ( 87, "Omega"),                     // for char 'W' using  87 as base10 equivilant (for C# source). Spec has 0127 octal.              ( 87,"Omega")             <=> (0127, "Omega")          ,
            ( 79, "Omicron"),                   // for char 'O' using  79 as base10 equivilant (for C# source). Spec has 0117 octal.              ( 79,"Omicron")           <=> (0117, "Omicron")        ,
            ( 70, "Phi"),                       // for char 'F' using  70 as base10 equivilant (for C# source). Spec has 0106 octal.              ( 70,"Phi")               <=> (0106, "Phi")            ,
            ( 80, "Pi"),                        // for char 'P' using  80 as base10 equivilant (for C# source). Spec has 0120 octal.              ( 80,"Pi")                <=> (0120, "Pi")             ,
            ( 89, "Psi"),                       // for char 'Y' using  89 as base10 equivilant (for C# source). Spec has 0131 octal.              ( 89,"Psi")               <=> (0131, "Psi")            ,
            (194, "Rfraktur"),                  // for char 'Â' using 194 as base10 equivilant (for C# source). Spec has 0302 octal.              (194,"Rfraktur")          <=> (0302, "Rfraktur")       ,
            ( 82, "Rho"),                       // for char 'R' using  82 as base10 equivilant (for C# source). Spec has 0122 octal.              ( 82,"Rho")               <=> (0122, "Rho")            ,
            ( 83, "Sigma"),                     // for char 'S' using  83 as base10 equivilant (for C# source). Spec has 0123 octal.              ( 83,"Sigma")             <=> (0123, "Sigma")          ,
            ( 84, "Tau"),                       // for char 'T' using  84 as base10 equivilant (for C# source). Spec has 0124 octal.              ( 84,"Tau")               <=> (0124, "Tau")            ,
            ( 81, "Theta"),                     // for char 'Q' using  81 as base10 equivilant (for C# source). Spec has 0121 octal.              ( 81,"Theta")             <=> (0121, "Theta")          ,
            ( 85, "Upsilon"),                   // for char 'U' using  85 as base10 equivilant (for C# source). Spec has 0125 octal.              ( 85,"Upsilon")           <=> (0125, "Upsilon")        ,
            (161, "Upsilon1"),                  // for char '¡' using 161 as base10 equivilant (for C# source). Spec has 0241 octal.              (161,"Upsilon1")          <=> (0241, "Upsilon1")       ,
            ( 88, "Xi"),                           // for char 'X' using  88 as base10 equivilant (for C# source). Spec has 0130 octal.              ( 88,"Xi")                <=> (0130, "Xi")             ,
            ( 90, "Zeta"),                      // for char 'Z' using  90 as base10 equivilant (for C# source). Spec has 0132 octal.              ( 90,"Zeta")              <=> (0132, "Zeta")           ,
            (192, "aleph"),                     // for char 'À' using 192 as base10 equivilant (for C# source). Spec has 0300 octal.              (192,"aleph")             <=> (0300, "aleph")          ,
            ( 97, "alpha"),                     // for char 'a' using  97 as base10 equivilant (for C# source). Spec has 0141 octal.              ( 97,"alpha")             <=> (0141, "alpha")          ,
            ( 38, "ampersand"),                 // for char '&' using  38 as base10 equivilant (for C# source). Spec has 0046 octal.              ( 38,"ampersand")         <=> (0046, "ampersand")      ,
            (208, "angle"),                     // for char 'Ð' using 208 as base10 equivilant (for C# source). Spec has 0320 octal.              (208,"angle")             <=> (0320, "angle")          ,
            (225, "angleleft"),                 // for char 'á' using 225 as base10 equivilant (for C# source). Spec has 0341 octal.              (225,"angleleft")         <=> (0341, "angleleft")      ,
            (241, "angleright"),                // for char 'ñ' using 241 as base10 equivilant (for C# source). Spec has 0361 octal.              (241,"angleright")        <=> (0361, "angleright")     ,
            (187, "approxequal"),               // for char '»' using 187 as base10 equivilant (for C# source). Spec has 0273 octal.              (187,"approxequal")       <=> (0273, "approxequal")    ,
            (171, "arrowboth"),                 // for char '«' using 171 as base10 equivilant (for C# source). Spec has 0253 octal.              (171,"arrowboth")         <=> (0253, "arrowboth")      ,
            (219, "arrowdblboth"),              // for char 'Û' using 219 as base10 equivilant (for C# source). Spec has 0333 octal.              (219,"arrowdblboth")      <=> (0333, "arrowdblboth")   ,
            (223, "arrowdbldown"),              // for char 'ß' using 223 as base10 equivilant (for C# source). Spec has 0337 octal.              (223,"arrowdbldown")      <=> (0337, "arrowdbldown")   ,
            (220, "arrowdblleft"),              // for char 'Ü' using 220 as base10 equivilant (for C# source). Spec has 0334 octal.              (220,"arrowdblleft")      <=> (0334, "arrowdblleft")   ,
            (222, "arrowdblright"),             // for char 'Þ' using 222 as base10 equivilant (for C# source). Spec has 0336 octal.              (222,"arrowdblright")     <=> (0336, "arrowdblright")  ,
            (221, "arrowdblup"),                // for char 'Ý' using 221 as base10 equivilant (for C# source). Spec has 0335 octal.              (221,"arrowdblup")        <=> (0335, "arrowdblup")     ,
            (175, "arrowdown"),                 // for char '¯' using 175 as base10 equivilant (for C# source). Spec has 0257 octal.              (175,"arrowdown")         <=> (0257, "arrowdown")      ,
            (190, "arrowhorizex"),              // for char '¾' using 190 as base10 equivilant (for C# source). Spec has 0276 octal.              (190,"arrowhorizex")      <=> (0276, "arrowhorizex")   ,
            (172, "arrowleft"),                 // for char '¬' using 172 as base10 equivilant (for C# source). Spec has 0254 octal.              (172,"arrowleft")         <=> (0254, "arrowleft")      ,
            (174, "arrowright"),                // for char '®' using 174 as base10 equivilant (for C# source). Spec has 0256 octal.              (174,"arrowright")        <=> (0256, "arrowright")     ,
            (173, "arrowup"),                   //              using 173 as base10 equivilant (for C# source). Spec has 0255 octal.              (173,"arrowup")           <=> (0255, "arrowup")        ,
            (189, "arrowvertex"),               // for char '½' using 189 as base10 equivilant (for C# source). Spec has 0275 octal.              (189,"arrowvertex")       <=> (0275, "arrowvertex")    ,
            ( 42, "asteriskmath"),              // for char '*' using  42 as base10 equivilant (for C# source). Spec has 0052 octal.              ( 42,"asteriskmath")      <=> (0052, "asteriskmath")   ,
            (124, "bar"),                       // for char '|' using 124 as base10 equivilant (for C# source). Spec has 0174 octal.              (124,"bar")               <=> (0174, "bar")            ,
            ( 98, "beta"),                      // for char 'b' using  98 as base10 equivilant (for C# source). Spec has 0142 octal.              ( 98,"beta")              <=> (0142, "beta")           ,
            (123, "braceleft"),                 // for char '{' using 123 as base10 equivilant (for C# source). Spec has 0173 octal.              (123,"braceleft")         <=> (0173, "braceleft")      ,
            (125, "braceright"),                // for char '}' using 125 as base10 equivilant (for C# source). Spec has 0175 octal.              (125,"braceright")        <=> (0175, "braceright")     ,
            (236, "bracelefttp"),               // for char 'ì' using 236 as base10 equivilant (for C# source). Spec has 0354 octal.              (236,"bracelefttp")       <=> (0354, "bracelefttp")    ,
            (237, "braceleftmid"),              // for char 'í' using 237 as base10 equivilant (for C# source). Spec has 0355 octal.              (237,"braceleftmid")      <=> (0355, "braceleftmid")   ,
            (238, "braceleftbt"),               // for char 'î' using 238 as base10 equivilant (for C# source). Spec has 0356 octal.              (238,"braceleftbt")       <=> (0356, "braceleftbt")    ,
            (252, "bracerighttp"),              // for char 'ü' using 252 as base10 equivilant (for C# source). Spec has 0374 octal.              (252,"bracerighttp")      <=> (0374, "bracerighttp")   ,
            (253, "bracerightmid"),             // for char 'ý' using 253 as base10 equivilant (for C# source). Spec has 0375 octal.              (253,"bracerightmid")     <=> (0375, "bracerightmid")  ,
            (254, "bracerightbt"),              // for char 'þ' using 254 as base10 equivilant (for C# source). Spec has 0376 octal.              (254,"bracerightbt")      <=> (0376, "bracerightbt")   ,
            (239, "braceex"),                   // for char 'ï' using 239 as base10 equivilant (for C# source). Spec has 0357 octal.              (239,"braceex")           <=> (0357, "braceex")        ,
            ( 91, "bracketleft"),               // for char '[' using  91 as base10 equivilant (for C# source). Spec has 0133 octal.              ( 91,"bracketleft")       <=> (0133, "bracketleft")    ,
            ( 93, "bracketright"),              // for char ']' using  93 as base10 equivilant (for C# source). Spec has 0135 octal.              ( 93,"bracketright")      <=> (0135, "bracketright")   ,
            (233, "bracketlefttp"),             // for char 'é' using 233 as base10 equivilant (for C# source). Spec has 0351 octal.              (233,"bracketlefttp")     <=> (0351, "bracketlefttp")  ,
            (234, "bracketleftex"),             // for char 'ê' using 234 as base10 equivilant (for C# source). Spec has 0352 octal.              (234,"bracketleftex")     <=> (0352, "bracketleftex")  ,
            (235, "bracketleftbt"),             // for char 'ë' using 235 as base10 equivilant (for C# source). Spec has 0353 octal.              (235,"bracketleftbt")     <=> (0353, "bracketleftbt")  ,
            (249, "bracketrighttp"),            // for char 'ù' using 249 as base10 equivilant (for C# source). Spec has 0371 octal.              (249,"bracketrighttp")    <=> (0371, "bracketrighttp") ,
            (250, "bracketrightex"),            // for char 'ú' using 250 as base10 equivilant (for C# source). Spec has 0372 octal.              (250,"bracketrightex")    <=> (0372, "bracketrightex") ,
            (251, "bracketrightbt"),            // for char 'û' using 251 as base10 equivilant (for C# source). Spec has 0373 octal.              (251,"bracketrightbt")    <=> (0373, "bracketrightbt") ,
            (183, "bullet"),                    // for char '·' using 183 as base10 equivilant (for C# source). Spec has 0267 octal.              (183,"bullet")            <=> (0267, "bullet")         ,
            (191, "carriagereturn"),            // for char '¿' using 191 as base10 equivilant (for C# source). Spec has 0277 octal.              (191,"carriagereturn")    <=> (0277, "carriagereturn") ,
            ( 99, "chi"),                       // for char 'c' using  99 as base10 equivilant (for C# source). Spec has 0143 octal.              ( 99,"chi")               <=> (0143, "chi")            ,
            (196, "circlemultiply"),            // for char 'Ä' using 196 as base10 equivilant (for C# source). Spec has 0304 octal.              (196,"circlemultiply")    <=> (0304, "circlemultiply") ,
            (197, "circleplus"),                // for char 'Å' using 197 as base10 equivilant (for C# source). Spec has 0305 octal.              (197,"circleplus")        <=> (0305, "circleplus")     ,
            (167, "club"),                      // for char '§' using 167 as base10 equivilant (for C# source). Spec has 0247 octal.              (167,"club")              <=> (0247, "club")           ,
            ( 58, "colon"),                     // for char ':' using  58 as base10 equivilant (for C# source). Spec has 0072 octal.              ( 58,"colon")             <=> (0072, "colon")          ,
            ( 44, "comma"),                     // for char ',' using  44 as base10 equivilant (for C# source). Spec has 0054 octal.              ( 44,"comma")             <=> (0054, "comma")          ,
            ( 64, "congruent"),                 // for char '@' using  64 as base10 equivilant (for C# source). Spec has 0100 octal.              ( 64,"congruent")         <=> (0100, "congruent")      ,
            (227, "copyrightsans"),             // for char 'ã' using 227 as base10 equivilant (for C# source). Spec has 0343 octal.              (227,"copyrightsans")     <=> (0343, "copyrightsans")  ,
            (211, "copyrightserif"),            // for char 'Ó' using 211 as base10 equivilant (for C# source). Spec has 0323 octal.              (211,"copyrightserif")    <=> (0323, "copyrightserif") ,
            (176, "degree"),                    // for char '°' using 176 as base10 equivilant (for C# source). Spec has 0260 octal.              (176,"degree")            <=> (0260, "degree")         ,
            (100, "delta"),                     // for char 'd' using 100 as base10 equivilant (for C# source). Spec has 0144 octal.              (100,"delta")             <=> (0144, "delta")          ,
            (168, "diamond"),                   // for char '¨' using 168 as base10 equivilant (for C# source). Spec has 0250 octal.              (168,"diamond")           <=> (0250, "diamond")        ,
            (184, "divide"),                    // for char '¸' using 184 as base10 equivilant (for C# source). Spec has 0270 octal.              (184,"divide")            <=> (0270, "divide")         ,
            (215, "dotmath"),                   // for char '×' using 215 as base10 equivilant (for C# source). Spec has 0327 octal.              (215,"dotmath")           <=> (0327, "dotmath")        ,
            ( 56, "eight"),                     // for char '8' using  56 as base10 equivilant (for C# source). Spec has 0070 octal.              ( 56,"eight")             <=> (0070, "eight")          ,
            (206, "element"),                   // for char 'Î' using 206 as base10 equivilant (for C# source). Spec has 0316 octal.              (206,"element")           <=> (0316, "element")        ,
            (188, "ellipsis"),                  // for char '¼' using 188 as base10 equivilant (for C# source). Spec has 0274 octal.              (188,"ellipsis")          <=> (0274, "ellipsis")       ,
            (198, "emptyset"),                  // for char 'Æ' using 198 as base10 equivilant (for C# source). Spec has 0306 octal.              (198,"emptyset")          <=> (0306, "emptyset")       ,
            (101, "epsilon"),                   // for char 'e' using 101 as base10 equivilant (for C# source). Spec has 0145 octal.              (101,"epsilon")           <=> (0145, "epsilon")        ,
            ( 61, "equal"),                     // for char '=' using  61 as base10 equivilant (for C# source). Spec has 0075 octal.              ( 61,"equal")             <=> (0075, "equal")          ,
            (186, "equivalence"),               // for char 'º' using 186 as base10 equivilant (for C# source). Spec has 0272 octal.              (186,"equivalence")       <=> (0272, "equivalence")    ,
            (104, "eta"),                       // for char 'h' using 104 as base10 equivilant (for C# source). Spec has 0150 octal.              (104,"eta")               <=> (0150, "eta")            ,
            ( 33, "exclam"),                    // for char '!' using  33 as base10 equivilant (for C# source). Spec has 0041 octal.              ( 33,"exclam")            <=> (0041, "exclam")         ,
            ( 36, "existential"),               // for char '$' using  36 as base10 equivilant (for C# source). Spec has 0044 octal.              ( 36,"existential")       <=> (0044, "existential")    ,
            ( 53, "five"),                      // for char '5' using  53 as base10 equivilant (for C# source). Spec has 0065 octal.              ( 53,"five")              <=> (0065, "five")           ,
            (166, "florin"),                    // for char '¦' using 166 as base10 equivilant (for C# source). Spec has 0246 octal.              (166,"florin")            <=> (0246, "florin")         ,
            ( 52, "four"),                      // for char '4' using  52 as base10 equivilant (for C# source). Spec has 0064 octal.              ( 52,"four")              <=> (0064, "four")           ,
            (164, "fraction"),                  // for char '¤' using 164 as base10 equivilant (for C# source). Spec has 0244 octal.              (164,"fraction")          <=> (0244, "fraction")       ,
            (103, "gamma"),                     // for char 'g' using 103 as base10 equivilant (for C# source). Spec has 0147 octal.              (103,"gamma")             <=> (0147, "gamma")          ,
            (209, "gradient"),                  // for char 'Ñ' using 209 as base10 equivilant (for C# source). Spec has 0321 octal.              (209,"gradient")          <=> (0321, "gradient")       ,
            ( 62, "greater"),                   // for char '>' using  62 as base10 equivilant (for C# source). Spec has 0076 octal.              ( 62,"greater")           <=> (0076, "greater")        ,
            (179, "greaterequal"),              // for char '³' using 179 as base10 equivilant (for C# source). Spec has 0263 octal.              (179,"greaterequal")      <=> (0263, "greaterequal")   ,
            (169, "heart"),                     // for char '©' using 169 as base10 equivilant (for C# source). Spec has 0251 octal.              (169,"heart")             <=> (0251, "heart")          ,
            (165, "infinity"),                  // for char '¥' using 165 as base10 equivilant (for C# source). Spec has 0245 octal.              (165,"infinity")          <=> (0245, "infinity")       ,
            (242, "integral"),                  // for char 'ò' using 242 as base10 equivilant (for C# source). Spec has 0362 octal.              (242,"integral")          <=> (0362, "integral")       ,
            (243, "integraltp"),                // for char 'ó' using 243 as base10 equivilant (for C# source). Spec has 0363 octal.              (243,"integraltp")        <=> (0363, "integraltp")     ,
            (244, "integralex"),                // for char 'ô' using 244 as base10 equivilant (for C# source). Spec has 0364 octal.              (244,"integralex")        <=> (0364, "integralex")     ,
            (245, "integralbt"),                // for char 'õ' using 245 as base10 equivilant (for C# source). Spec has 0365 octal.              (245,"integralbt")        <=> (0365, "integralbt")     ,
            (199, "intersection"),              // for char 'Ç' using 199 as base10 equivilant (for C# source). Spec has 0307 octal.              (199,"intersection")      <=> (0307, "intersection")   ,
            (105, "iota"),                      // for char 'i' using 105 as base10 equivilant (for C# source). Spec has 0151 octal.              (105,"iota")              <=> (0151, "iota")           ,
            (107, "kappa"),                     // for char 'k' using 107 as base10 equivilant (for C# source). Spec has 0153 octal.              (107,"kappa")             <=> (0153, "kappa")          ,
            (108, "lambda"),                    // for char 'l' using 108 as base10 equivilant (for C# source). Spec has 0154 octal.              (108,"lambda")            <=> (0154, "lambda")         ,
            ( 60, "less"),                      // for char '<' using  60 as base10 equivilant (for C# source). Spec has 0074 octal.              ( 60,"less")              <=> (0074, "less")           ,
            (163, "lessequal"),                 // for char '£' using 163 as base10 equivilant (for C# source). Spec has 0243 octal.              (163,"lessequal")         <=> (0243, "lessequal")      ,
            (217, "logicaland"),                // for char 'Ù' using 217 as base10 equivilant (for C# source). Spec has 0331 octal.              (217,"logicaland")        <=> (0331, "logicaland")     ,
            (216, "logicalnot"),                // for char 'Ø' using 216 as base10 equivilant (for C# source). Spec has 0330 octal.              (216,"logicalnot")        <=> (0330, "logicalnot")     ,
            (218, "logicalor"),                 // for char 'Ú' using 218 as base10 equivilant (for C# source). Spec has 0332 octal.              (218,"logicalor")         <=> (0332, "logicalor")      ,
            (224, "lozenge"),                   // for char 'à' using 224 as base10 equivilant (for C# source). Spec has 0340 octal.              (224,"lozenge")           <=> (0340, "lozenge")        ,
            ( 45, "minus"),                     // for char '-' using  45 as base10 equivilant (for C# source). Spec has 0055 octal.              ( 45,"minus")             <=> (0055, "minus")          ,
            (162, "minute"),                    // for char '¢' using 162 as base10 equivilant (for C# source). Spec has 0242 octal.              (162,"minute")            <=> (0242, "minute")         ,
            (109, "mu"),                        // for char 'm' using 109 as base10 equivilant (for C# source). Spec has 0155 octal.              (109,"mu")                <=> (0155, "mu")             ,
            (180, "multiply"),                  // for char '´' using 180 as base10 equivilant (for C# source). Spec has 0264 octal.              (180,"multiply")          <=> (0264, "multiply")       ,
            ( 57, "nine"),                      // for char '9' using  57 as base10 equivilant (for C# source). Spec has 0071 octal.              ( 57,"nine")              <=> (0071, "nine")           ,
            (207, "notelement"),                // for char 'Ï' using 207 as base10 equivilant (for C# source). Spec has 0317 octal.              (207,"notelement")        <=> (0317, "notelement")     ,
            (185, "notequal"),                  // for char '¹' using 185 as base10 equivilant (for C# source). Spec has 0271 octal.              (185,"notequal")          <=> (0271, "notequal")       ,
            (203, "notsubset"),                 // for char 'Ë' using 203 as base10 equivilant (for C# source). Spec has 0313 octal.              (203,"notsubset")         <=> (0313, "notsubset")      ,
            (110, "nu"),                        // for char 'n' using 110 as base10 equivilant (for C# source). Spec has 0156 octal.              (110,"nu")                <=> (0156, "nu")             ,
            ( 35, "numbersign"),                // for char '#' using  35 as base10 equivilant (for C# source). Spec has 0043 octal.              ( 35,"numbersign")        <=> (0043, "numbersign")     ,
            (119, "omega"),                     // for char 'w' using 119 as base10 equivilant (for C# source). Spec has 0167 octal.              (119,"omega")             <=> (0167, "omega")          ,
            (118, "omega1"),                    // for char 'v' using 118 as base10 equivilant (for C# source). Spec has 0166 octal.              (118,"omega1")            <=> (0166, "omega1")         ,
            (111, "omicron"),                   // for char 'o' using 111 as base10 equivilant (for C# source). Spec has 0157 octal.              (111,"omicron")           <=> (0157, "omicron")        ,
            ( 49, "one"),                       // for char '1' using  49 as base10 equivilant (for C# source). Spec has 0061 octal.              ( 49,"one")               <=> (0061, "one")            ,
            ( 40, "parenleft"),                 // for char '(' using  40 as base10 equivilant (for C# source). Spec has 0050 octal.              ( 40,"parenleft")         <=> (0050, "parenleft")      ,
            ( 41, "parenright"),                // for char ')' using  41 as base10 equivilant (for C# source). Spec has 0051 octal.              ( 41,"parenright")        <=> (0051, "parenright")     ,
            (230, "parenlefttp"),               // for char 'æ' using 230 as base10 equivilant (for C# source). Spec has 0346 octal.              (230,"parenlefttp")       <=> (0346, "parenlefttp")    ,
            (231, "parenleftex"),               // for char 'ç' using 231 as base10 equivilant (for C# source). Spec has 0347 octal.              (231,"parenleftex")       <=> (0347, "parenleftex")    ,
            (232, "parenleftbt"),               // for char 'è' using 232 as base10 equivilant (for C# source). Spec has 0350 octal.              (232,"parenleftbt")       <=> (0350, "parenleftbt")    ,
            (246, "parenrighttp"),              // for char 'ö' using 246 as base10 equivilant (for C# source). Spec has 0366 octal.              (246,"parenrighttp")      <=> (0366, "parenrighttp")   ,
            (247, "parenrightex"),              // for char '÷' using 247 as base10 equivilant (for C# source). Spec has 0367 octal.              (247,"parenrightex")      <=> (0367, "parenrightex")   ,
            (248, "parenrightbt"),              // for char 'ø' using 248 as base10 equivilant (for C# source). Spec has 0370 octal.              (248,"parenrightbt")      <=> (0370, "parenrightbt")   ,
            (182, "partialdiff"),               // for char '¶' using 182 as base10 equivilant (for C# source). Spec has 0266 octal.              (182,"partialdiff")       <=> (0266, "partialdiff")    ,
            ( 37, "percent"),                   // for char '%' using  37 as base10 equivilant (for C# source). Spec has 0045 octal.              ( 37,"percent")           <=> (0045, "percent")        ,
            ( 46, "period"),                    // for char '.' using  46 as base10 equivilant (for C# source). Spec has 0056 octal.              ( 46,"period")            <=> (0056, "period")         ,
            ( 94, "perpendicular"),             // for char '^' using  94 as base10 equivilant (for C# source). Spec has 0136 octal.              ( 94,"perpendicular")     <=> (0136, "perpendicular")  ,
            (102, "phi"),                       // for char 'f' using 102 as base10 equivilant (for C# source). Spec has 0146 octal.              (102,"phi")               <=> (0146, "phi")            ,
            (106, "phi1"),                      // for char 'j' using 106 as base10 equivilant (for C# source). Spec has 0152 octal.              (106,"phi1")              <=> (0152, "phi1")           ,
            (112, "pi"),                        // for char 'p' using 112 as base10 equivilant (for C# source). Spec has 0160 octal.              (112,"pi")                <=> (0160, "pi")             ,
            ( 43, "plus"),                      // for char '+' using  43 as base10 equivilant (for C# source). Spec has 0053 octal.              ( 43,"plus")              <=> (0053, "plus")           ,
            (177, "plusminus"),                 // for char '±' using 177 as base10 equivilant (for C# source). Spec has 0261 octal.              (177,"plusminus")         <=> (0261, "plusminus")      ,
            (213, "product"),                   // for char 'Õ' using 213 as base10 equivilant (for C# source). Spec has 0325 octal.              (213,"product")           <=> (0325, "product")        ,
            (204, "propersubset"),              // for char 'Ì' using 204 as base10 equivilant (for C# source). Spec has 0314 octal.              (204,"propersubset")      <=> (0314, "propersubset")   ,
            (201, "propersuperset"),            // for char 'É' using 201 as base10 equivilant (for C# source). Spec has 0311 octal.              (201,"propersuperset")    <=> (0311, "propersuperset") ,
            (181, "proportional"),              // for char 'µ' using 181 as base10 equivilant (for C# source). Spec has 0265 octal.              (181,"proportional")      <=> (0265, "proportional")   ,
            (121, "psi"),                       // for char 'y' using 121 as base10 equivilant (for C# source). Spec has 0171 octal.              (121,"psi")               <=> (0171, "psi")            ,
            ( 63, "question"),                  // for char '?' using  63 as base10 equivilant (for C# source). Spec has 0077 octal.              ( 63,"question")          <=> (0077, "question")       ,
            (214, "radical"),                   // for char 'Ö' using 214 as base10 equivilant (for C# source). Spec has 0326 octal.              (214,"radical")           <=> (0326, "radical")        ,
            ( 96, "radicalex"),                 // for char '`' using  96 as base10 equivilant (for C# source). Spec has 0140 octal.              ( 96,"radicalex")         <=> (0140, "radicalex")      ,
            (205, "reflexsubset"),              // for char 'Í' using 205 as base10 equivilant (for C# source). Spec has 0315 octal.              (205,"reflexsubset")      <=> (0315, "reflexsubset")   ,
            (202, "reflexsuperset"),            // for char 'Ê' using 202 as base10 equivilant (for C# source). Spec has 0312 octal.              (202,"reflexsuperset")    <=> (0312, "reflexsuperset") ,
            (226, "registersans"),              // for char 'â' using 226 as base10 equivilant (for C# source). Spec has 0342 octal.              (226,"registersans")      <=> (0342, "registersans")   ,
            (210, "registerserif"),             // for char 'Ò' using 210 as base10 equivilant (for C# source). Spec has 0322 octal.              (210,"registerserif")     <=> (0322, "registerserif")  ,
            (114, "rho"),                       // for char 'r' using 114 as base10 equivilant (for C# source). Spec has 0162 octal.              (114,"rho")               <=> (0162, "rho")            ,
            (178, "second"),                    // for char '²' using 178 as base10 equivilant (for C# source). Spec has 0262 octal.              (178,"second")            <=> (0262, "second")         ,
            ( 59, "semicolon"),                 // for char ';' using  59 as base10 equivilant (for C# source). Spec has 0073 octal.              ( 59,"semicolon")         <=> (0073, "semicolon")      ,
            ( 55, "seven"),                     // for char '7' using  55 as base10 equivilant (for C# source). Spec has 0067 octal.              ( 55,"seven")             <=> (0067, "seven")          ,
            (115, "sigma"),                     // for char 's' using 115 as base10 equivilant (for C# source). Spec has 0163 octal.              (115,"sigma")             <=> (0163, "sigma")          ,
            ( 86, "sigma1"),                    // for char 'V' using  86 as base10 equivilant (for C# source). Spec has 0126 octal.              ( 86,"sigma1")            <=> (0126, "sigma1")         ,
            (126, "similar"),                   // for char '~' using 126 as base10 equivilant (for C# source). Spec has 0176 octal.              (126,"similar")           <=> (0176, "similar")        ,
            ( 54, "six"),                       // for char '6' using  54 as base10 equivilant (for C# source). Spec has 0066 octal.              ( 54,"six")               <=> (0066, "six")            ,
            ( 47, "slash"),                     // for char '/' using  47 as base10 equivilant (for C# source). Spec has 0057 octal.              ( 47,"slash")             <=> (0057, "slash")          ,
            ( 32, "space"),                     // for char ' ' using  32 as base10 equivilant (for C# source). Spec has 0040 octal.              ( 32,"space")             <=> (0040, "space")          ,
            (170, "spade"),                     // for char 'ª' using 170 as base10 equivilant (for C# source). Spec has 0252 octal.              (170,"spade")             <=> (0252, "spade")          ,
            ( 39, "suchthat"),                  // for char ''' using  39 as base10 equivilant (for C# source). Spec has 0047 octal.              ( 39,"suchthat")          <=> (0047, "suchthat")       ,
            (229, "summation"),                 // for char 'å' using 229 as base10 equivilant (for C# source). Spec has 0345 octal.              (229,"summation")         <=> (0345, "summation")      ,
            (116, "tau"),                       // for char 't' using 116 as base10 equivilant (for C# source). Spec has 0164 octal.              (116,"tau")               <=> (0164, "tau")            ,
            ( 92, "therefore"),                 // for char '\' using  92 as base10 equivilant (for C# source). Spec has 0134 octal.              ( 92,"therefore")         <=> (0134, "therefore")      ,
            (113, "theta"),                     // for char 'q' using 113 as base10 equivilant (for C# source). Spec has 0161 octal.              (113,"theta")             <=> (0161, "theta")          ,
            ( 74, "theta1"),                    // for char 'J' using  74 as base10 equivilant (for C# source). Spec has 0112 octal.              ( 74,"theta1")            <=> (0112, "theta1")         ,
            ( 51, "three"),                     // for char '3' using  51 as base10 equivilant (for C# source). Spec has 0063 octal.              ( 51,"three")             <=> (0063, "three")          ,
            (228, "trademarksans"),             // for char 'ä' using 228 as base10 equivilant (for C# source). Spec has 0344 octal.              (228,"trademarksans")     <=> (0344, "trademarksans")  ,
            (212, "trademarkserif"),            // for char 'Ô' using 212 as base10 equivilant (for C# source). Spec has 0324 octal.              (212,"trademarkserif")    <=> (0324, "trademarkserif") ,
            ( 50, "two"),                       // for char '2' using  50 as base10 equivilant (for C# source). Spec has 0062 octal.              ( 50,"two")               <=> (0062, "two")            ,
            ( 95, "underscore"),                // for char '_' using  95 as base10 equivilant (for C# source). Spec has 0137 octal.              ( 95,"underscore")        <=> (0137, "underscore")     ,
            (200, "union"),                     // for char 'È' using 200 as base10 equivilant (for C# source). Spec has 0310 octal.              (200,"union")             <=> (0310, "union")          ,
            ( 34, "universal"),                 // for char '"' using  34 as base10 equivilant (for C# source). Spec has 0042 octal.              ( 34,"universal")         <=> (0042, "universal")      ,
            (117, "upsilon"),                   // for char 'u' using 117 as base10 equivilant (for C# source). Spec has 0165 octal.              (117,"upsilon")           <=> (0165, "upsilon")        ,
            (195, "weierstrass"),               // for char 'Ã' using 195 as base10 equivilant (for C# source). Spec has 0303 octal.              (195,"weierstrass")       <=> (0303, "weierstrass")    ,
            (120, "xi"),                        // for char 'x' using 120 as base10 equivilant (for C# source). Spec has 0170 octal.              (120,"xi")                <=> (0170, "xi")             ,
            ( 48, "zero"),                      // for char '0' using  48 as base10 equivilant (for C# source). Spec has 0060 octal.              ( 48,"zero")              <=> (0060, "zero")           ,
            (122, "zeta")                       // for char 'z' using 122 as base10 equivilant (for C# source). Spec has 0172 octal.              (122,"zeta")              <=> (0172, "zeta")           
        };


        private static readonly (int, int)[] UnicodeEquivilants = {
            (0x391,  65),                       // Greek Capital Letter Alpha
        };

        /// <summary>
        /// Single instance of this encoding.
        /// </summary>
        public static SymbolEncoding Instance { get; } = new SymbolEncoding();

        /// <inheritdoc />
        public override string EncodingName => "SymbolEncoding";

        private SymbolEncoding()
        {
            foreach ((var code, var name) in EncodingTable)
            {
                // Note: code from source is already base 10 no need to use OctalHelpers.FromOctalInt
                Add(code, name);
            }
        }
    }
}
