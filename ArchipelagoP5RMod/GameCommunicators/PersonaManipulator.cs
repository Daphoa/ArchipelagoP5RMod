using System.Collections.Frozen;
using ArchipelagoP5RMod.Types;
using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod.GameCommunicators;

public class PersonaManipulator
{
    private FlowFunctionWrapper.BasicFlowFunc _personaSetLvl;
    private FlowFunctionWrapper.BasicFlowFunc _addPersonaStock;
    private FlowFunctionWrapper.BasicFlowFunc _addPersonaSkill;

    private IntPtr _personaSetLvlPtr;
    private IntPtr _addPersonaStockPtr;
    private IntPtr _addPersonaSkillPtr;

    private readonly IDictionary<PartyMember, uint> _characterPersona;

    public PersonaManipulator(IReloadedHooks hooks)
    {
        Dictionary<PartyMember, uint> characterPersona = new Dictionary<PartyMember, uint>
        {
            { PartyMember.Joker, 201 }, // Arsène
            { PartyMember.Skull, 202 },
            { PartyMember.Mona, 203 },
            { PartyMember.Panther, 204 },
            { PartyMember.Fox, 205 }
        };
        _characterPersona = characterPersona.ToFrozenDictionary();

        AddressScanner.DelayedScanPattern(
            "48 89 5C 24 ?? 55 48 81 EC E0 00 00 00",
            address => _personaSetLvl =
                hooks.CreateWrapper<FlowFunctionWrapper.BasicFlowFunc>(address, out _personaSetLvlPtr));
        AddressScanner.DelayedScanPattern(
            "40 53 48 83 EC 20 33 C9 E8 ?? ?? ?? ?? B9 01 00 00 00 8B D8 E8 ?? ?? ?? ?? 66 83 F8 FF",
            address => _addPersonaStock =
                hooks.CreateWrapper<FlowFunctionWrapper.BasicFlowFunc>(address, out _addPersonaStockPtr));
        AddressScanner.DelayedScanPattern(
            "48 89 5C 24 ?? 57 48 83 EC 20 B9 01 00 00 00 E8 ?? ?? ?? ?? B9 02 00 00 00 44 8B C8 E8 ?? ?? ?? ?? " +
            "33 C9 8B D8 E8 ?? ?? ?? ?? 41 0F B7 D1 8B F8 0F B7 C8 E8 ?? ?? ?? ?? 0F B7 D0 66 83 F8 FF 0F 84 ?? " +
            "?? ?? ??",
            address => _addPersonaSkill =
                hooks.CreateWrapper<FlowFunctionWrapper.BasicFlowFunc>(address, out _addPersonaSkillPtr));
    }

    public void SetPartyLvl(PartyMember partyMember, uint lvl)
    {
        FlowFunctionWrapper.CallFlowFunctionSetup((uint)partyMember, _characterPersona[partyMember], lvl);

        _personaSetLvl.Invoke();

        FlowFunctionWrapper.CallFlowFunctionCleanup();
    }

    public void SetPersonaLvl(uint persona, uint lvl)
    {
        FlowFunctionWrapper.CallFlowFunctionSetup((uint)PartyMember.Joker, persona, lvl);

        _personaSetLvl.Invoke();

        FlowFunctionWrapper.CallFlowFunctionCleanup();
    }

    public void AddPersonaStock(uint personaId)
    {
        FlowFunctionWrapper.CallFlowFunctionSetup(personaId);

        _addPersonaStock.Invoke();

        FlowFunctionWrapper.CallFlowFunctionCleanup();
    }

    public void AddPersonaSkill(PartyMember partyMember, uint skillId)
    {
        FlowFunctionWrapper.CallFlowFunctionSetup((short)partyMember, _characterPersona[partyMember], skillId);
        
        _addPersonaSkill.Invoke();

        FlowFunctionWrapper.CallFlowFunctionCleanup();
    }
}