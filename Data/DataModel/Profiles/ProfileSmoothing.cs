#define ExtensiveChecking
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Common;
using JetBrains.Annotations;

namespace Data.DataModel.Profiles {
    public static class ProfileSmoothing {
        [NotNull]
        public static Profile FindBestPowerReductionRatio([NotNull] Profile profile,
                                                          double storagesize,
                                                          [NotNull] out Profile storageProfile,
                                                          out double reductionFactor,
                                                          double dynamicCappingFactor)
        {
            double maxiumPower = profile.Values.Max();
            double minimumPower = profile.Values.Min();
            bool hasReachedLimit = false;

            double factor = 0.99;
            double lastGoodFactor = 1;
            //for the storage profile
            Profile newProfile;
            double increase = 0.1;
            while (!hasReachedLimit) {
                newProfile = UsePeakShaving(profile, storagesize, false, out storageProfile, factor, out _, out _, dynamicCappingFactor);
                var newMax = newProfile.Values.Max();
                var newMin = newProfile.Values.Min();
                if (newMax < maxiumPower && newMin > minimumPower) {
                    lastGoodFactor = factor;
                    factor -= increase;
                    if (factor < 0) {
                        factor = increase;
                    }

                    maxiumPower = newMax;
                    minimumPower = newMin;
                }
                else {
                    increase = increase * 0.5;
                    factor = lastGoodFactor - increase;
                    if (factor > 1) {
                        factor = 1;
                    }

                    if (factor < 0) {
                        factor = increase;
                    }
                }

                if (increase < 0.0001) {
                    hasReachedLimit = true;
                }
            }

            if (lastGoodFactor < 0) {
                throw new FlaException("Negative factor: " + lastGoodFactor);
            }

            if (lastGoodFactor > 1) {
                throw new FlaException("increase instead of decrease");
            }

            reductionFactor = lastGoodFactor;
            newProfile = UsePeakShaving(profile, storagesize, false, out storageProfile, lastGoodFactor, out _, out _, dynamicCappingFactor);
            return newProfile;
        }

        [NotNull]
        public static Profile IntegrateStorage([NotNull] Profile profile,
                                               double maxStorageAmount,
                                               [NotNull] string newName,
                                               [NotNull] out Profile storageProfile)
        {
            double storage = 0;
            var resultValues = new List<double>();
            var storageValues = new List<double>();
            for (var i = 0; i < profile.Values.Count - 24; i++) {
                var currentVal = profile.Values[i];
                //load
                if (currentVal > 0) {
                    //if something is in storage, try to withdraw
                    if (storage > 0) {
                        var withdrawAmount = Math.Min(currentVal, storage);
                        storage -= withdrawAmount;
                        currentVal -= withdrawAmount;
                    }

                    //else nothing can be done
                }
                else {
                    //saving
                    if (storage < maxStorageAmount) {
                        var spaceLeft = maxStorageAmount - storage;
                        var storageAmount = Math.Min(spaceLeft, currentVal * -1); //einspeisung ist negativ
                        currentVal += storageAmount; //add the storage amount because it is negative
                        storage += storageAmount;
                    }

                    //nothing can be done
                }

                if (storage < 0) {
                    throw new Exception("Negative storage is wrong.");
                }

                resultValues.Add(currentVal);
                storageValues.Add(storage);
            }

            var resultProfile = new Profile(profile, resultValues.AsReadOnly()) {Name = newName};
            storageProfile = new Profile(profile, storageValues.AsReadOnly()) {Name = newName + "-EnergyStorage"};
            return resultProfile;
        }

