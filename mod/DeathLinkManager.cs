﻿using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class DeathLinkManager
{
    enum DeathLinkSetting: long
    {
        Off = 0,
        Default = 1,
        AllDeaths = 2,
    }

    private static DeathLinkSetting setting = DeathLinkSetting.Off;

    private static DeathLinkService service = null;

    private static bool manualDeathInProgress = false;

    // When "dying" on the pause menu, or when time is frozen by e.g. the ship log, you can end up in states that
    // at first appear to be a softlock. AFAICT none of them really are softlocks, but they still feel like bugs,
    // so we want to "buffer deaths" until whatever paused in-game is done.
    private static bool dieAfterUnpause = false;

    public static void Enable(long value)
    {
        if (Enum.IsDefined(typeof(DeathLinkSetting), value))
            setting = (DeathLinkSetting)value;
        else
            APRandomizer.OWMLModConsole.WriteLine($"{value} is not a valid death link setting", OWML.Common.MessageType.Error);

        if (setting != DeathLinkSetting.Off && service == null)
        {
            service = APRandomizer.APSession.CreateDeathLinkService();
            service.EnableDeathLink();
            service.OnDeathLinkReceived += OnDeathLinkReceived;
        }
    }

    // useful for testing
    /*[HarmonyPrefix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Update))]
    public static void ToolModeUI_Update_Prefix()
    {
        if (OWInput.SharedInputManager.IsNewlyPressed(InputLibrary.down2))
        {
            OnDeathLinkReceived(new DeathLink("death link test player", "death link test cause"));
        }
    }*/

    private static void OnDeathLinkReceived(DeathLink deathLinkObject)
    {
        APRandomizer.OWMLModConsole.WriteLine($"OnDeathLinkReceived() Timestamp={deathLinkObject.Timestamp}, Source={deathLinkObject.Source}, Cause={deathLinkObject.Cause}");

        APRandomizer.InGameAPConsole.AddText(deathLinkObject.Cause);

        if (OWTime.IsPaused())
        {
            APRandomizer.OWMLModConsole.WriteLine($"buffering death because OWTime is currently paused");
            dieAfterUnpause = true;
        }
        else
            ActuallyKillThePlayer();
    }

    [HarmonyPostfix, HarmonyPatch(typeof(OWTime), nameof(OWTime.Unpause))]
    private static void OWTime_Unpause_Postfix()
    {
        if (dieAfterUnpause)
        {
            dieAfterUnpause = false;
            APRandomizer.OWMLModConsole.WriteLine($"applying buffered death now that OWTime has unpaused");
            ActuallyKillThePlayer();
        }
    }

    private static void ActuallyKillThePlayer()
    {
        DeathLinkManager.manualDeathInProgress = true;
        Locator.GetDeathManager().KillPlayer(DeathType.Default);
        DeathLinkManager.manualDeathInProgress = false;
    }

    private static Random prng = new Random();

    private static Dictionary<DeathType, List<string>> deathMessages = new Dictionary<DeathType, List<string>> {
        { DeathType.Default, new List<string>
        {
            " became one with the universe.",
            " stubbed their toe on a fascinating rock.",
            " made an oopsie."
        } },
        { DeathType.Impact, new List<string>
        {
            " should've slowed down.",
            " checked for fall damage.",
            " didn’t bounce."
        } },
        { DeathType.Asphyxiation, new List<string>
        {
            " forgot to hug a tree.",
            " forgot their spacesuit."
        } },
        { DeathType.Energy, new List<string>
        {
            " experienced nuclear fusion firsthand.",
            " forgot to turn off auto pilot.",
            " became the marshmallow.",
            " isn't a hotshot."
        } },
        { DeathType.Supernova, new List<string>
        {
            " roasted all the marshmallows.",
            " experienced astrophysics firsthand."
        } },
        { DeathType.Digestion, new List<string>
        {
            " did some hands on biology.",
            " touched da fishy.",
            " was eaten by Ernesto."
        } },
        { DeathType.BigBang, new List<string>
        {
            "'s garage band got out of control.",
            " started a sitcom."
        } },
        { DeathType.Crushed, new List<string>
        {
            " became a pancake."
        } },
        { DeathType.Meditation, new List<string>
        {
            " took a long nap.",
            " didn’t set an alarm."
        } },
        { DeathType.TimeLoop, new List<string>
        {
            " wasn’t watching the clock.",
            " couldn’t escape."
        } },
        { DeathType.Lava, new List<string>
        {
            " will be back.",
            " caught the ring.",
            " went for a swim."
        } },
        { DeathType.BlackHole, new List<string>
        {
            " was spaghettified.",
            " didn’t come out the other side."
        } },
        { DeathType.Dream, new List<string>
        {
            " underestimated the astral plane.",
            " made a new friend."
        } },
        { DeathType.DreamExplosion, new List<string>
        {
            " learned why product recalls are important.",
            " did some QA testing."
        } },
        { DeathType.CrushedByElevator, new List<string>
        {
            " became a Flat Hearther.",
            " didn’t look up."
        } },
    };

    [HarmonyPrefix, HarmonyPatch(typeof(DeathManager), nameof(DeathManager.KillPlayer))]
    public static void DeathManager_KillPlayer_Prefix(DeathManager __instance, DeathType deathType)
    {
        // if this death was sent to us by another player's death link, do nothing, since that would start an infinite death loop
        if (manualDeathInProgress)
        {
            APRandomizer.OWMLModConsole.WriteLine($"DeathManager.KillPlayer ignoring {deathType} death because this is a death we received from another player");
            return;
        }
        if (__instance._isDead)
        {
            APRandomizer.OWMLModConsole.WriteLine($"DeathManager.KillPlayer ignoring {deathType} death because DeathManager._isDead is already true");
            return;
        }
        if (__instance._isDying)
        {
            APRandomizer.OWMLModConsole.WriteLine($"DeathManager.KillPlayer ignoring {deathType} death because DeathManager._isDying is already true");
            return;
        }

        if (setting == DeathLinkSetting.Off)
        {
            APRandomizer.OWMLModConsole.WriteLine($"DeathManager.KillPlayer ignoring {deathType} death since death_link is off");
            return;
        }

        if (service == null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"Unable to send {deathType} death to AP server because death link service is null", OWML.Common.MessageType.Error);
            return;
        }

        if (setting == DeathLinkSetting.Default) {
            if (deathType == DeathType.Meditation || deathType == DeathType.Supernova || deathType == DeathType.TimeLoop || deathType == DeathType.BigBang)
            {
                APRandomizer.OWMLModConsole.WriteLine($"DeathManager.KillPlayer ignoring {deathType} death since death_link is only set to Default");
                return;
            }
        }

        APRandomizer.OWMLModConsole.WriteLine($"DeathManager.KillPlayer detected a {deathType} death, sending to AP server");
        var messagesForDeathType = deathMessages.ContainsKey(deathType) ? deathMessages[deathType] : deathMessages[DeathType.Default];
        var deathLinkMessage = APRandomizer.SaveData.apConnectionData.slotName + messagesForDeathType[prng.Next(0, messagesForDeathType.Count)];
        APRandomizer.InGameAPConsole.AddText($"Because death link is set to {setting}, sending this {deathType} death to other players with the message: \"{deathLinkMessage}\"");
        service.SendDeathLink(new DeathLink(APRandomizer.SaveData.apConnectionData.slotName, deathLinkMessage));
    }
}
