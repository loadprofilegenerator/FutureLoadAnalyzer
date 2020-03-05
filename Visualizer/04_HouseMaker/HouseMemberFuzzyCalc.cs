//using FLS;
//using FLS.Rules;

using System;
using System.Collections.Generic;
using System.IO;
using AI.Fuzzy.Library;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Xunit;

//using LinguisticVariable = FLS.LinguisticVariable;

//using LinguisticVariable = Accord.Fuzzy.LinguisticVariable;

//using LinguisticVariable = FLS.LinguisticVariable;
//using Rule = FLS.Rules.Rule;

namespace BurgdorfStatistics._04_HouseMaker {
    public class HouseMemberFuzzyCalc {
        [NotNull] private readonly MamdaniFuzzySystem _fsTips = new MamdaniFuzzySystem();
        [NotNull] private readonly Logging.Logger _logger;

        public HouseMemberFuzzyCalc([NotNull] Logging.Logger logger)
        {
            _logger = logger;
            //
            // Create empty fuzzy system
            //

            //
            // Create input variables for the system
            //
            var energy = new FuzzyVariable("energy", 0.0, 10000.0);
            //// finetune these parameters some more / automate the fine tuning
            energy.Terms.Add(new FuzzyTerm("zeroenergy", new TriangularMembershipFunction(-5.0, 0.0, 1000)));
            energy.Terms.Add(new FuzzyTerm("onethousand", new TrapezoidMembershipFunction(1000, 1200, 1300, 1800)));
            energy.Terms.Add(new FuzzyTerm("twothousand", new TrapezoidMembershipFunction(1500, 2200, 2500, 3500)));
            energy.Terms.Add(new FuzzyTerm("threethousand", new TrapezoidMembershipFunction(3300, 4000, 4700, 5000)));
            energy.Terms.Add(new FuzzyTerm("fourthousand", new TrapezoidMembershipFunction(4500, 5000, 6000, 6500)));
            energy.Terms.Add(new FuzzyTerm("fivethousand", new TriangularMembershipFunction(6500, 7500, 8000)));
            energy.Terms.Add(new FuzzyTerm("sixthousand", new TriangularMembershipFunction(7000, 8000, 10000)));
            _fsTips.Input.Add(energy);

            /*FuzzyVariable fvFood = new FuzzyVariable("food", 0.0, 10.0);
            fvFood.Terms.Add(new FuzzyTerm("rancid", new TrapezoidMembershipFunction(0.0, 0.0, 1.0, 3.0)));
            fvFood.Terms.Add(new FuzzyTerm("delicious", new TrapezoidMembershipFunction(7.0, 9.0, 10.0, 10.0)));
            fsTips.Input.Add(fvFood);*/

            //
            // Create output variables for the system
            //
            var people = new FuzzyVariable("people", 0.0, 10);
            people.Terms.Add(new FuzzyTerm("zero", new TriangularMembershipFunction(0.0, 0.2, 0.5)));
            people.Terms.Add(new FuzzyTerm("oneperson", new TriangularMembershipFunction(0.0, 1, 2)));
            people.Terms.Add(new FuzzyTerm("twopersons", new TriangularMembershipFunction(1, 2, 3)));
            people.Terms.Add(new FuzzyTerm("threepersons", new TriangularMembershipFunction(2, 3, 4)));
            people.Terms.Add(new FuzzyTerm("fourpersons", new TriangularMembershipFunction(3, 4, 5)));
            people.Terms.Add(new FuzzyTerm("fivepersons", new TriangularMembershipFunction(4, 5, 6)));
            people.Terms.Add(new FuzzyTerm("sixpersons", new TriangularMembershipFunction(5, 6, 7)));
            _fsTips.Output.Add(people);

            //
            // Create three fuzzy rules
            //
            //  try
            //            {// or (food is rancid)
            _fsTips.Rules.Add(_fsTips.ParseRule("if (energy is zeroenergy ) then people is zero"));
            _fsTips.Rules.Add(_fsTips.ParseRule("if (energy is onethousand) then people is oneperson"));
            _fsTips.Rules.Add(_fsTips.ParseRule("if (energy is twothousand) then (people is twopersons)"));
            _fsTips.Rules.Add(_fsTips.ParseRule("if (energy is threethousand) then (people is threepersons)"));
            _fsTips.Rules.Add(_fsTips.ParseRule("if (energy is fourthousand) then (people is fourpersons)"));
            _fsTips.Rules.Add(_fsTips.ParseRule("if (energy is fivethousand) then (people is fivepersons)"));
            _fsTips.Rules.Add(_fsTips.ParseRule("if (energy is sixthousand) then (people is sixpersons)"));
        }

        [Fact]
        public void RunVarTuning()
        {
            List<double> energyuses;
            using (var sr = new StreamReader("v:\\energyuses.json")) {
                energyuses = JsonConvert.DeserializeObject<List<double>>(sr.ReadToEnd());
            }

            var peopleCounts = new Dictionary<int, int>();
            for (var i = 0; i < 10; i++) {
                peopleCounts.Add(i, 0);
            }

            var sum = 0;
            foreach (var energy in energyuses) {
                var people = GetPeopleCountForEnergy(energy);
                peopleCounts[people]++;
                sum += people;
            }

            foreach (var pair in peopleCounts) {
                _logger.Info(pair.Key + ": " + pair.Value);
            }

            _logger.Info("Total: " + sum);
        }

        public int GetPeopleCountForEnergy(double energyval)
        {
            if (energyval > 10000) {
                // return heating energy?
                return 5;
            }

            var energy = _fsTips.InputByName("energy");
            //FuzzyVariable fvFood = fsTips.InputByName("food");
            var people = _fsTips.OutputByName("people");

            //
            // Associate input values with input variables
            //

            var inputValues = new Dictionary<FuzzyVariable, double> {{energy, energyval}};

            var result = _fsTips.Calculate(inputValues);

            //_logger.Info(i + ": " + result[people].ToString("f1"));
            var resultval = Math.Round(result[people]);
            if (double.IsNaN(resultval)) {
                return 5;
            }

            return (int)resultval;
        }

        [Fact]
        public void Fuzzy3()
        {
            var energy = _fsTips.InputByName("energy");
            //FuzzyVariable fvFood = fsTips.InputByName("food");
            var people = _fsTips.OutputByName("people");

            //
            // Associate input values with input variables
            //

            for (var i = 0; i < 5000; i += 100) {
                var inputValues = new Dictionary<FuzzyVariable, double> {{energy, i}};

                var result = _fsTips.Calculate(inputValues);

                _logger.Info(i + ": " + result[people].ToString("f1") + " - " + GetPeopleCountForEnergy(i));
            }
        }
    }
}