        [NotNull]
        public static Profile IntegrateStorageWiFloatingTargetValue([NotNull] Profile profile,
                                                                    double maxStorageAmount,
                                                                    [NotNull] string newName,
                                                                    [NotNull] out Profile storageProfile,
                                                                    int timeStepsToConsider)
        {
            var storage = maxStorageAmount / 2;
            var resultValues = new List<double>();
            var storageValues = new List<double>();
            for (var i = 0; i < profile.Values.Count; i++) {
                double targetValue = GetTargetValue(profile.Values, timeStepsToConsider, i);
                var currentVal = profile.Values[i];
                //load
                if (currentVal > targetValue) {
                    //if something is in storage, try to withdraw
                    if (storage > 0) {
                        var withdrawAmount = Math.Min(currentVal - targetValue, storage);
                        storage -= withdrawAmount;
                        currentVal -= withdrawAmount;
                    }

                    //else nothing can be done
                }
                else {
                    //saving
                    if (storage < maxStorageAmount) {
                        var spaceLeft = maxStorageAmount - storage;
                        var diffToTarget = targetValue - currentVal;
                        var storageAmount = Math.Min(spaceLeft, diffToTarget); //einspeisung ist negativ
                        currentVal += storageAmount; //add the storage amount because it is negative
                        storage += storageAmount;
                    }

                    //nothing can be done
                }

                if (storage < 0) {
                    throw new Exception("Negative storage is wrong.");
                }

                if (storage > maxStorageAmount) {
                    throw new Exception("Over max storage is wrong.");
                }

                resultValues.Add(currentVal);
                storageValues.Add(storage);
            }

            var resultProfile = new Profile(profile, resultValues.AsReadOnly()) {Name = newName};
            storageProfile = new Profile(profile, storageValues.AsReadOnly()) {Name = newName + "-EnergyStorage"};
            return resultProfile;
        }

        [NotNull]
        public static Profile IntegrateStorageWithAdjustedCurrentValue([NotNull] Profile profile,
                                                                       double maxStorageAmount,
                                                                       [NotNull] string newName,
                                                                       [NotNull] out Profile storageProfile,
                                                                       double targetValue)
        {
            var storage = maxStorageAmount;
            var resultValues = new List<double>();
            var storageValues = new List<double>();
            for (var i = 0; i < profile.Values.Count - 24; i++) {
                var currentVal = profile.Values[i];
                //load
                if (currentVal > targetValue) {
                    //if something is in storage, try to withdraw
                    if (storage > 0) {
                        var withdrawAmount = Math.Min(currentVal - targetValue, storage);
                        storage -= withdrawAmount;
                        currentVal -= withdrawAmount;
                    }

                    //else nothing can be done
                }
                else {
                    //saving
                    if (storage < maxStorageAmount) {
                        var spaceLeft = maxStorageAmount - storage;
                        var diffToTarget = targetValue - currentVal;
                        var storageAmount = Math.Min(spaceLeft, diffToTarget); //einspeisung ist negativ
                        currentVal += storageAmount; //add the storage amount because it is negative
                        storage += storageAmount;
                    }

                    //nothing can be done
                }

                if (storage < 0) {
                    throw new Exception("Negative storage is wrong.");
                }

                if (storage > maxStorageAmount) {
                    throw new Exception("Over max storage is wrong.");
                }

                resultValues.Add(currentVal);
                storageValues.Add(storage);
            }

            var resultProfile = new Profile(profile, resultValues.AsReadOnly()) {Name = newName};
            storageProfile = new Profile(profile, storageValues.AsReadOnly())
                { Name = newName + "-EnergyStorage" };
            return resultProfile;
        }

