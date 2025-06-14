﻿using ArchipelagoP5RMod.GameCommunicators;
using ArchipelagoP5RMod.Types;

namespace ArchipelagoP5RMod;

/**
 * Intended to be call the first time a save is loaded with AP integrated.
 */
public class FirstTimeSetup
{
    private readonly HashSet<uint> _onBits =
    [
        // Chest display on minimap
        0x20000000 + 476, 0x20000000 + 479, 0x20000000 + 495, 0x20000000 + 492, 0x20000000 + 377,
        0x20000000 + 497, 0x20000000 + 482, 0x20000000 + 483, 0x20000000 + 484, 0x20000000 + 493,
        0x20000000 + 485, 0x20000000 + 498, 0x20000000 + 480, 0x20000000 + 486, 0x20000000 + 487,
        0x20000000 + 491, 0x20000000 + 481, 0x20000000 + 490, 0x20000000 + 488, 0x20000000 + 477,
        0x20000000 + 478, 0x20000000 + 494, 0x20000000 + 495, 0x20000000 + 875, 0x20000000 + 895,
        0x20000000 + 878, 0x20000000 + 883, 0x20000000 + 884, 0x20000000 + 885, 0x20000000 + 886,
        0x20000000 + 887, 0x20000000 + 897, 0x20000000 + 898, 0x20000000 + 888, 0x20000000 + 889,
        0x20000000 + 891, 0x20000000 + 899, 0x20000000 + 876, 0x20000000 + 877, 0x20000000 + 1275,
        0x20000000 + 1295, 0x20000000 + 1276, 0x20000000 + 1277, 0x20000000 + 1278, 0x20000000 + 1279,
        0x20000000 + 1296, 0x20000000 + 1281, 0x20000000 + 1282, 0x20000000 + 1283, 0x20000000 + 1285,
        0x20000000 + 1286, 0x20000000 + 1297, 0x20000000 + 1287, 0x20000000 + 1298, 0x20000000 + 1291,
        0x20000000 + 1288, 0x20000000 + 1289, 0x20000000 + 1290, 0x20000000 + 1295, 0x20000000 + 1681,
        0x20000000 + 1682, 0x20000000 + 1683, 0x20000000 + 1684, 0x20000000 + 1696, 0x20000000 + 1675,
        0x20000000 + 1676, 0x20000000 + 1677, 0x20000000 + 1685, 0x20000000 + 1697, 0x20000000 + 1679,
        0x20000000 + 1680, 0x20000000 + 1695, 0x20000000 + 1686, 0x20000000 + 1678, 0x20000000 + 2075,
        0x20000000 + 2077, 0x20000000 + 2076, 0x20000000 + 2078, 0x20000000 + 2079, 0x20000000 + 2084,
        0x20000000 + 2095, 0x20000000 + 2080, 0x20000000 + 2081, 0x20000000 + 2096, 0x20000000 + 2082,
        0x20000000 + 2097, 0x20000000 + 2087, 0x20000000 + 2085, 0x20000000 + 2083, 0x20000000 + 2975,
        0x20000000 + 2976, 0x20000000 + 2977, 0x20000000 + 2978, 0x20000000 + 2996, 0x20000000 + 2983,
        0x20000000 + 2995, 0x20000000 + 2988, 0x20000000 + 2997, 0x20000000 + 2979, 0x20000000 + 2980,
        0x20000000 + 2981, 0x20000000 + 2982, 0x20000000 + 2998, 0x20000000 + 2985, 0x20000000 + 2990,
        0x20000000 + 2986, 0x20000000 + 2991, 0x20000000 + 3049, 0x20000000 + 2991, 0x20000000 + 3383,
        0x20000000 + 3384, 0x20000000 + 3382, 0x20000000 + 3381, 0x20000000 + 3379, 0x20000000 + 3380,
        0x20000000 + 3375, 0x20000000 + 3376, 0x20000000 + 3377, 0x20000000 + 3378, 0x20000000 + 4468,
        0x20000000 + 4469, 0x20000000 + 4470, 0x20000000 + 4488, 0x20000000 + 4483, 0x20000000 + 4471,
        0x20000000 + 4472, 0x20000000 + 4473, 0x20000000 + 4489, 0x20000000 + 4474, 0x20000000 + 4485,
        0x20000000 + 4481, 0x20000000 + 4487, 0x20000000 + 4475, 0x20000000 + 4476, 0x20000000 + 4477,
        0x20000000 + 4484, 0x20000000 + 4360, 0x20000000 + 4480, 0x20000000 + 4372, 0x20000000 + 4478,
        0x20000000 + 4486, 0x20000000 + 4479,

        // Maps
        6148, // Castle map 1

        // Tutorial
        0x20000000 + 171, // Grappling Hook Tutorial
        0x20000000 + 4081, // Chest Tutorial
        0x20000000 + 46, // Alert Tutorial
        0x20000000 + 4665, // Stone Tutorial
        11496, 11470, 4054, 4049, 4051, 4056, 4057, 11737, 252, 253, 1139, 1143, 1163, 4192, 4227, 11588, 3859, 3863,
        4059, 4060, 3916, 3906, 4719, 232, 4885, 4886, 4142, 5358, 4192, 11555, 11558, 4061, 4452, 4055, 4451,
        0x20000000 + 43, 0x20000000 + 44, 0x20000000 + 45, 0x20000000 + 47, 0x20000000 + 46, 0x20000000 + 48,
        0x20000000 + 49, 0x20000000 + 50, 0x20000000 + 51, 0x20000000 + 52, 0x20000000 + 53, 0x20000000 + 170,
        0x20000000 + 54, 0x20000000 + 165, 0x20000000 + 56, 0x20000000 + 166, 0x20000000 + 167, 0x20000000 + 168,
        0x20000000 + 169, 0x20000000 + 5103, 0x20000000 + 5104, 0x20000000 + 5105, 805306368 + 226, 0x30000000 + 224,
        0x30000000 + 216, 0x30000000 + 215, 0x30000000 + 217, 0x30000000 + 213, 0x30000000 + 235, 0x30000000 + 288,
        0x30000000 + 227, 0x30000000 + 293, 0x30000000 + 300, 0 + 234, 0x20000000 + 171, 0x20000000 + 5102,
        0x20000000 + 172, 0x20000000 + 4666, 0x20000000 + 4665, 0x20000000 + 4664, 0x20000000 + 43, 0x20000000 + 44,
        0x20000000 + 170, 0x20000000 + 165, 0x20000000 + 166, 0x20000000 + 167, 0x20000000 + 168, 0x20000000 + 169,
        0x20000000 + 5103,
        1188, 1189, 1199, // Infiltration tutorials

        // Events
        0x20000000 + 5116, 0x20000000 + 5117, // Red Lust Seed Event
    ];

