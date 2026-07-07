namespace HAVIGAME.Services.Advertisings {
    [System.Flags]
    public enum AdGroup : byte {
        None = 0,
        Group_1 = 1 << 0,
        Group_2 = 1 << 1,
        Group_3 = 1 << 2,
        Group_4 = 1 << 3,
        Tier_1 = 1 << 4,
        Tier_2 = 1 << 5,
        Tier_3 = 1 << 6,
        Tier_4 = 1 << 7,
        Group_All = Group_1 | Group_2 | Group_3 | Group_4,
        Tier_All = Tier_1 | Tier_2 | Tier_3 | Tier_4,
        All = Group_1 | Group_2 | Group_3 | Group_4 | Tier_1 | Tier_2 | Tier_3 | Tier_4,
    }
}
