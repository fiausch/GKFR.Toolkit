using System.Collections.Generic;

namespace GKFR.ModLoader
{
    public interface IGkfrMod
    {
        string Id { get; }
        string DisplayName { get; }
        string Description { get; }
        bool Enabled { get; set; }
    }

    public interface IGkfrModSettings
    {
        void DrawExtraGui();
    }

    public static class GkfrModRegistry
    {
        static readonly List<IGkfrMod> mods = new List<IGkfrMod>();

        public static IReadOnlyList<IGkfrMod> Mods => mods;

        public static void Register(IGkfrMod mod)
        {
            if (mod == null)
            {
                return;
            }

            for (int i = 0; i < mods.Count; i++)
            {
                if (mods[i].Id == mod.Id)
                {
                    mods[i] = mod;
                    return;
                }
            }

            mods.Add(mod);
        }
    }
}
