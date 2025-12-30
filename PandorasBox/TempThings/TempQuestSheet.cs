using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;
using System.CodeDom.Compiler;


namespace PandorasBox;


[GeneratedCode("Lumina.Excel.Generator", "2.0.0")]
[Sheet("Quest", 0x0AB3FE19)]
readonly public unsafe struct TempQuest(ExcelPage page, uint offset, uint row) : IExcelRow<TempQuest>
{
    public ExcelPage ExcelPage => page;
    public uint RowOffset => offset;
    public uint RowId => row;

    public readonly ReadOnlySeString Name => page.ReadString(offset, offset);
    public readonly Collection<QuestParamsStruct> QuestParams => new(page, offset, offset, &QuestParamsCtor, 50);
    public readonly Collection<QuestListenerParamsStruct> QuestListenerParams => new(page, offset, offset, &QuestListenerParamsCtor, 64);
    public readonly Collection<TodoParamsStruct> TodoParams => new(page, offset, offset, &TodoParamsCtor, 24);
    public readonly uint GilReward => page.ReadUInt32(offset + 2548);
    public readonly RowRef<Item> CurrencyReward => new(page.Module, page.ReadUInt32(offset + 2552), page.Language);
    public readonly uint CurrencyRewardCount => page.ReadUInt32(offset + 2556);
    public readonly Collection<RowRef> Reward => new(page, offset, offset, &RewardCtor, 7);
    public readonly Collection<RowRef<Item>> OptionalItemReward => new(page, offset, offset, &OptionalItemRewardCtor, 5);
    public readonly RowRef<InstanceContent> InstanceContentUnlock => new(page.Module, page.ReadUInt32(offset + 2608), page.Language);
    public readonly ushort ExpFactor => page.ReadUInt16(offset + 2612);
    public readonly RowRef<Emote> EmoteReward => new(page.Module, (uint)page.ReadUInt16(offset + 2614), page.Language);
    public readonly RowRef<Action> ActionReward => new(page.Module, (uint)page.ReadUInt16(offset + 2616), page.Language);
    public readonly Collection<ushort> SystemReward => new(page, offset, offset, &SystemRewardCtor, 2);
    public readonly ushort GCTypeReward => page.ReadUInt16(offset + 2622);
    public readonly Collection<RowRef<Item>> ItemCatalyst => new(page, offset, offset, &ItemCatalystCtor, 3);
    public readonly Collection<byte> ItemCountCatalyst => new(page, offset, offset, &ItemCountCatalystCtor, 3);
    public readonly byte ItemRewardType => page.ReadUInt8(offset + 2630);
    public readonly Collection<byte> ItemCountReward => new(page, offset, offset, &ItemCountRewardCtor, 7);
    public readonly Collection<RowRef<Stain>> RewardStain => new(page, offset, offset, &RewardStainCtor, 7);
    public readonly Collection<byte> OptionalItemCountReward => new(page, offset, offset, &OptionalItemCountRewardCtor, 5);
    public readonly Collection<RowRef<Stain>> OptionalItemStainReward => new(page, offset, offset, &OptionalItemStainRewardCtor, 5);
    public readonly Collection<RowRef<GeneralAction>> GeneralActionReward => new(page, offset, offset, &GeneralActionRewardCtor, 2);
    public readonly RowRef<QuestRewardOther> OtherReward => new(page.Module, (uint)page.ReadUInt8(offset + 2657), page.Language);
    public readonly byte Tomestone => page.ReadUInt8(offset + 2658);
    public readonly byte TomestoneReward => page.ReadUInt8(offset + 2659);
    public readonly byte TomestoneCountReward => page.ReadUInt8(offset + 2660);
    public readonly byte ReputationReward => page.ReadUInt8(offset + 2661);
    public readonly bool Unknown0 => page.ReadBool(offset + 2662);
    public readonly bool Unknown1 => page.ReadBool(offset + 2663);
    public readonly bool Unknown2 => page.ReadBool(offset + 2664);
    public readonly bool Unknown3 => page.ReadBool(offset + 2665);
    public readonly bool Unknown4 => page.ReadBool(offset + 2666);
    public readonly bool Unknown5 => page.ReadBool(offset + 2667);
    public readonly bool Unknown6 => page.ReadBool(offset + 2668);
    public readonly Collection<bool> OptionalItemIsHQReward => new(page, offset, offset, &OptionalItemIsHQRewardCtor, 5);
    public readonly ReadOnlySeString Id => page.ReadString(offset + 2676, offset);
    public readonly Collection<RowRef<TempQuest>> PreviousQuest => new(page, offset, offset, &PreviousQuestCtor, 3);
    public readonly Collection<RowRef<TempQuest>> QuestLock => new(page, offset, offset, &QuestLockCtor, 2);
    public readonly Collection<RowRef<InstanceContent>> InstanceContent => new(page, offset, offset, &InstanceContentCtor, 3);
    public readonly RowRef IssuerStart => RowRef.GetFirstValidRowOrUntyped(page.Module, page.ReadUInt32(offset + 2712), [typeof(EObjName), typeof(ENpcResident)], -965341264, page.Language);
    public readonly RowRef<Level> IssuerLocation => new(page.Module, page.ReadUInt32(offset + 2716), page.Language);
    public readonly RowRef TargetEnd => RowRef.GetFirstValidRowOrUntyped(page.Module, page.ReadUInt32(offset + 2720), [typeof(EObjName), typeof(ENpcResident)], -965341264, page.Language);
    public readonly RowRef<JournalGenre> JournalGenre => new(page.Module, page.ReadUInt32(offset + 2724), page.Language);
    public readonly uint Icon => page.ReadUInt32(offset + 2728);
    public readonly uint IconSpecial => page.ReadUInt32(offset + 2732);
    public readonly RowRef<Mount> MountRequired => new(page.Module, (uint)page.ReadInt32(offset + 2736), page.Language);
    public readonly Collection<ushort> ClassJobLevel => new(page, offset, offset, &ClassJobLevelCtor, 2);
    public readonly ushort Header => page.ReadUInt16(offset + 2744);
    public readonly RowRef<Festival> Festival => new(page.Module, (uint)page.ReadUInt16(offset + 2746), page.Language);
    public readonly ushort BellStart => page.ReadUInt16(offset + 2748);
    public readonly ushort BellEnd => page.ReadUInt16(offset + 2750);
    public readonly ushort BeastReputationValue => page.ReadUInt16(offset + 2752);
    public readonly SubrowRef<Behavior> ClientBehavior => new(page.Module, (uint)page.ReadUInt16(offset + 2754), page.Language);
    public readonly SubrowRef<QuestClassJobSupply> QuestClassJobSupply => new(page.Module, (uint)page.ReadUInt16(offset + 2756), page.Language);
    public readonly RowRef<PlaceName> PlaceName => new(page.Module, (uint)page.ReadUInt16(offset + 2758), page.Language);
    public readonly ushort SortKey => page.ReadUInt16(offset + 2760);
    public readonly RowRef<ExVersion> Expansion => new(page.Module, (uint)page.ReadUInt8(offset + 2762), page.Language);
    public readonly RowRef<ClassJobCategory> ClassJobCategory0 => new(page.Module, (uint)page.ReadUInt8(offset + 2763), page.Language);
    public readonly byte QuestLevelOffset => page.ReadUInt8(offset + 2764);
    public readonly RowRef<ClassJobCategory> ClassJobCategory1 => new(page.Module, (uint)page.ReadUInt8(offset + 2765), page.Language);
    public readonly byte PreviousQuestJoin => page.ReadUInt8(offset + 2766);
    public readonly byte Unknown7 => page.ReadUInt8(offset + 2767);
    public readonly byte QuestLockJoin => page.ReadUInt8(offset + 2768);
    public readonly byte Unknown8 => page.ReadUInt8(offset + 2769);
    public readonly byte Unknown9 => page.ReadUInt8(offset + 2770);
    public readonly RowRef<ClassJob> ClassJobUnlock => new(page.Module, (uint)page.ReadUInt8(offset + 2771), page.Language);
    public readonly RowRef<GrandCompany> GrandCompany => new(page.Module, (uint)page.ReadUInt8(offset + 2772), page.Language);
    public readonly RowRef<GrandCompanyRank> GrandCompanyRank => new(page.Module, (uint)page.ReadUInt8(offset + 2773), page.Language);
    public readonly byte InstanceContentJoin => page.ReadUInt8(offset + 2774);
    public readonly byte FestivalBegin => page.ReadUInt8(offset + 2775);
    public readonly byte FestivalEnd => page.ReadUInt8(offset + 2776);
    public readonly RowRef<BeastTribe> BeastTribe => new(page.Module, (uint)page.ReadUInt8(offset + 2777), page.Language);
    public readonly RowRef<BeastReputationRank> BeastReputationRank => new(page.Module, (uint)page.ReadUInt8(offset + 2778), page.Language);
    public readonly RowRef<SatisfactionNpc> SatisfactionNpc => new(page.Module, (uint)page.ReadUInt8(offset + 2779), page.Language);
    public readonly byte SatisfactionLevel => page.ReadUInt8(offset + 2780);
    public readonly RowRef<DeliveryQuest> DeliveryQuest => new(page.Module, (uint)page.ReadUInt8(offset + 2781), page.Language);
    public readonly byte RepeatIntervalType => page.ReadUInt8(offset + 2782);
    public readonly RowRef<QuestRepeatFlag> QuestRepeatFlag => new(page.Module, (uint)page.ReadUInt8(offset + 2783), page.Language);
    public readonly byte Type => page.ReadUInt8(offset + 2784);
    public readonly byte Unknown_70 => page.ReadUInt8(offset + 2785);
    public readonly byte LevelMax => page.ReadUInt8(offset + 2786);
    public readonly RowRef<ClassJob> ClassJobRequired => new(page.Module, (uint)page.ReadUInt8(offset + 2787), page.Language);
    public readonly RowRef<QuestRewardOther> QuestRewardOtherDisplay => new(page.Module, (uint)page.ReadUInt8(offset + 2788), page.Language);
    public readonly byte Unknown10 => page.ReadUInt8(offset + 2789);
    public readonly RowRef<EventIconType> EventIconType => new(page.Module, (uint)page.ReadUInt8(offset + 2790), page.Language);
    /// <summary>
    /// 1/2 - normal daily beast tribe quests, 3 - 'exclusive' (if player's rank is not greater than max rank requirement of quests offered by npc, exactly one of the available quests will be from this pool)
    /// </summary>
    public readonly byte DailyQuestPool => page.ReadUInt8(offset + 2791);
    public readonly bool IsHouseRequired => page.ReadPackedBool(offset + 2792, 0);
    public readonly bool IsRepeatable => page.ReadPackedBool(offset + 2792, 1);
    public readonly bool CanCancel => page.ReadPackedBool(offset + 2792, 2);
    public readonly bool Introduction => page.ReadPackedBool(offset + 2792, 3);
    public readonly bool HideOfferIcon => page.ReadPackedBool(offset + 2792, 4);
    public readonly bool Unknown12 => page.ReadPackedBool(offset + 2792, 5);
    public readonly bool Unknown13 => page.ReadPackedBool(offset + 2792, 6);

    private static QuestParamsStruct QuestParamsCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => new(page, parentOffset, offset + 4 + i * 8);
    private static QuestListenerParamsStruct QuestListenerParamsCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => new(page, parentOffset, offset + 404 + i * 20);
    private static TodoParamsStruct TodoParamsCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => new(page, parentOffset, offset + 1684 + i * 36);
    private static RowRef RewardCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => (/* ItemRewardType */ page.ReadUInt8(offset + 2630)) switch
    {
        1 => RowRef.Create<Item>(page.Module, page.ReadUInt32(offset + 2560 + i * 4), page.Language),
        3 => RowRef.Create<Item>(page.Module, page.ReadUInt32(offset + 2560 + i * 4), page.Language),
        5 => RowRef.Create<Item>(page.Module, page.ReadUInt32(offset + 2560 + i * 4), page.Language),
        6 => RowRef.CreateSubrow<QuestClassJobReward>(page.Module, page.ReadUInt32(offset + 2560 + i * 4), page.Language),
        7 => RowRef.Create<BeastRankBonus>(page.Module, page.ReadUInt32(offset + 2560 + i * 4), page.Language),
        _ => RowRef.CreateUntyped(page.ReadUInt32(offset + 2560 + i * 4), page.Language),
    };
    private static RowRef<Item> OptionalItemRewardCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => new(page.Module, page.ReadUInt32(offset + 2588 + i * 4), page.Language);
    private static ushort SystemRewardCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => page.ReadUInt16(offset + 2618 + i * 2);
    private static RowRef<Item> ItemCatalystCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => new(page.Module, (uint)page.ReadUInt8(offset + 2624 + i), page.Language);
    private static byte ItemCountCatalystCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => page.ReadUInt8(offset + 2627 + i);
    private static byte ItemCountRewardCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => page.ReadUInt8(offset + 2631 + i);
    private static RowRef<Stain> RewardStainCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => new(page.Module, (uint)page.ReadUInt8(offset + 2638 + i), page.Language);
    private static byte OptionalItemCountRewardCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => page.ReadUInt8(offset + 2645 + i);
    private static RowRef<Stain> OptionalItemStainRewardCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => new(page.Module, (uint)page.ReadUInt8(offset + 2650 + i), page.Language);
    private static RowRef<GeneralAction> GeneralActionRewardCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => new(page.Module, (uint)page.ReadUInt8(offset + 2655 + i), page.Language);
    private static bool OptionalItemIsHQRewardCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => page.ReadBool(offset + 2669 + i);
    private static RowRef<TempQuest> PreviousQuestCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => new(page.Module, page.ReadUInt32(offset + 2680 + i * 4), page.Language);
    private static RowRef<TempQuest> QuestLockCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => new(page.Module, page.ReadUInt32(offset + 2692 + i * 4), page.Language);
    private static RowRef<InstanceContent> InstanceContentCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => new(page.Module, page.ReadUInt32(offset + 2700 + i * 4), page.Language);
    private static ushort ClassJobLevelCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => page.ReadUInt16(offset + 2740 + i * 2);

    public readonly struct QuestParamsStruct(ExcelPage page, uint parentOffset, uint offset)
    {
        public readonly ReadOnlySeString ScriptInstruction => page.ReadString(offset, parentOffset);
        public readonly uint ScriptArg => page.ReadUInt32(offset + 4);
    }

    public readonly struct QuestListenerParamsStruct(ExcelPage page, uint parentOffset, uint offset)
    {
        public readonly uint Listener => page.ReadUInt32(offset);
        public readonly uint ConditionValue => page.ReadUInt32(offset + 4);
        public readonly ushort Behavior => page.ReadUInt16(offset + 8);
        public readonly byte ActorSpawnSeq => page.ReadUInt8(offset + 10);
        public readonly byte ActorDespawnSeq => page.ReadUInt8(offset + 11);
        public readonly byte Unknown0 => page.ReadUInt8(offset + 12);
        public readonly byte Unknown1 => page.ReadUInt8(offset + 13);
        public readonly byte QuestUInt8A => page.ReadUInt8(offset + 14);
        public readonly byte ConditionType => page.ReadUInt8(offset + 15);
        public readonly byte ConditionOperator => page.ReadUInt8(offset + 16);
        public readonly bool VisibleBool => page.ReadPackedBool(offset + 17, 0);
        public readonly bool ConditionBool => page.ReadPackedBool(offset + 17, 1);
        public readonly bool ItemBool => page.ReadPackedBool(offset + 17, 2);
        public readonly bool AnnounceBool => page.ReadPackedBool(offset + 17, 3);
        public readonly bool BehaviorBool => page.ReadPackedBool(offset + 17, 4);
        public readonly bool AcceptBool => page.ReadPackedBool(offset + 17, 5);
        public readonly bool QualifiedBool => page.ReadPackedBool(offset + 17, 6);
        public readonly bool CanTargetBool => page.ReadPackedBool(offset + 17, 7);
    }

    public readonly struct TodoParamsStruct(ExcelPage page, uint parentOffset, uint offset)
    {
        public readonly Collection<RowRef<Level>> ToDoLocation => new(page, parentOffset, offset, &ToDoLocationCtor, 8);
        public readonly byte ToDoCompleteSeq => page.ReadUInt8(offset + 32);
        public readonly byte ToDoQty => page.ReadUInt8(offset + 33);
        public readonly byte CountableNum => page.ReadUInt8(offset + 34);

        private static RowRef<Level> ToDoLocationCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => new(page.Module, page.ReadUInt32(offset + i * 4), page.Language);
    }

    static TempQuest IExcelRow<TempQuest>.Create(ExcelPage page, uint offset, uint row) =>
        new(page, offset, row);
}
