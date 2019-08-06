/////////////////////////////////
/// Mod: Longer Loiter
/// Author: ShortBeard
/// Version: 1.0
/// Description: Allows you to loiter for up to 24 hours
/// /////////////////////////////

using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using System;
using System.Reflection;
using UnityEngine;

namespace LongerLoiter {

    public class LongerLoiter : MonoBehaviour {

        private bool restWindowIsShowing = false;
        private Rect loiterButtonRect = new Rect(102, 13, 48, 24);
        private Panel mainPanel;
        private DaggerfallRestWindow restWindow;
        private Type topWindowType;
        private const int LOITER_LIMIT = 24;
        private const int LOITER_ENUM_INDEX = 3; //RestWindow.cs "currentRestMode" is an enum where loiter state is the 4th member 

        [Invoke(StateManager.StateTypes.Game, 0)]
        public static void Init(InitParams initParams) {
            GameObject longerLoiterObj = new GameObject("LongerLoiter");
            longerLoiterObj.AddComponent<LongerLoiter>();
            Mod longerLoiterMod = initParams.Mod;
            longerLoiterMod.IsReady = true;
        }

        private void Update() {
            if (DaggerfallUI.Instance.UserInterfaceManager.TopWindow is DaggerfallRestWindow && restWindowIsShowing == false) {
                restWindow = (DaggerfallRestWindow)DaggerfallUI.Instance.UserInterfaceManager.TopWindow;
                topWindowType = restWindow.GetType();
                MoveOldLoiterButton(); //Move the old loiter button off the screen so player can't use it
                CreateNewLoiterButton(GetRestWindowPanel()); //Put the new loiter button in it's place that uses methods without the time limit
                Debug.Log("Rest window has run");
                restWindowIsShowing = true;

            }
            if (GameManager.Instance.StateManager.CurrentState != StateManager.StateTypes.UI && restWindowIsShowing == true) {
                restWindowIsShowing = false;
            }
        }


        /// <summary>
        /// Get the main panel from the current rest window using reflection
        /// </summary>
        /// <returns></returns>
        private Panel GetRestWindowPanel() {
            FieldInfo mainPanelInfo = topWindowType.GetField("mainPanel", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic);
            mainPanel = (Panel)mainPanelInfo.GetValue(restWindow);
            return mainPanel;
        }

        /// <summary>
        /// Much of the access of the rest window panel and button properties need to be accessed via reflection since none of it is public.
        /// After accessing the existing loiter button, get its position and move it off the screen so that the player can no longer interact with it.
        /// </summary>
        private void MoveOldLoiterButton() {
            Debug.Log("Moving the old loiter button");
            //Get a reference to the original daggerfall loiter button
            FieldInfo buttonInfo = topWindowType.GetField("loiterButton", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic);
            Button oldLoiterButton = (Button)buttonInfo.GetValue(restWindow);

            //Move the old loiter button position to (0,0) so that the player can no longer interact with it
            PropertyInfo buttonPositionInfo = buttonInfo.GetValue(restWindow).GetType().GetProperty("Position");
            Vector2 buttonPos = (Vector2)buttonPositionInfo.GetValue(buttonInfo.GetValue(restWindow), null);
            buttonPositionInfo.SetValue(oldLoiterButton, new Vector2(0, 0), null);


            //Give the old loiter button a size of (0,0) so that the player can no longer interact with it
            buttonPositionInfo = buttonInfo.GetValue(restWindow).GetType().GetProperty("Size");
            Vector2 buttonize = (Vector2)buttonPositionInfo.GetValue(buttonInfo.GetValue(restWindow), null);
            buttonPositionInfo.SetValue(oldLoiterButton, new Vector2(0, 0), null);

        }


        /// <summary>
        /// This just uses the existing DaggerfallUI add button fuctionality to create a new loiter button where the old one used to be
        /// </summary>
        private void CreateNewLoiterButton(Panel mainPanel) {
            Button newLoiterButton = DaggerfallUI.AddButton(loiterButtonRect, mainPanel); //Put the new button it its place
            newLoiterButton.OnMouseClick += LoiterButton_OnMouseClick; //Subscribe the new button to a loiter method that has no time restriction
        }


        /// <summary>
        /// This is just a method copied from DaggerfallRestWindow.cs to keep everything consistent 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="position"></param>
        private void LoiterButton_OnMouseClick(BaseScreenComponent sender, Vector2 position) {
            DaggerfallInputMessageBox mb = new DaggerfallInputMessageBox(DaggerfallUI.UIManager, (restWindow));
            mb.SetTextBoxLabel(HardStrings.loiterHowManyHours);
            mb.TextPanelDistanceX = 5;
            mb.TextPanelDistanceY = 8;
            mb.TextBox.Text = "0";
            mb.TextBox.Numeric = true;
            mb.TextBox.MaxCharacters = 8;
            mb.TextBox.WidthOverride = 286;
            mb.OnGotUserInput += LoiterPrompt_OnGotUserInput;
            mb.Show();
        }


        /// <summary>
        /// After entering a number of hours to loiter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="input"></param>
        private void LoiterPrompt_OnGotUserInput(DaggerfallInputMessageBox sender, string input) {
            //Trim leading zeroes for entries such as "024" so that it parses correctly
            if (input != "0") {
                input.TrimStart(new char[] { '0' });
            }

            // Validate input
            int time = 0;
            bool result = int.TryParse(input, out time);
            if (!result)
                return;

            // Validate range
            if (time < 0) {
                time = 0;
            }
            else if (time > LOITER_LIMIT) {
                DaggerfallUI.MessageBox("You cannot loiter for more than " + LOITER_LIMIT + " hours");
                return;
            }


            //Reflection to set hoursRemaining field from the current DaggerfallRestWindow.cs instance
            FieldInfo restWindowInfo = restWindow.GetType().GetField("hoursRemaining", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic);
            restWindowInfo.SetValue(restWindow, time);

            //Reflection to set waitTimer field from the current DaggerfallRestWindow.cs instance
            restWindowInfo = restWindow.GetType().GetField("waitTimer", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic);
            restWindowInfo.SetValue(restWindow, Time.realtimeSinceStartup);

            //Reflection to set the currentRestMode enum field from the current DaggerfallRestWindow.cs instance.
            restWindowInfo = restWindow.GetType().GetField("currentRestMode", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic);
            restWindowInfo.SetValue(restWindow, LOITER_ENUM_INDEX); //Set the currentRestState to loitering
        }
    }
}