    public void Setup(FlagManipulator flagManipulator, PersonaManipulator personaManipulator,
        ConfidantManipulator confidantManipulator, SocialStatManipulator socialStatManipulator,
        DateManipulator dateManipulator, MiscManipulator miscManipulator, ItemManipulator itemManipulator,
        PartyManipulator partyManipulator)
    {
        foreach (uint adr in _onBits)
        {
            flagManipulator.SetBit(adr, true);
        }

        // sdl04_05_PM_D
        flagManipulator.SetBit(80, true);
        flagManipulator.SetBit(12308, true);
        // SCENE_CHANGE_WAIT();
        // RECOVERY_ALL();
        flagManipulator.SetBit(8735, true);
        flagManipulator.SetCount(144, 0);
        // CALL_FIELD( 150, 2, 0, 0 );


        // MAIN_SetConquestFlag
        // TODO investigate this

        // DbgScript_200_030 - seems to be general setup
        flagManipulator.SetCount(6, 7);
        flagManipulator.SetBit(6353, true);
        flagManipulator.SetCount(174, 40000);
        flagManipulator.SetCount(145, 55000);

        // Safe rooms
        // flagManipulator.SetBit(6495, true);
        // flagManipulator.SetBit(6496, true);
        // flagManipulator.SetBit(6497, true);
        // flagManipulator.SetBit(6498, true);
        // flagManipulator.SetBit(6499, true);
        // flagManipulator.SetBit(6500, true);
        
        // PARTY_IN(2);
        // flagManipulator.SetBit(11824, true); // Ryuji flag
        // PARTY_IN(3);
        // flagManipulator.SetBit(11825, true);
        // PARTY_IN(4);
        // flagManipulator.SetBit(11826, true);
        // flagManipulator.SetBit(11827, false);
        // flagManipulator.SetBit(11828, false);
        // flagManipulator.SetBit(11829, false);
        // flagManipulator.SetBit(11830, false);
        // flagManipulator.SetBit(11831, false);

        flagManipulator.SetBit(8735, true);
        flagManipulator.SetBit(8734, true);
        flagManipulator.SetBit(113, true);
        flagManipulator.SetBit(1040, true);
        flagManipulator.SetBit(6345, true);
        flagManipulator.SetBit(6346, true);
        flagManipulator.SetBit(6347, true);
        flagManipulator.SetBit(6348, true);
        flagManipulator.SetBit(6349, true);
        flagManipulator.SetBit(6350, true);
        flagManipulator.SetBit(6351, true);
        flagManipulator.SetBit(6352, true);
        flagManipulator.SetBit(6353, true);
        flagManipulator.SetBit(6354, true);
        flagManipulator.SetBit(6393, true);
        flagManipulator.SetBit(6344, true);
        flagManipulator.SetBit(6235, true);
        flagManipulator.SetBit(6193, true);
        flagManipulator.SetBit(6194, true);
        flagManipulator.SetBit(6195, true);
        flagManipulator.SetBit(6196, true);
        flagManipulator.SetBit(6197, true);
        flagManipulator.SetBit(11276, true);
        flagManipulator.SetBit(6233, true);
        flagManipulator.SetBit(6345, true);
        flagManipulator.SetBit(6346, true);
        flagManipulator.SetBit(6347, true);
        flagManipulator.SetBit(6348, true);
        flagManipulator.SetBit(6349, true);
        flagManipulator.SetBit(6350, true);

        // DbgScript_150_000
        flagManipulator.SetBit(6144, true);
        flagManipulator.SetBit(12538, true);
        flagManipulator.SetBit(12308, true);
        flagManipulator.SetBit(10662, false);
        flagManipulator.SetBit(81, false);
        flagManipulator.SetBit(82, false);
        flagManipulator.SetBit(83, false);
        flagManipulator.SetBit(84, false);
        flagManipulator.SetBit(85, false);
        flagManipulator.SetBit(86, false);

        // local_flag_clear
        flagManipulator.SetCount(144, 0);
        flagManipulator.SetCount(145, 0);
        flagManipulator.SetCount(146, 0);
        flagManipulator.SetCount(147, 0);
        flagManipulator.SetCount(148, 0);
        flagManipulator.SetCount(149, 0);
        flagManipulator.SetCount(150, 0);
        flagManipulator.SetCount(151, 0);

        // Setup Kamoshida palace flags
        flagManipulator.SetCount(145, 40100);
        flagManipulator.SetBit(6741, false);
        flagManipulator.SetBit(11974, true);
        flagManipulator.SetBit(3920, false);
        flagManipulator.SetBit(105, false);
        flagManipulator.SetBit(6206, false);
        miscManipulator.VltFilterVisibleFlow(false);

        // Script -> Kamoshida palace
        flagManipulator.SetBit(1072, true);

        // Party members
        flagManipulator.SetBit(11779, true); // Can Edit Party
#if DEVELOP
        partyManipulator.UnlockPartyMember(PartyMember.Skull);
        partyManipulator.UnlockPartyMember(PartyMember.Mona);
        partyManipulator.UnlockPartyMember(PartyMember.Panther);
        partyManipulator.UnlockPartyMember(PartyMember.Fox);
        partyManipulator.UnlockPartyMember(PartyMember.Queen);
        partyManipulator.UnlockPartyMember(PartyMember.Noir);
        partyManipulator.UnlockPartyMember(PartyMember.Oracle);
        partyManipulator.UnlockPartyMember(PartyMember.Crow);
        partyManipulator.UnlockPartyMember(PartyMember.Violet);
        flagManipulator.SetBit(12048, true); // Black Mask
#endif

        // Skip fusion tutorial guess
        // flagManipulator.SetBit(6350, false);
        flagManipulator.SetBit(6393, true);

        // Open automatic confidants
        confidantManipulator.CmmOpen(Confidant.Igor);
        confidantManipulator.CmmOpen(Confidant.Morgana);
        confidantManipulator.CmmOpen(Confidant.Ann);
        confidantManipulator.CmmOpen(Confidant.Ryuji);
        confidantManipulator.CmmOpen(Confidant.Mishima);
        confidantManipulator.CmmOpen(Confidant.Maruki);
        confidantManipulator.CmmOpen(Confidant.Sumire);
        confidantManipulator.CmmOpen(Confidant.Sae);
        confidantManipulator.CmmOpen(Confidant.Akechi);
        confidantManipulator.CmmOpen(Confidant.Futaba);

        // SUB_ConqusetKamoshida_Start
        personaManipulator.AddPersonaStock(201); // Arsene
#if DEVELOP
        // personaManipulator.AddPersonaStock(131);
        // personaManipulator.AddPersonaStock(4);
        // personaManipulator.AddPersonaStock(121);
        personaManipulator.SetPartyLvl(PartyMember.Joker, 99); // Arsene
        personaManipulator.SetPartyLvl(PartyMember.Skull, 99);
        personaManipulator.SetPartyLvl(PartyMember.Mona, 99);
        personaManipulator.SetPartyLvl(PartyMember.Panther, 99);
        personaManipulator.SetPartyLvl(PartyMember.Fox, 99);
        personaManipulator.SetPartyLvl(PartyMember.Queen, 99);
        personaManipulator.SetPartyLvl(PartyMember.Noir, 99);
        personaManipulator.SetPartyLvl(PartyMember.Oracle, 99);
        personaManipulator.SetPartyLvl(PartyMember.Crow, 99);
        personaManipulator.SetPartyLvl(PartyMember.Violet, 99);
        personaManipulator.AddPersonaSkill(PartyMember.Skull, 200);
        personaManipulator.AddPersonaSkill(PartyMember.Mona, 325);
#endif
        // flagManipulator.SetBit(6349, true);
        // flagManipulator.SetBit(4012, true);
        // flagManipulator.SetBit(11971, true);
        // flagManipulator.SetBit(11972, true);
        // flagManipulator.SetBit(11973, true);
        // flagManipulator.SetBit(11974, true);
        // flagManipulator.SetBit(11467, true);
        // flagManipulator.SetBit(6309, true);
        // flagManipulator.SetBit(11464, true);
        // flagManipulator.SetBit(11496, true);
        // flagManipulator.SetBit(11276, true);

        // dateManipulator.SetDateDisplay(true);

        // QoL flags
        flagManipulator.SetBit(1195, true); // Allows Yongen-Jaya travel at night 
        flagManipulator.SetBit(1184, true); // Allows full map travel at night
        flagManipulator.SetBit(2134, true); // Unlock traits 

#if DEVELOP
        flagManipulator.SetBit(0x2A3B, true); // Grapple hook
        itemManipulator.RewardItem(0x4000 + 154, 1); // Grapple hook item
        confidantManipulator.EnableCmmFeature(0x50); // Instant kill
#endif

        // Social stats
        // socialStatManipulator.AddPcAllParam(34, 6, 14, 11, 12);
        // socialStatManipulator.AddPcAllParam(0, 0, 0, 11, 0);
    }
}