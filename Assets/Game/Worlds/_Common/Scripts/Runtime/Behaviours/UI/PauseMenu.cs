// Creature Creator - https://github.com/daniellochner/Creature-Creator
// Copyright (c) Daniel Lochner

using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DanielLochner.Assets.CreatureCreator
{
    public class PauseMenu : Dialog<PauseMenu>
    {
        #region Properties
        private bool CanToggle => !ConfirmationDialog.Instance.IsOpen && !InformationDialog.Instance.IsOpen && !InputDialog.Instance.IsOpen && !UnlockableBodyPartsMenu.Instance.IsOpen && !UnlockablePatternsMenu.Instance.IsOpen && !KeybindingsDialog.Instance.IsOpen;
        #endregion

        #region Methods
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && CanToggle)
            {
                Toggle();
            }
        }
        protected override void LateUpdate() { /* Override default close-on-ESC behaviour */ }

        public void Leave()
        {
            ConfirmationDialog.Confirm("Leave?", "Are you sure you want to leave the current game, and return to the main menu?", onYes: delegate
            {
                if (GameSetup.Instance.IsMultiplayer)
                {
                    NetworkConnectionManager.Instance.Leave();
                }
                else
                {
                    NetworkShutdownManager.Instance.Shutdown();
                    SceneManager.LoadScene("MainMenu");
                }
            });
        }
        #endregion
    }
}