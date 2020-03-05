//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using BurgdorfStatistics.DataModel.Profiles;
//using Newtonsoft.Json;
//using Xunit;

//namespace BurgdorfStatistics._05_PresentVisualizer {
//    public class ExportRewriter {
//        [Fact]
//        public void RewriteFirstExport()
//        {
//            using (var sr = new StreamReader(@"firstExport.json")) {
//                var s = sr.ReadToEnd();
//                var entries = JsonConvert.DeserializeObject<List<ExportEntry>>(s);
//                sr.Close();
//                using (var sw = new StreamWriter(@"firstExport.csv")) {
//                    foreach (var exportEntry in entries) {
//                        var s2 = exportEntry.GebäudeObjektIDs + ";" + exportEntry.FamilySize + ";" + exportEntry.HouseGuid + ";" + exportEntry.YearlyElectricityUse;
//                        sw.WriteLine(s2);
//                    }

//                    sw.Close();
//                }
//            }
//        }

//        /*
//        private class Profile
//        {
//            public Profile([NotNull] string name)
//            {
//                Name = name;
//            }

//            [NotNull]
//            public string Name { get; }
//            //public List<string> Entries { get; } = new List<string>();
//            [NotNull]
//            public List<double> Values { get; } = new List<double>();
//        }*/
//        [Fact]
//        public void RewriteFirstExportJson()
//        {
//            using (var sr = new StreamReader(@"adaptricity.csv")) {
//                var profs = new List<Profile>();
//                while (!sr.EndOfStream) {
//                    var s = sr.ReadLine();
//                    if (s == null) {
//                        throw new Exception("line was null");
//                    }

//                    var arr = s.Split(';');
//                    var values = new List<double>();
//                    for (var i = 1; i < arr.Length; i++) {
//                        if (!string.IsNullOrWhiteSpace(arr[i])) {
//                            var d = Convert.ToDouble(arr[i]) / 13;
//                            values.Add(d);
//                        }
//                    }

//                    var p = new Profile(arr[0], values.AsReadOnly(), Power);
//                    profs.Add(p);
//                }

//                sr.Close();
//                {
//                    using (var sw = new StreamWriter(@"profiles1.csv")) {
//                        var header = "";
//                        var builder = new StringBuilder();
//                        builder.Append(header);

//                        foreach (var profile in profs) {
//                            builder.Append(profile.Name + ";");
//                        }

//                        header = builder.ToString();

//                        sw.WriteLine(header);
//                        var length = profs.Max(x => x.Values.Count);
//                        for (var i = 1; i < length; i++) {
//                            var l = "";
//                            var builder1 = new StringBuilder();
//                            builder1.Append(l);
//                            foreach (var prof in profs) {
//                                builder1.Append(prof.Values[i] + ";");
//                            }

//                            l = builder1.ToString();

//                            sw.WriteLine(l);
//                        }

//                        sw.Close();
//                    }
//                }
//                {
//                    using (var sw2 = new StreamWriter(@"profiles2.csv")) {
//                        foreach (var profile in profs) {
//                            var l = new StringBuilder();
//                            l.Append(profile.Name + ";");
//                            for (var i = 1; i < profile.Values.Count; i++) {
//                                l.Append(Math.Round(profile.Values[i], 1) + ";");
//                            }

//                            sw2.WriteLine(l);
//                        }

//                        sw2.Close();
//                    }
//                }
//            }
//        }
//    }
//}