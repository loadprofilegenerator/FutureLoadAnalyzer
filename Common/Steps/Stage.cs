namespace Common.Steps {
    public enum Stage {
        Unknown,
        Raw = 1,
        Complexes = 2,
        ComplexEnergyData = 3,
        Houses = 4,
        ScenarioCreation = 5,
        ScenarioVisualisation = 6,
        RawProfileVisualisation = 7,
        ProfileGeneration = 8,
        ProfileAnalysis = 9,
        CrossSliceProfileAnalysis = 10,
        Preparation = 100,
        Plotting = 200,
        Reporting = 300,
        Testing = 1000,
        OtherWork = 2000
    }
}