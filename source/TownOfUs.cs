using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Reactor;
using Reactor.Utilities.Extensions;
using Reactor.Networking.Attributes;
using TownOfUs.CustomOption;
using TownOfUs.Patches;
using TownOfUs.RainbowMod;
using TownOfUs.Extensions;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using TownOfUs.Patches.ScreenEffects;
using TownOfUs.CrewmateRoles.DetectiveMod;
using TownOfUs.NeutralRoles.SoulCollectorMod;
using System.IO;
using Reactor.Utilities;

namespace TownOfUs
{
    [BepInPlugin(Id, "Town Of Us", VersionString)]
    [BepInDependency(ReactorPlugin.Id)]
    [BepInDependency(SubmergedCompatibility.SUBMERGED_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [ReactorModFlags(Reactor.Networking.ModFlags.RequireOnAllClients)]
    public class TownOfUs : BasePlugin
    {
        public const string Id = "com.slushiegoose.townofus";
        public const string VersionString = "5.2.2";
        public static System.Version Version = System.Version.Parse(VersionString);
        public const string VersionTag = "<color=#ff33fc></color>";

        public static AssetLoader bundledAssets;

        public enum SpriteType
        {
            Janitor, Engineer, SwapperSwitch, SwapperSwitchDisabled, Footprint,
            Medic, Seer, Sample, Morph, Arrow, Mine, Swoop, Douse,
            Ignite, Revive, Button, Disperse, Drag, Drop, CycleBack,
            CycleForward, Guess, Flash, Alert, Remember, Track, Plant,
            Detonate, Transport, Mediate, Vest, Protect, Blackmail,
            BlackmailLetter, BlackmailOverlay, Lighter, Darker, Infect,
            Rampage, Trap, Inspect, Examine, Recall, Mark,
            ImitateSelect, ImitateDeselect, Observe, Bite, Reveal,
            Confess, NoAbility, Camouflage, CamoSprint, CamoSprintFreeze,
            Hack, Mimic, Lock, Stalk, CrimeScene, Campaign, Fortify,
            Hypnotise, Hysteria, Jail, InJail, Execute, Collect,
            Reap, Soul, Watch, Camp, Shoot, Rewind,
            TownOfUsBanner, UpdateToUButton, UpdateSubmergedButton,
            Plus, Minus, PlusActive, MinusActive
        }
        
        public static Dictionary<SpriteType,Sprite> Sprites = new ();

        public static Vector3 ButtonPosition { get; private set; } = new Vector3(2.6f, 0.7f, -9f);

        private static DLoadImage _iCallLoadImage;

        private Harmony _harmony;

        public static ConfigEntry<bool> DeadSeeGhosts { get; set; }
        public static ConfigEntry<bool> MinPlayersUnlock { get; set; }
        public static ConfigEntry<bool> DisableWinCondition { get; set; }

        public static string RuntimeLocation;

        public override void Load()
        {
            RuntimeLocation = Path.GetDirectoryName(Assembly.GetAssembly(typeof(TownOfUs)).Location);
            ReactorCredits.Register<TownOfUs>(ReactorCredits.AlwaysShow);
            System.Console.WriteLine("000.000.000.000/000000000000000000");

            _harmony = new Harmony("com.slushiegoose.townofus");

            Generate.GenerateAll();

            bundledAssets = new();

            foreach (SpriteType spriteType in Enum.GetValues(typeof(SpriteType)))
            {
                Sprites[spriteType] = CreateSprite($"TownOfUs.Resources.{spriteType}.png");
            }
            
            PalettePatch.Load();
            ClassInjector.RegisterTypeInIl2Cpp<RainbowBehaviour>();
            ClassInjector.RegisterTypeInIl2Cpp<CrimeScene>();
            ClassInjector.RegisterTypeInIl2Cpp<Soul>();

            for (int i = 1; i <= 5; i++)
            {
                try
                {
                    var filePath = Application.persistentDataPath;
                    var file = filePath + $"/GameSettings-Slot{i}";
                    if (File.Exists(file))
                    {
                        string newFile = Path.Combine(filePath, $"Saved Settings {i}.txt");
                        File.Move(file, newFile);
                    }
                }
                catch
                {
                }
            }

            // RegisterInIl2CppAttribute.Register();

            DeadSeeGhosts = Config.Bind("Settings", "Dead See Other Ghosts", true,
                "Whether you see other dead player's ghosts while your dead");
            MinPlayersUnlock = Config.Bind("Debug", "Unlock minimum players count", false,
                "Whether you can start a game with less then 4 platyers");
            DisableWinCondition = Config.Bind("Debug", "Disables game ending", false,
                "Whether you want to disable the game ending");

            _harmony.PatchAll();
            SubmergedCompatibility.Initialize();

            ServerManager.DefaultRegions = new Il2CppReferenceArray<IRegionInfo>(new IRegionInfo[0]);
        }

        public static Sprite CreateSprite(string name)
        {
            var pixelsPerUnit = 100f;
            var pivot = new Vector2(0.5f, 0.5f);

            var assembly = Assembly.GetExecutingAssembly();
            var tex = AmongUsExtensions.CreateEmptyTexture();
            var imageStream = assembly.GetManifestResourceStream(name);
            var img = imageStream.ReadFully();
            LoadImage(tex, img, true);
            tex.DontDestroy();
            var sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), pivot, pixelsPerUnit);
            sprite.DontDestroy();
            return sprite;
        }

        public static void LoadImage(Texture2D tex, byte[] data, bool markNonReadable)
        {
            _iCallLoadImage ??= IL2CPP.ResolveICall<DLoadImage>("UnityEngine.ImageConversion::LoadImage");
            var il2CPPArray = (Il2CppStructArray<byte>)data;
            _iCallLoadImage.Invoke(tex.Pointer, il2CPPArray.Pointer, markNonReadable);
        }

        private delegate bool DLoadImage(IntPtr tex, IntPtr data, bool markNonReadable);
    }
}