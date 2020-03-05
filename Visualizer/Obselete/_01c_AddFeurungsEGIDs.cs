/*BurgdorfStatistics.DataModel.Src;
using NUnit.Framework;

namespace BurgdorfStatistics._01_Analysis
{
    // ReSharper disable once IdentifierWordIsNotInDictionary
    // ReSharper disable once InconsistentNaming
    internal class _01c_AddFeurungsEGIDs
    {


        [Test]
        public void Run()
        {
            var db = Helpers.Getdatabase();
            //var objektStandortZuEgids = db.Fetch<ObjektStandortZuEgid>();
            //var feurungsstätten = db.Fetch<FeuerungsStaette>();
            var gwradressen = db.Fetch<GWRAdresse>();
            //List<AdresseZuEgid> azes = new List<AdresseZuEgid>();
            foreach (GWRAdresse gwrAdresse in gwradressen) {
                AdresseZuEgid aze = new AdresseZuEgid();
                aze.Adresse = gwrAdresse.Strassenbezeichnung_DSTR + " " + gwrAdresse.EingangsnummerGebaeude_DEINR;
                aze.Adresse = Helpers.CleanAdressString(aze.Adresse);
                aze.EGID = gwrAdresse.EidgGebaeudeidentifikator_EGID;
                aze.Plz = gwrAdresse.Postleitzahl_DPLZ4 + " Burgdorf";
                db.Save(aze);
              //  azes.Add(aze);
            }

            //foreach (FeuerungsStaette staette in feurungsstätten) {
                //string adresse = staette.Strasse + " " + staette.Hausnummer;
                //var alreadyCreatedEntries = azes.Where(x => x.Adresse == adresse).ToList();
                //if (alreadyCreatedEntries == 0) {
                    //StandorteWithoutEGID = f
                //}
            //}
        }
    }
}
*/

