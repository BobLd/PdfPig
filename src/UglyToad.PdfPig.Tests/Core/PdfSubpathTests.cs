﻿namespace UglyToad.PdfPig.Tests.Core
{
    using UglyToad.PdfPig.Core;

    public class PdfSubpathTests
    {
        private static readonly DoubleComparer DoubleComparer = new DoubleComparer(0.001);
        private static readonly DoubleComparer PreciseDoubleComparer = new DoubleComparer(0.000001);
        private static readonly PointComparer PointComparer = new PointComparer(DoubleComparer);

        #region data
        public static IEnumerable<object[]> IsCounterClockwiseData => new[]
        {
            new object[]
            {
                new double[][]
                {
                    new double[] { 608.2936484002989, 443.7364349595423 },
                    new double[] { 469.00632489296777, 404.71981899260777 },
                    new double[] { 483.8487638943515, 201.39815474227552 },
                    new double[] { 845.1343222879254, 246.30758604338166 },
                    new double[] { 669.8821855062253, 274.5361321650157 },
                    new double[] { 383.8095675539657, 420.3011458295175 },
                    new double[] { 617.0547666353555, 470.2204418785535 },
                    new double[] { 82.84931200865586, 190.39579480403447 },
                    new double[] { 673.2256798368375, 143.87902474779725 },
                    new double[] { 949.3940062983974, 179.69479633397967 },
                },
                true
            },
            new object[]
            {
                new double[][]
                {
                    new double[] { 476.81554482073693, 522.9354794900999 },
                    new double[] { 63.40044015386304, 503.87115876734714 },
                    new double[] { 77.50470654929364, 415.2517513466559 },
                    new double[] { 602.6416506984787, 531.5755989995912 },
                    new double[] { 945.3123609415696, 48.74844122553201 },
                    new double[] { 172.32413098758403, 808.3421273466664 },
                    new double[] { 380.6337837279111, 170.87787102110119 },
                    new double[] { 180.78366414289437, 905.2800627102379 },
                    new double[] { 458.49131752678596, 446.106837663974 },
                    new double[] { 43.80428397895675, 829.9973170650543 },
                },
                false
            },
            new object[]
            {
                new double[][]
                {
                    new double[] { 448.88325033327493, 370.5834431306133 },
                    new double[] { 290.2885214430252, 176.84180903787083 },
                    new double[] { 40.60059207814548, 963.0599449326335 },
                    new double[] { 768.2277909162901, 678.645743419389 },
                    new double[] { 266.7566199776823, 859.9652627289186 },
                    new double[] { 822.9056472694879, 896.5689372393169 },
                    new double[] { 826.7473223443114, 783.5186118485211 },
                    new double[] { 78.0468033614834, 546.0495564957905 },
                    new double[] { 8.633607480775796, 507.23390359727404 },
                    new double[] { 997.9816230374784, 677.523944467832 },
                },
                false
            },
            new object[]
            {
                new double[][]
                {
                    new double[] { 202.3230469547055, 161.49436155685348 },
                    new double[] { 251.94735947881563, 829.171609104472 },
                    new double[] { 463.66491786957187, 603.0993420287084 },
                    new double[] { 114.92356595253949, 292.41466702399686 },
                    new double[] { 536.8333279246178, 857.315453133446 },
                    new double[] { 50.18788932650664, 412.80218436070714 },
                    new double[] { 982.6133353084964, 504.0373992968087 },
                    new double[] { 56.25272820087157, 274.3031489006598 },
                    new double[] { 54.576861186808046, 910.175233498483 },
                    new double[] { 185.53060179976433, 267.0344440870054 },
                },
                false
            },
            new object[]
            {
                new double[][]
                {
                    new double[] { 679.5314500935101, 975.2870721022717 },
                    new double[] { 940.2289123512929, 629.7357627057702 },
                    new double[] { 62.88788255055777, 865.7674555241166 },
                    new double[] { 636.7351821393295, 106.65141912825472 },
                    new double[] { 423.7483532487842, 146.9445935128988 },
                },
                true
            },
            new object[]
            {
                new double[][]
                {
                    new double[] { 469.0138471986609, 913.7290426992142 },
                    new double[] { 476.32011175077093, 968.3035665571127 },
                    new double[] { 924.0627086891274, 184.71556821090118 },
                    new double[] { 789.4456512910025, 890.457230293438 },
                    new double[] { 316.27902593875115, 636.2624035724327 },
                },
                true
            },
            new object[]
            {
                new double[][]
                {
                    new double[] { 612.4362858549162, 555.0290746323946 },
                    new double[] { 503.10430745138234, 212.1385472106042 },
                    new double[] { 580.1110476087689, 154.75514569362048 },
                    new double[] { 293.28021215928914, 381.1349830542828 },
                    new double[] { 421.5948472378218, 648.0362979151611 },
                },
                false
            },
            new object[]
            {
                new double[][]
                {
                    new double[] { 738.929126646827, 696.3938827279978 },
                    new double[] { 718.9684179351378, 84.09343329857255 },
                    new double[] { 769.9140607300079, 323.2708431140446 },
                    new double[] { 91.24558704820618, 91.52777989400995 },
                    new double[] { 974.908165685702, 922.7129528238121 },
                },
                false
            },
            new object[]
            {
                new double[][]
                {
                    new double[] { 110.82728900219941, 544.9625282980728 },
                    new double[] { 80.96370530431307, 325.82042616045584 },
                    new double[] { 980.8549877160835, 966.8071943781295 },
                },
                true
            },
            new object[]
            {
                new double[][]
                {
                    new double[] { 672.2666674887735, 878.3496961250557 },
                    new double[] { 687.8731912987437, 492.89628099530967 },
                    new double[] { 342.2807868820875, 190.51029096090977 },
                },
                false
            },
            new object[]
            {
                new double[][]
                {
                    new double[] { 456.34244275013293, 715.027084095151 },
                    new double[] { 87.16013459626792, 85.86794530557663 },
                    new double[] { 392.3433695334779, 115.17024985244673 },
                },
                true
            },
            new object[]
            {
                new double[][]
                {
                    new double[] { 609.715156417142, 936.6297325222877 },
                    new double[] { 262.4749724314196, 418.82943310284946 },
                    new double[] { 186.04244381418178, 245.0017595790035 },
                    new double[] { 86.16854031983401, 784.9098699808084 },
                    new double[] { 190.7897764357055, 529.2389034541217 },
                    new double[] { 131.90153296283668, 861.3218240235083 },
                    new double[] { 478.18546444147034, 351.86240954243067 },
                },
                false
            },
            new object[]
            {
                new double[][]
                {
                    new double[] { 998.1380008321523, 369.126402333843 },
                    new double[] { 995.1461171751115, 92.12176165214159 },
                    new double[] { 134.6949234797923, 965.3426414055724 },
                    new double[] { 150.2279696920602, 557.8672980201856 },
                    new double[] { 264.1333959156217, 727.6189954785406 },
                    new double[] { 697.344968806681, 355.5562649644036 },
                    new double[] { 408.1805101182644, 735.1535225969927 },
                    new double[] { 856.8454177610708, 713.3757514070634 },
                    new double[] { 215.32070134150473, 671.3763274502109 },
                    new double[] { 297.6881146689045, 372.5209263318404 },
                    new double[] { 371.6300542174854, 368.8432693909592 },
                    new double[] { 468.9928839198324, 579.7738004272005 },
                    new double[] { 569.8541921952922, 815.2889286061185 },
                    new double[] { 214.22482061958138, 715.6768470398234 },
                    new double[] { 450.53520973871696, 790.4887765558984 },
                },
                true
            },
            new object[]
            {
                new double[][]
                {
                    new double[] { 295.0908180474644, 566.7416231974277 },
                    new double[] { 4.728301052427275, 379.3002022862234 },
                    new double[] { 493.54792742587404, 250.09348144002652 },
                    new double[] { 674.3762868759716, 818.9960008389934 },
                    new double[] { 858.4970954636957, 305.7395859968263 },
                    new double[] { 777.5251735977608, 438.1345410933981 },
                    new double[] { 911.6417260033118, 538.7515829500079 },
                    new double[] { 805.0963852060096, 54.1399548426369 },
                    new double[] { 99.16068977219861, 363.36750912897463 },
                    new double[] { 284.7650349868293, 454.46212704874654 },
                    new double[] { 172.3357516434867, 340.1266334767119 },
                    new double[] { 284.43474628447564, 12.499144373366189 },
                    new double[] { 624.5733368824075, 127.44172005005039 },
                    new double[] { 5.7714822312397995, 166.56582851146285 },
                    new double[] { 653.9413624059443, 745.1381254793514 },
                },
                false
            },
            new object[]
            {
                new double[][]
                {
                    new double[] { 252.28085355588536, 27.07984983311251 },
                    new double[] { 180.82517117640938, 765.2327558030423 },
                    new double[] { 671.9968485425226, 507.9164379993931 },
                    new double[] { 56.68070764991029, 808.1142930407553 },
                    new double[] { 529.30514264428, 204.06563347160412 },
                    new double[] { 173.52820154484394, 202.71847416108778 },
                    new double[] { 934.4779107607807, 80.61524226445071 },
                    new double[] { 81.52311650903476, 510.51203519093923 },
                    new double[] { 31.42445056734644, 611.207152815212 },
                    new double[] { 153.9546478049284, 234.84309437723294 },
                    new double[] { 368.82584256836105, 293.2101133289825 },
                    new double[] { 563.5993709012033, 391.0166612894065 },
                    new double[] { 784.9793771759037, 839.8703301169597 },
                    new double[] { 441.65063169558005, 550.7050683993313 },
                    new double[] { 777.2385155293424, 425.72505071098067 },
                    new double[] { 806.7305891748078, 523.8780115516616 },
                    new double[] { 735.3453861114214, 240.82150641022128 },
                    new double[] { 373.42641138089516, 43.723078319251925 },
                    new double[] { 714.3328835877788, 980.8782292030259 },
                    new double[] { 134.4651452707837, 57.284425240566314 },
                },
                true
            },
            new object[]
            {
                new double[][]
                {
                    new double[] { 301.0679546895252, 650.3225921061893 },
                    new double[] { 290.1375488377912, 16.494859386740224 },
                    new double[] { 309.31828424146113, 257.71597272888755 },
                    new double[] { 867.5667553028213, 477.2845409775075 },
                    new double[] { 363.0725145809166, 206.71395104215972 },
                    new double[] { 732.0381813955951, 873.3389316199567 },
                    new double[] { 284.55939014974786, 91.5417571502125 },
                    new double[] { 898.9912165117968, 847.3561035127337 },
                    new double[] { 880.0841450440903, 806.7964960575931 },
                    new double[] { 9.95618366785167, 476.4558446489462 },
                    new double[] { 924.0442454201044, 986.961222899876 },
                    new double[] { 117.40839004053016, 520.3687806803076 },
                    new double[] { 381.4993315637265, 969.5410892094217 },
                    new double[] { 558.2276605593191, 340.4110418413754 },
                    new double[] { 939.8914476623564, 837.6812903087314 },
                    new double[] { 540.8533259213762, 939.7510124536617 },
                    new double[] { 978.328755063642, 500.4646493692516 },
                    new double[] { 202.57823837145384, 231.49256822844012 },
                    new double[] { 961.3159014014341, 620.479369911658 },
                    new double[] { 931.8112476511618, 265.39888437468096 },
                },
                false
            },
            new object[]
            {
                new double[][]
                {
                    new double[] { 14.800842191870167, 663.4465835403801 },
                    new double[] { 516.3876681397454, 929.5726895851639 },
                    new double[] { 311.21672910267085, 842.2729917184947 },
                    new double[] { 10.20033390715036, 739.8176761382576 },
                    new double[] { 910.5791572204455, 852.7007728209953 },
                    new double[] { 117.70167173750212, 774.168763188029 },
                    new double[] { 131.41818377648406, 563.3594775177128 },
                    new double[] { 532.6721177776084, 326.64892988121017 },
                    new double[] { 512.9836236452278, 222.81685908731262 },
                    new double[] { 177.71691138513657, 997.7057864580166 },
                    new double[] { 744.5922510937745, 37.23057503108662 },
                    new double[] { 244.58705321204442, 746.5421401054565 },
                    new double[] { 469.8814759288329, 324.8095505430748 },
                    new double[] { 882.3267351862916, 893.2258892268225 },
                    new double[] { 652.2537370603092, 749.3269189444026 },
                    new double[] { 227.03184778443008, 53.7857262269682 },
                    new double[] { 695.1811646478141, 581.1697397748696 },
                    new double[] { 779.5395030844411, 931.9625121354823 },
                    new double[] { 953.4574481743947, 726.5794548409784 },
                    new double[] { 457.1901780763846, 379.91509445240604 },
                    new double[] { 59.9526353089671, 712.7218534878671 },
                    new double[] { 757.9917245805595, 308.76255751763483 },
                    new double[] { 802.935017652651, 7.310504059650058 },
                    new double[] { 390.7851583285599, 548.0996955569092 },
                    new double[] { 986.6861310475554, 96.1372470382974 },
                    new double[] { 248.67962604718608, 712.6772178997445 },
                    new double[] { 721.2169570376802, 711.8963747715053 },
                    new double[] { 813.7975354221466, 169.5599687998417 },
                    new double[] { 227.44880133364342, 213.4695903108651 },
                    new double[] { 334.0645366250655, 98.51267879376135 },
                    new double[] { 410.01118062567645, 415.1469868187319 },
                    new double[] { 975.2597449472323, 584.1999901687817 },
                    new double[] { 284.17636484767064, 308.1456125413913 },
                    new double[] { 549.4609001915817, 732.7566313929179 },
                    new double[] { 258.20033278898626, 464.2207916601894 },
                    new double[] { 822.2058392179248, 425.77538489816624 },
                    new double[] { 53.92209272311344, 214.37531225880446 },
                    new double[] { 233.41159326873495, 876.3928796409385 },
                    new double[] { 663.7450191521767, 409.6678747918601 },
                    new double[] { 362.07360046644754, 885.1306707431294 },
                },
                false
            },
            new object[]
            {
                new double[][]
                {
                    new double[] { 247.96337577052384, 136.66209630721326 },
                    new double[] { 478.2707004172929, 22.274894624685793 },
                    new double[] { 217.00215175259962, 981.9707447857631 },
                    new double[] { 588.8723964552604, 861.8712721273173 },
                    new double[] { 838.9884266775456, 161.40348415213023 },
                    new double[] { 213.29466808500263, 759.5732068488198 },
                    new double[] { 296.76309957310684, 876.4301536116592 },
                    new double[] { 533.3617136845685, 604.4622315445964 },
                    new double[] { 64.39187482363285, 769.8836839579526 },
                    new double[] { 124.4631466581777, 400.22790162204836 },
                    new double[] { 543.4564693661497, 585.6408398219498 },
                    new double[] { 404.4530538817104, 410.7333006150529 },
                    new double[] { 183.8455617244611, 990.550354195785 },
                    new double[] { 334.13571586352174, 566.9629441079231 },
                    new double[] { 793.700246201893, 746.4573515394505 },
                    new double[] { 235.50169268775323, 51.56201625517009 },
                    new double[] { 654.7762510820518, 185.35542109807602 },
                    new double[] { 71.29997690605295, 699.1669668920807 },
                    new double[] { 185.93300559525272, 706.1761972338285 },
                    new double[] { 502.37437524232155, 830.1960118695748 },
                    new double[] { 319.0121675170979, 260.79970444134216 },
                    new double[] { 745.9902885310859, 993.8367610175973 },
                    new double[] { 702.6511153452411, 161.50394876924133 },
                    new double[] { 518.7458473337373, 84.21979582051198 },
                    new double[] { 847.6625835318309, 602.7548366411913 },
                    new double[] { 142.76659134603898, 655.9821295634375 },
                    new double[] { 277.0763022298303, 200.13096904929506 },
                    new double[] { 779.6680869132609, 461.1049676031593 },
                    new double[] { 195.97819762196266, 682.7342586088021 },
                    new double[] { 679.061887553984, 386.20645507201436 },
                    new double[] { 983.1610423183647, 827.1859473667223 },
                    new double[] { 462.56670293494415, 985.1700550010869 },
                    new double[] { 967.8904617423967, 314.9433481360241 },
                    new double[] { 794.6296697506857, 231.126003195803 },
                    new double[] { 672.7410058829398, 604.3454688392936 },
                    new double[] { 364.1965415099363, 291.241763019921 },
                    new double[] { 770.5891329881562, 201.2683717205912 },
                    new double[] { 555.712116079188, 550.2566443651252 },
                    new double[] { 507.4317366076063, 680.2154136907125 },
                    new double[] { 192.31799951637174, 480.69326858498573 },
                },
                true
            },
        };
        #endregion

        [Theory]
        [MemberData(nameof(IsCounterClockwiseData))]
        public void IsCounterClockwise(double[][] source, bool expected)
        {
            PdfSubpath pdfPath = new PdfSubpath();
            pdfPath.MoveTo(source[0][0], source[0][1]);

            for (int i = 1; i < source.Length; i++)
            {
                var point = source[i];
                pdfPath.LineTo(point[0], point[1]);
            }
            pdfPath.LineTo(source[0][0], source[0][1]); // close path

            Assert.Equal(expected, pdfPath.IsCounterClockwise);
        }
    }
}
