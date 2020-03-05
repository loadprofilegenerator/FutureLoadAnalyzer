using System;
using System.Collections.Generic;
using System.Diagnostics;
using Common.Config;
using Common.Steps;
using Data.DataModel.Export;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders {
    public abstract class BaseProvider :BasicLoggable {
        [NotNull]
        [ItemNotNull]
        public List<string> DevelopmentStatus { get; } = new List<string>();
        [NotNull] private readonly Stopwatch _sw = new Stopwatch();

        protected BaseProvider([NotNull] string name, [NotNull] ServiceRepository services, [NotNull] ScenarioSliceParameters slice):base(services.Logger,Stage.ProfileGeneration,name)
        {
            Services = services;
            Slice = slice;
        }
        [NotNull]
        public ScenarioSliceParameters Slice { get; }

        [NotNull]
        public ServiceRepository Services { get; }

        public TimeSpan Elapsed => _sw.Elapsed;
        [CanBeNull]
        protected abstract Prosumer ProvidePrivateProfile([NotNull] ProviderParameterDto ppdto);

        public virtual void DoFinishCheck()
        {

        }
        [CanBeNull]
        public Prosumer ProvideProfile([NotNull] ProviderParameterDto parameters)
        {
            _sw.Start();
            var p = ProvidePrivateProfile(parameters);
            _sw.Stop();
            return p;
        }

    }
}