        public static void ProcessOneTimestep(double storagesize,
                                              ref double currentVal,
                                              double lowerPowerLimit,
                                              double upperPowerLimit,
                                              ref double storage,
                                              double bufferTarget,
                                              double dynamicCappingFactor,
                                              double reductionFactor)
        {
            //charging
            if (currentVal < lowerPowerLimit) {
                //try to charge the storage
                if (dynamicCappingFactor < 1 && (reductionFactor > dynamicCappingFactor)) {
                    double chargingAmount = currentVal - lowerPowerLimit;
                    storage -= chargingAmount;
                    if (storage > storagesize) {
                        storage = storagesize;
                    }

                    currentVal -= chargingAmount;
#if ExtensiveChecking
                    CheckViolations(storagesize, currentVal, upperPowerLimit, storage, lowerPowerLimit);
#endif
                    return;
                }

                if (storage < storagesize) {
                    double chargingAmount = currentVal - lowerPowerLimit;
                    var spaceLeft = storagesize - storage;
                    var correctedChargingAmount = Math.Min(spaceLeft, Math.Abs(chargingAmount));
                    storage += correctedChargingAmount;
                    currentVal += correctedChargingAmount;
#if ExtensiveChecking
                    CheckViolations(storagesize, currentVal, upperPowerLimit, storage, lowerPowerLimit);
#endif
                    return;
                }
            }

            if (currentVal > upperPowerLimit && storage > 0) {
                //discharge
                var energyNeeded = currentVal - upperPowerLimit;
                var energyLeft = storage;
                var correctedEnergyRelease = Math.Min(energyLeft, energyNeeded);
                storage -= correctedEnergyRelease;
                currentVal -= correctedEnergyRelease;
#if ExtensiveChecking
                CheckViolations(storagesize, currentVal, upperPowerLimit, storage, lowerPowerLimit);
#endif
                // ReSharper disable once RedundantJumpStatement
                return;
            }

            //balance storage
            if (storage > bufferTarget && currentVal > lowerPowerLimit) {
                //discharge
                var maximumEnergyToDischarge = lowerPowerLimit - currentVal; // -500 - 50 = 550 max discharge
                var bufferDif = storage - bufferTarget; // 520 - 500 = 20
                var bufferMax = bufferTarget - 0; //500
                var proportinalBufferDiff = bufferDif / bufferMax; //20/500
                if (proportinalBufferDiff > 1) {
                    proportinalBufferDiff = 1;
                }

                if (proportinalBufferDiff < 0) {
                    proportinalBufferDiff = 0;
                }

                var energyToDischarge = maximumEnergyToDischarge * proportinalBufferDiff; // %
                var energyLeft = storage;
                var correctedEnergyRelease = Math.Min(energyLeft, energyToDischarge * -1);
                storage -= correctedEnergyRelease;
                currentVal -= correctedEnergyRelease;
#if ExtensiveChecking
                CheckViolations(storagesize, currentVal, upperPowerLimit, storage, lowerPowerLimit);
#endif
                return;
            }

            if (storage < bufferTarget && currentVal < upperPowerLimit) {
                //charge
                var maxEnergyToCharge = upperPowerLimit - currentVal; // +500 - 50 = 550 max charge

                var bufferDif = bufferTarget - storage; // 520 - 500 = 20
                var bufferMax = storagesize - bufferTarget; //1000-500
                var proportinalBufferDiff = bufferDif / bufferMax; //20/500
                if (proportinalBufferDiff > 1) {
                    proportinalBufferDiff = 1;
                }

                if (proportinalBufferDiff < 0) {
                    proportinalBufferDiff = 0;
                }

                var energyToCharge = maxEnergyToCharge * proportinalBufferDiff;
                var spaceLeft = storagesize - storage;
                var correctedChargingAmount = Math.Min(spaceLeft, Math.Abs(energyToCharge));
                storage += correctedChargingAmount;
                currentVal += correctedChargingAmount;
#if ExtensiveChecking
                CheckViolations(storagesize, currentVal, upperPowerLimit, storage, lowerPowerLimit);
#endif
                // ReSharper disable once RedundantJumpStatement
                return;
            }
        }

