﻿using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Colors;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ImGuiNET;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Splatoon.SplatoonScripting;
using Splatoon.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol
{
    public unsafe class Dynamis_Delta : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 1122 };

        public override Metadata? Metadata => new(1, "NightmareXIV");

        Config Conf => Controller.GetConfig<Config>();

        PlayerCharacter Player => Svc.ClientState.LocalPlayer;

        int Stage = 0;
        uint myTether;
        bool isMeClose;

        class Effects
        {
            public const uint NearWorld = 3442;
            public const uint FarWorld = 3443;
            //  Remote Code Smell (3504), Remains = 16.2, Param = 0, Count = 0
            public const uint UpcomingBlueTether = 3504;
            //  Local Code Smell (3440), Remains = 16.2, Param = 0, Count = 0
            public const uint UpcomingGreenTether = 3440;

            public const uint BlueTether = 1673;
            public const uint GreenTether = 1672;

            public const uint MonitorLeft = 3453;
            public const uint MonitorRight = 3452;

            public const uint TwiceRuin = 2534;
            public const uint ThriceRuin = 2530;
        }

        bool IsAnyoneUnsafe => FakeParty.Get().Any(x => x.HasEffect(Effects.ThriceRuin) || x.HasEffect(Effects.TwiceRuin));

        public override void OnSetup()
        {
            for(var i = 0; i < 8; i++)
            {
                Controller.RegisterElement($"Debug{i}", new(0) { Enabled = false});
            }
            Controller.RegisterElementFromCode("Bait", "{\"Name\":\"\",\"Enabled\":false,\"radius\":0.0,\"color\":3355508735,\"overlayBGColor\":4278190080,\"overlayTextColor\":4294967295,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"BAIT\",\"tether\":true}");
            base.OnSetup();
            Controller.RegisterElementFromCode("Alert", "{\"Enabled\":false,\"Name\":\"\",\"type\":1,\"radius\":0.0,\"overlayBGColor\":4278190080,\"overlayTextColor\":4294967295,\"overlayVOffset\":3.0,\"overlayFScale\":2.0,\"thicc\":0.0,\"overlayText\":\"WARNING\",\"refActorType\":1}");
        }

        //Outer blue seq: break tether, go to partner, stay until monitor appears, go out for bait
        //Inner blue seq: stay until monitor appears, break and go out for bait

        public override void OnUpdate()
        {
            Off();
            if (Controller.Scene == 6)
            {
                var beetle = GetBeetle();
                var final = GetFinalOmega();
                if(beetle != null)
                {
                    if (Stage == 0 && (HasEffect(Effects.UpcomingBlueTether) || HasEffect(Effects.UpcomingGreenTether)))
                    {
                        var p = FakeParty.Get().ToArray();
                        var myMob = HasEffect(Effects.UpcomingGreenTether) ? final : beetle;
                        myTether = HasEffect(Effects.UpcomingGreenTether) ? Effects.UpcomingGreenTether : Effects.UpcomingBlueTether;
                        var sameTethers = p.Where(x => x.HasEffect(myTether)).OrderBy(x => GetAngleRelativeToObject(myMob, x, true)).ToArray();
                        var myPartner = (Player.Address.EqualsAny(sameTethers[0..2].Select(x => x.Address)) ? sameTethers[0..2] : sameTethers[2..4]).Where(x => x.Address != Player.Address).First();
                        for (int i = 0; i < sameTethers.Length; i++)
                        {
                            if (Controller.TryGetElementByName($"Debug{i}", out var e))
                            {
                                e.Enabled = true;
                                e.SetRefPosition(sameTethers[i].Position);
                                e.overlayText = $"{GetAngleRelativeToObject(myMob, sameTethers[i], true)}" + (myPartner.Address == sameTethers[i].Address ? " Partner" : "");
                            }
                        }
                        isMeClose = Vector3.Distance(myPartner.Position, new Vector3(100, 0, 100)) > Vector3.Distance(Player.Position, new Vector3(100, 0, 100));
                        //InternalLog.Information($"Me close: {isMeClose}");
                        if(myTether == Effects.UpcomingBlueTether)
                        {
                            Alert("Blue - to beetle!", ImGuiColors.TankBlue);
                        }
                        else
                        {
                            Alert("Green - to final (stretch)", ImGuiColors.HealerGreen);
                        }
                        if(Svc.Objects.Any(x => x.DataId == 15710))
                        {
                            DuoLog.Information($"Snapshotting: you are ${(myTether==Effects.UpcomingBlueTether?"blue":"green")} " + (isMeClose ? "close" : "far"));
                            Stage = 1;
                            DuoLog.Information("Stage 1");
                        }
                    }
                    else if(Stage == 1)
                    {
                        if(myTether == Effects.UpcomingBlueTether)
                        {
                            if (HasEffect(Effects.UpcomingBlueTether))
                            {
                                Alert("Prepare to stack!");
                            }
                            else
                            {
                                Alert("STACK WITH YOUR PARTNER FAST", GradientColor.Get(0xff000000.ToVector4(), ImGuiColors.ParsedPurple, 200));
                            }
                            if (HasEffect(Effects.BlueTether))
                            {
                                if (IsAnyoneUnsafe)
                                {
                                    Alert("Await for debuff before breaking!", Colors.Red.ToVector4());
                                }
                                else
                                {
                                    Alert("Break - go far!", GradientColor.Get(ImGuiColors.DalamudRed, ImGuiColors.DalamudYellow, 200));
                                }
                            }
                        }
                        else
                        {
                            Alert("Stack together");
                            //green tether
                        }
                        if(FakeParty.Get().Any(x => x.HasEffect(Effects.MonitorLeft) || x.HasEffect(Effects.MonitorRight)))
                        {
                            Stage = 2;
                            DuoLog.Information("Stage 2");
                        }
                    }
                    else if(Stage == 2)
                    {
                        var arms = GetArms().ToArray();
                        if (arms.Length == 6)
                        {
                            BattleChara? myArm = null;
                            if(myTether == Effects.UpcomingBlueTether)
                            {
                                if (isMeClose)
                                {
                                    Alert("Middle, bait Beyond Defense", ImGuiColors.DalamudRed);
                                }
                                else
                                {
                                    Alert("Bait designated arm", ImGuiColors.DalamudOrange);
                                    myArm = arms.OrderBy(x => Vector3.Distance(x.Position, beetle.Position)).ToArray()[0..2].OrderBy(x => Vector3.Distance(Player.Position, x.Position)).First();
                                }
                            }
                            else
                            {
                                if (isMeClose)
                                {
                                    Alert("Bait designated arm", ImGuiColors.DalamudOrange);
                                    myArm = arms.OrderBy(x => Vector3.Distance(x.Position, final.Position)).ToArray()[2..4].OrderBy(x => Vector3.Distance(Player.Position, x.Position)).First();
                                }
                                else
                                {
                                    Alert("Bait designated arm", ImGuiColors.DalamudOrange);
                                    myArm = arms.OrderBy(x => Vector3.Distance(x.Position, final.Position)).ToArray()[0..2].OrderBy(x => Vector3.Distance(Player.Position, x.Position)).First();
                                }
                            }
                            if(myArm != null && Controller.TryGetElementByName("Bait", out var e))
                            {
                                e.Enabled = true;
                                e.SetRefPosition(myArm.Position);
                            }
                            if(arms.Any(x => x.CastActionId == 31600))
                            {
                                Stage = 3;
                                DuoLog.Information($"Stage 3");
                            }
                        }
                    }
                    else if(Stage == 3)
                    {
                        if (HasEffect(Effects.BlueTether))
                        {
                            if (IsAnyoneUnsafe)
                            {
                                Alert("Await for debuff before breaking!", Colors.Red.ToVector4());
                            }
                            else
                            {
                                Alert("Break - go far!", GradientColor.Get(ImGuiColors.DalamudRed, ImGuiColors.DalamudYellow, 200));
                            }
                        }
                        if(myTether == Effects.UpcomingBlueTether)
                        {
                            if (!HasEffect(Effects.TwiceRuin))
                            {
                                Alert("Stack in middle", ImGuiColors.HealerGreen);
                            }
                            else
                            {
                                Alert("AVOID STACK AND MONITORS", ImGuiColors.DPSRed);
                            }
                        }
                        else
                        {
                            Alert("Spread for monitors baits", ImGuiColors.HealerGreen);
                        }
                        if (!FakeParty.Get().Any(x => x.HasEffect(Effects.MonitorLeft) || x.HasEffect(Effects.MonitorRight)))
                        {
                            Stage = 4;
                            DuoLog.Information("Stage 4");
                        }
                    }
                    else if(Stage == 4)
                    {
                        if (HasEffect(Effects.NearWorld))
                        {
                            Alert("NEAR WORLD", ImGuiColors.HealerGreen);
                        }
                        else if (HasEffect(Effects.FarWorld))
                        {
                            Alert("FAR WORLD", ImGuiColors.ParsedBlue);
                        }
                        else
                        {
                            if(myTether == Effects.UpcomingBlueTether)
                            {
                                Alert("Baiter for near");
                            }
                            else
                            {
                                if (isMeClose)
                                {
                                    if (HasEffect(Effects.GreenTether))
                                    {
                                        if (IsAnyoneUnsafe)
                                        {
                                            Alert("Await for debuff before breaking!", Colors.Red.ToVector4());
                                        }
                                        else
                                        {
                                            Alert("Break - go CLOSE!", GradientColor.Get(ImGuiColors.DalamudRed, ImGuiColors.DalamudYellow, 200));
                                        }
                                    }
                                    else
                                    {
                                        Alert("Chill spot");
                                    }
                                }
                                else
                                {
                                    Alert("Far - maintain tether", ImGuiColors.HealerGreen);
                                }
                            }
                        }
                        if(!FakeParty.Get().Any(x => x.HasEffect(Effects.NearWorld)))
                        {
                            Stage = 5;
                            DuoLog.Information("Stage 5");
                        }
                    }
                    else if(Stage == 5)
                    {
                        if (HasEffect(Effects.GreenTether))
                        {
                            if (IsAnyoneUnsafe)
                            {
                                Alert("Await for debuff before breaking!", Colors.Red.ToVector4());
                            }
                            else
                            {
                                Alert("Break - go CLOSE!", GradientColor.Get(ImGuiColors.DalamudRed, ImGuiColors.DalamudYellow, 200));
                            }
                        }
                        else
                        {
                            Stage = 0;
                            DuoLog.Information("Mechanic finished");
                        }
                    }
                }
                else
                {
                    Stage = 0;
                }
            }
            else
            {
                Stage = 0;
            }
        }

        public override void OnSettingsDraw()
        {
            ImGui.InputInt("Stage", ref Stage);
            if (ImGui.RadioButton("Green", myTether == Effects.UpcomingGreenTether))
            {
                myTether = Effects.UpcomingGreenTether;
            }
            if (ImGui.RadioButton("Blue", myTether == Effects.UpcomingBlueTether))
            {
                myTether = Effects.UpcomingBlueTether;
            }
            ImGui.Checkbox("Close", ref isMeClose);
        }

        void Alert(string? text = null, Vector4? color = null)
        {
            if (Controller.TryGetElementByName("Alert", out var e))
            {
                if (text == null)
                {
                    e.Enabled = false;
                }
                else
                {
                    e.Enabled = true;
                    if(color != null)
                    {
                        e.overlayBGColor = color.Value.ToUint();
                    }
                    else
                    {
                        e.overlayBGColor = 0xFF000000;
                    }
                    e.overlayText = text;
                }
            }
        }


        IEnumerable<BattleChara> GetArms()
        {
            foreach(var x in Svc.Objects)
            {
                if (x is BattleChara b && b.DataId.EqualsAny<uint>(15719, 15718)) yield return x as BattleChara;
            }
        }

        void Off()
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        }

        BattleChara? GetBeetle() => Svc.Objects.FirstOrDefault(x => x is BattleChara c && c.Struct()->Character.ModelCharaId == 3771) as BattleChara;

        BattleChara? GetFinalOmega() => Svc.Objects.FirstOrDefault(x => x is BattleChara c && c.Struct()->Character.ModelCharaId == 3775) as BattleChara;

        bool HasEffect(uint id) => Player.StatusList.Any(x => x.StatusId == id);
        bool HasEffect(uint id, float remainsMin, float remainsMax) => Player.StatusList.Any(x => x.StatusId == id && x.RemainingTime.InRange(remainsMin,remainsMax));

        float GetAngleRelativeToObject(GameObject source, GameObject target, bool invert = false)
        {
            var angle = MathHelper.GetRelativeAngle(source.Position, target.Position);
            var angleRot = source.Rotation.RadToDeg();
            return (angle - angleRot + (invert?(360+180):360) ) % 360;
        }

        public class Config : IEzConfig
        {
            public bool Debug = false;
        }
    }

    public static class Dynamis_Delta_Extensions
    {
        public static bool HasEffect(this BattleChara obj, uint id) => obj.StatusList.Any(x => x.StatusId == id);
        public static bool HasEffect(this BattleChara obj, uint id, float remainsMin, float remainsMax) => obj.StatusList.Any(x => x.StatusId == id && x.RemainingTime.InRange(remainsMin, remainsMax));
    }
}