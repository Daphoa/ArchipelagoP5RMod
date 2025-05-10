using ArchipelagoP5RMod.GameCommunicators;
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

        // Events
        0x20000000 + 5116, 0x20000000 + 5117, // Red Lust Seed Event
    ];

    public void Setup(FlagManipulator flagManipulator, PersonaManipulator personaManipulator,
        ConfidantManipulator confidantManipulator)
    {
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

        // palace_clear_flag
        flagManipulator.SetBit(8735, true);
        flagManipulator.SetBit(8734, true);

        // Script -> Kamoshida palace
        flagManipulator.SetBit(1072, true);

        // SUB_ConqusetKamoshida_Start
        confidantManipulator.CmmOpen(Confidant.Morgana);
        confidantManipulator.CmmOpen(Confidant.Ann);
        confidantManipulator.CmmOpen(Confidant.Ryuji);
        // confidantManipulator.CmmOpen(Confidant.Takemi);
        // confidantManipulator.CmmOpen(Confidant.Sojiro);
        personaManipulator.AddPersonaStock(201);
        personaManipulator.AddPersonaStock(131);
        personaManipulator.AddPersonaStock(4);
        personaManipulator.AddPersonaStock(121);
        personaManipulator.SetPartyLvl(PartyMember.Joker, 5);
        personaManipulator.SetPartyLvl(PartyMember.Skull, 5);
        personaManipulator.SetPartyLvl(PartyMember.Mona, 5);
        personaManipulator.SetPartyLvl(PartyMember.Panther, 5);
        // personaManipulator.SetPartyLvl(PartyMember.Fox, 5);
        // personaManipulator.SetPartyLvl(PartyMember.Noir, 5);
        // personaManipulator.SetPartyLvl(PartyMember.Oracle, 5);
        personaManipulator.AddPersonaSkill(PartyMember.Skull, 200);
        personaManipulator.AddPersonaSkill(PartyMember.Mona, 325);
        flagManipulator.SetBit(4012, true);
        flagManipulator.SetBit(11971, true);
        flagManipulator.SetBit(11972, true);
        flagManipulator.SetBit(11973, true);
        flagManipulator.SetBit(11974, true);
        flagManipulator.SetBit(11467, true);
        flagManipulator.SetBit(6309, true);
        flagManipulator.SetBit(11464, true);
        flagManipulator.SetBit(11496, true);
        flagManipulator.SetBit(11276, true);

        // Some flags stolen from debug that might be helpful
        flagManipulator.SetBit(11974, true);
        flagManipulator.SetBit(3920, false);
        flagManipulator.SetBit(105, false);
        flagManipulator.SetBit(6206, false);
        flagManipulator.SetBit(6724, true);
        flagManipulator.SetBit(8826, false);
        flagManipulator.SetBit(8828, false);
        flagManipulator.SetBit(8830, true);
        flagManipulator.SetBit(8843, true);
        flagManipulator.SetCount(159, 1);
        flagManipulator.SetBit(6211, false);
        flagManipulator.SetBit(6217, false);
        flagManipulator.SetBit(6723, true);
        flagManipulator.SetBit(10808, true);
        flagManipulator.SetBit(10810, true);
        flagManipulator.SetBit(6194, true);
        flagManipulator.SetBit(6403, true);
        flagManipulator.SetBit(8829, false);
        flagManipulator.SetBit(8830, false);
        flagManipulator.SetBit(10760, true);
        flagManipulator.SetBit(10761, true);
        flagManipulator.SetBit(11541, true);
        flagManipulator.SetBit(11556, true);
        flagManipulator.SetBit(6176, false);
        flagManipulator.SetBit(6180, false);
        flagManipulator.SetBit(6195, true);
        flagManipulator.SetBit(6400, true);
        flagManipulator.SetBit(11246, true);

        // Party members
        // flagManipulator.SetBit(11779, true); // Can Edit Party
        flagManipulator.SetBit(11824, true); // Ryuji
        flagManipulator.SetBit(11825, true); // Morgana
        flagManipulator.SetBit(11826, true); // Ann
        // flagManipulator.SetBit(11827, true); // Yusuke
        // flagManipulator.SetBit(11828, true); // Makoto
        // flagManipulator.SetBit(11829, true); // Haru
        // flagManipulator.SetBit(11830, true); // Futaba
        // flagManipulator.SetBit(11831, true); // Aketchi
        // flagManipulator.SetBit(11832, true); // Kasumi

        foreach (uint adr in _onBits)
        {
            flagManipulator.SetBit(adr, true);
        }
    }
}