        [NotNull]
        public static Profile UsePeakShaving([NotNull] Profile profile,
                                             double storagesize,
                                             bool makeStorageProfile,
                                             [CanBeNull] out Profile storageProfile,
                                             double powerReductionFactor,
                                             [NotNull] out Profile temporaryTargetProfile,
                                             [NotNull] out Profile sum24HProfile,
                                             double dynamicCappingFactor)
        {
            if (powerReductionFactor > 1) {
                throw new FlaException("Power reduction > 1");
            }

            var valueArr = profile.Values.ToArray();
            int valueCount = valueArr.Length;
            var resultValues = new double[valueCount];
            var storageValues = new List<double>();
            var temporaryTargetValues = new List<double>();
            var sumNext24H = new List<double>();
            double maxPower = profile.Values.Max();
            double upperPowerLimit = maxPower * powerReductionFactor;
            double lowerPowerLimit = upperPowerLimit * -1;
            double minBuffer = storagesize * 0.10;
            double maxBuffer = storagesize * 0.7;
            const int timestepsToConsider = 96 * 4;
            double sumNext24Hv2 = 0;
            for (int j = 0; j < timestepsToConsider; j++) {
                sumNext24Hv2 += valueArr[j];
            }

            double storage;
            if (sumNext24Hv2 > 0) {
                storage = maxBuffer;
            }
            else {
                storage = minBuffer;
            }

            for (var i = 0; i < valueCount; i++) {
                var currentVal = valueArr[i];
                //sum next 24h v2
                sumNext24Hv2 -= valueArr[i];
                int indexToAdd = i + timestepsToConsider;
                if (indexToAdd >= valueCount) {
                    indexToAdd -= valueCount;
                }

                sumNext24Hv2 += valueArr[indexToAdd];

                sumNext24H.Add(sumNext24Hv2);
                //storagesizevariation
                double temporaryTarget = storagesize - (storage - sumNext24Hv2);
                if (temporaryTarget < minBuffer) {
                    temporaryTarget = minBuffer;
                }

                if (temporaryTarget > maxBuffer) {
                    temporaryTarget = maxBuffer;
                }

                temporaryTargetValues.Add(temporaryTarget);

                ProcessOneTimestep(storagesize,
                    ref currentVal,
                    lowerPowerLimit,
                    upperPowerLimit,
                    ref storage,
                    temporaryTarget,
                    dynamicCappingFactor,
                    powerReductionFactor);

                resultValues[i] = currentVal;
                if (makeStorageProfile) {
                    storageValues.Add(storage);
                }

                if (currentVal > maxPower * 1.01 && storage < storagesize) {
                    throw new FlaException("Storage made things worse " + i + " curvall: " + currentVal);
                }

                CheckViolations(storagesize, currentVal, upperPowerLimit, storage, lowerPowerLimit);
            }

            var resultProfile = new Profile(profile, resultValues.ToList().AsReadOnly()) {Name = profile.Name};
            storageProfile = new Profile(profile, storageValues.AsReadOnly()) {Name = profile.Name + "-EnergyStorage"};
            temporaryTargetProfile = new Profile("temporary targets", temporaryTargetValues.AsReadOnly(), EnergyOrPower.Energy);
            sum24HProfile = new Profile("24h", sumNext24H.AsReadOnly(), EnergyOrPower.Energy);
            return resultProfile;
        }

        [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")]
        private static void CheckViolations(double storagesize, double currentVal, double upperPowerLimit, double storage, double lowerPowerLimit)
        {
            if (currentVal > upperPowerLimit * 1.01 && storage > 0) {
                throw new FlaException("load limit violation even though storage is not empty");
            }

            if (currentVal < lowerPowerLimit * 1.01 && storage * 1.01 < storagesize) {
                throw new FlaException("generation violation even though storage is not full");
            }

            if (storage < 0) {
                throw new Exception("Negative storage is wrong.");
            }

            if (storage > storagesize * 1.01) {
                throw new Exception("more in storage than storage size is wrong.: " + storage + " vs max " + storagesize);
            }
        }

        private static double GetTargetValue([NotNull] ReadOnlyCollection<double> values, int timeStepsToConsider, int currentTimestep)
        {
            double energySum = 0;
            for (int i = currentTimestep; i < values.Count && i < currentTimestep + timeStepsToConsider; i++) {
                energySum += values[i];
            }

            double avgPower = energySum / timeStepsToConsider;
            return avgPower;
        }
    }
}