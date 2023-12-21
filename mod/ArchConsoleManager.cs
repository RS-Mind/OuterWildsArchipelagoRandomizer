﻿using OWML.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ArchipelagoRandomizer
{
    /// <summary>
    /// Class that handles adding text to the console
    /// </summary>
    public class ArchConsoleManager : MonoBehaviour
    {
        private GameObject console;
        private GameObject pauseConsole;
        private GameObject pauseConsoleVisuals;
        private GameObject gameplayConsole;
        private Text pauseConsoleText;
        private Text gameplayConsoleText;
        //private GameObject consoleEntry => Randomizer.Assets.LoadAsset<GameObject>("ConsoleText");
        private List<string> consoleHistory;
        //private List<string> pauseConsoleEntries;
        private List<string> gameplayConsoleEntries;
        private InputField consoleText;
        private bool isPaused;

        private void Awake()
        {
            LoadManager.OnCompleteSceneLoad += CreateConsoles;
            consoleHistory = new List<string>();
        }

        private void Start()
        {
            GlobalMessenger.AddListener("EnterConversation", () => gameplayConsole.SetActive(false));
            GlobalMessenger.AddListener("ExitConversation", () => gameplayConsole.SetActive(true));
        }

        private void Update()
        {
            // Show the correct version of the console depending on if the game is paused or not
            if (isPaused != Randomizer.Instance.ModHelper.Menus.PauseMenu.IsOpen)
            {
                isPaused = !isPaused;
                ShowConsoles(isPaused);
            }
        }

        // Creates the two console displays
        private void CreateConsoles(OWScene scene, OWScene loadScene)
        {
            if (loadScene != OWScene.SolarSystem && loadScene != OWScene.EyeOfTheUniverse) return;
            // Create objects and establish references
            gameplayConsoleEntries = new List<string>();
            console = GameObject.Instantiate(Randomizer.Assets.LoadAsset<GameObject>("ArchRandoCanvas"));
            pauseConsoleVisuals = console.transform.Find("PauseConsole").gameObject;
            pauseConsole = console.transform.Find("PauseConsole/Scroll View/Viewport/PauseConsoleText").gameObject;
            gameplayConsole = console.transform.Find("GameplayConsole/GameplayConsoleText").gameObject;
            pauseConsoleText = pauseConsole.GetComponent<Text>();
            gameplayConsoleText = gameplayConsole.GetComponent<Text>();

            pauseConsoleText.text = string.Empty;
            gameplayConsoleText.text = string.Empty;

            // Copy text over from previous loops
            foreach (string entry in consoleHistory)
            {
                AddText(entry, true, true);
            }
            console.GetComponentInChildren<InputField>().onEndEdit.AddListener(OnConsoleEntry);
            consoleText = console.GetComponentInChildren<InputField>();
            pauseConsoleVisuals.SetActive(false);

            StartCoroutine(LoopGreeting());
        }

        // Shows the appropriate consoles when the game is paused or not
        private void ShowConsoles(bool showPauseConsole)
        {
            pauseConsoleVisuals.SetActive(showPauseConsole);
            gameplayConsole.SetActive(!showPauseConsole);
        }

        /// <summary>
        /// Adds a new text entry to the in-game consoles
        /// </summary>
        /// <param name="text">The text to add to the consoles</param>
        /// <param name="skipGameplayConsole">Whether to only show text on the pause console</param>
        /// <param name="skipHistory">Whether to not save this text between loops</param>
        public void AddText(string text, bool skipGameplayConsole = false, bool skipHistory = false)
        {
            if (!skipHistory) consoleHistory.Add(text);
            pauseConsoleText.text += "\n" + text;

            // We don't need to bother editing the Gameplay Console if this is on
            if (!skipGameplayConsole)
            {
                if (gameplayConsoleEntries.Count < 6)
                {
                    gameplayConsoleEntries.Add("\n" + text);
                }
                else
                {
                    for (int i = 0; i < 5; i++)
                    {
                        gameplayConsoleEntries[i] = gameplayConsoleEntries[i + 1];
                    }
                    gameplayConsoleEntries[5]= "\n" + text;
                }
                gameplayConsoleText.text = string.Empty;
                foreach (string entry in gameplayConsoleEntries)
                {
                    gameplayConsoleText.text += entry;
                }
            }

            if (!isPaused)
            {
                gameplayConsole.SetActive(true);
            }
        }

        /// <summary>
        /// Adds a new text entry to the in-game consoles.
        /// Identical to Randomizer.Instance.ArchConsoleManager.AddText(text), but implemented for convenience.
        /// </summary>
        /// <param name="text">The text to add to the consoles</param>
        /// <param name="skipGameplayConsole">Whether to only show text on the pause console</param>
        /// <param name="skipHistory">Whether to not save this text between loops</param>
        public static void AddConsoleText(string text, bool skipGameplayConsole = false, bool skipHistory = false)
        {
            Randomizer.Instance.ArchConsoleManager.AddText(text, skipGameplayConsole, skipHistory);
        }

        /// <summary>
        /// Runs whenever the console text is submitted.
        /// </summary>
        /// <param name="text"></param>
        public void OnConsoleEntry(string text)
        {
            if (text == "") return;
            // This is not how actual commands should be handled, but this exists for testing
            if (text.StartsWith("!echo "))
            {
                AddText(text.Replace("!echo ", ""));
            }
            else if (text == "!loops")
            {
                AddText($"<color=#6BFF6B>Loops: {TimeLoop.GetLoopCount()}</color>");
            }
            else
            {
                AddText($"<color=#FF6868>Command {text.Split(' ')[0]} not recognized.</color>");
            }
            consoleText.text = "";
        }

        private string LoopNumber()
        {
            string loopSuffix = "th";
            int loopCount = TimeLoop.GetLoopCount();
            int shortCount = loopCount % 10;
            if (loopCount < 11 || loopCount > 13)
            {
                switch (shortCount)
                {
                    case 1:
                        loopSuffix = "st";
                        break;
                    case 2:
                        loopSuffix = "nd";
                        break;
                    case 3:
                        loopSuffix = "rd";
                        break;
                    default:
                        loopSuffix = "th";
                        break;
                }
            }
            return loopCount.ToString() + loopSuffix;
        }

        // We need to wait for the end of the frame when loading into the system for the game to be able to read the current loop number
        IEnumerator LoopGreeting()
        {
            yield return new WaitForEndOfFrame();
            AddText($"<color=#6BFF6B>Welcome to your {LoopNumber()} loop!</color>", true);
        }
    }
}