using System;
using System.Collections.Generic;
using LightWeaver.Player;
using UnityEngine;

namespace LightWeaver.Core
{
    public enum PrismBlessingId : byte
    {
        WideLens = 0,       // Fener koni açısı +%28 genişler
        SolarCapacitor = 1, // Alınan +ENERGY küreleri +%45 fazla şarj verir
        PhotonFeather = 2,  // Zıplama gücü +%15 artar, Çift Zıplama pil harcamaz
        EcoCore = 3,        // Pil tüketim hızı %22 azalır
        BurstDynamo = 4,    // Flash-Burst dolum süresi yarıya iner
        EclipseRepulsor = 5 // The Eclipse dalgasını 5m geri iter ve skoru artırır
    }

    public readonly struct PrismBlessingDefinition
    {
        public readonly PrismBlessingId Id;
        public readonly string Title;
        public readonly string Description;

        public PrismBlessingDefinition(PrismBlessingId id, string title, string description)
        {
            Id = id;
            Title = title;
            Description = description;
        }
    }

    /// <summary>
    /// Her sektör geçişinde oyuncuya 3 rastgele Prizma Güçlendirmesi sunan ve
    /// koşucunun sistemlerine kalıcı koşu-içi modifikatörler uygulayan Roguelite yönetici.
    /// </summary>
    public sealed class PrismUpgradeManager : MonoBehaviour
    {
        [SerializeField] private LightConeCaster2D coneCaster;
        [SerializeField] private BatterySystem batterySystem;
        [SerializeField] private RunnerMotor2D runnerMotor;
        [SerializeField] private FlashBurstController flashBurst;

        private static readonly PrismBlessingDefinition[] Catalog =
        {
            new PrismBlessingDefinition(PrismBlessingId.WideLens, "PRISM WIDE LENS", "+28% Flashlight Cone Width"),
            new PrismBlessingDefinition(PrismBlessingId.SolarCapacitor, "SOLAR CAPACITOR", "+45% Battery From +ENERGY Orbs"),
            new PrismBlessingDefinition(PrismBlessingId.PhotonFeather, "PHOTON FEATHER", "+15% Jump Height & Free Double Jump"),
            new PrismBlessingDefinition(PrismBlessingId.EcoCore, "ECO CORE", "-22% Flashlight Battery Drain"),
            new PrismBlessingDefinition(PrismBlessingId.BurstDynamo, "BURST DYNAMO", "2x Faster Flash-Burst Cooldown"),
            new PrismBlessingDefinition(PrismBlessingId.EclipseRepulsor, "ECLIPSE REPULSOR", "Push Back The Void + Refill 30% Battery"),
        };

        private float _angleMult = 1f;
        private float _drainMult = 1f;
        private float _chargeMult = 1f;
        private float _jumpPowerMult = 1f;
        private bool _freeDoubleJump;
        private float _burstCooldownMult = 1f;

        private readonly List<PrismBlessingId> _poolIndices = new List<PrismBlessingId>(6);

        public void ResetAllBlessings()
        {
            _angleMult = 1f;
            _drainMult = 1f;
            _chargeMult = 1f;
            _jumpPowerMult = 1f;
            _freeDoubleJump = false;
            _burstCooldownMult = 1f;
            ApplyModifiers();
        }

        public void RollThreeOptions(PrismBlessingDefinition[] outputBuffer)
        {
            if (outputBuffer == null || outputBuffer.Length < 3) return;
            _poolIndices.Clear();
            for (int i = 0; i < Catalog.Length; i++) _poolIndices.Add(Catalog[i].Id);

            for (int k = 0; k < 3; k++)
            {
                int pick = UnityEngine.Random.Range(0, _poolIndices.Count);
                PrismBlessingId chosenId = _poolIndices[pick];
                _poolIndices.RemoveAt(pick);
                outputBuffer[k] = Catalog[(int)chosenId];
            }
        }

        public void ApplyBlessing(PrismBlessingId id)
        {
            switch (id)
            {
                case PrismBlessingId.WideLens:
                    _angleMult += 0.28f;
                    break;
                case PrismBlessingId.SolarCapacitor:
                    _chargeMult += 0.45f;
                    break;
                case PrismBlessingId.PhotonFeather:
                    _jumpPowerMult += 0.15f;
                    _freeDoubleJump = true;
                    break;
                case PrismBlessingId.EcoCore:
                    _drainMult *= 0.78f;
                    break;
                case PrismBlessingId.BurstDynamo:
                    _burstCooldownMult *= 0.5f;
                    break;
                case PrismBlessingId.EclipseRepulsor:
                    if (batterySystem != null) batterySystem.AddCharge(30f);
                    break;
            }
            ApplyModifiers();
        }

        private void ApplyModifiers()
        {
            if (coneCaster != null) coneCaster.SetBlessingAngleMultiplier(_angleMult);
            if (batterySystem != null) batterySystem.SetBlessingModifiers(_drainMult, _chargeMult);
            if (runnerMotor != null) runnerMotor.SetJumpBlessing(_jumpPowerMult, _freeDoubleJump);
            if (flashBurst != null) flashBurst.SetCooldownMultiplier(_burstCooldownMult);
        }
    }
}
