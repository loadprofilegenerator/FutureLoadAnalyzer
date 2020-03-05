using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data.DataModel.Dst;
using JetBrains.Annotations;

namespace BurgdorfStatistics._02_Komplexes {
    // ReSharper disable once InconsistentNaming
    public class C_BuildComplexEgids : RunableWithBenchmark {
        public C_BuildComplexEgids([NotNull] ServiceRepository services)
            : base(nameof(C_BuildComplexEgids), Stage.Complexes, 3, services, true)
        {
        }

        protected override void RunActualProcess()
        {
            var db = SqlConnection.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice).Database;
            var komplexe = db.Fetch<BuildingComplex>();
            SqlConnection.RecreateTable<BuildingComplexStandorte>(Stage.Complexes, Constants.PresentSlice);
            db.BeginTransaction();
            Log(MessageType.Info, "Making ComplexStandortLookupTable and fix complex Names");
            var noComplexNameWasSet = 0;
            var adressComplexName = 0;
            var totalStandorteSet = 0;
            foreach (var complex in komplexe) {
                if (complex.Adresses.Count > 0) {
                    complex.ComplexName = complex.Adresses[0];
                    adressComplexName++;
                }
                else {
                    complex.ComplexName = "EGID" + complex.EGids[0];
                    noComplexNameWasSet++;
                }

                db.Save(complex);
                foreach (var s in complex.ObjektStandorte) {
                    var bce = new BuildingComplexStandorte {
                        ComplexID = complex.ComplexID,
                        Standort = s,
                        ComplexName = complex.ComplexName
                    };
                    totalStandorteSet++;
                    db.Save(bce);
                }
            }

            Log(MessageType.Info, "Used Egid Complex name for " + noComplexNameWasSet);
            Log(MessageType.Info, "Used Adress Complex name for " + adressComplexName);
            Log(MessageType.Info, "Total Standorte Set: " + totalStandorteSet);
            db.CompleteTransaction();
        }
    }
}