using System;
using System.Threading.Tasks;
using Blocks.Common;
using UnityEngine;
using UnityEngine.UIElements;

namespace Blocks.PlayerAccount
{
    /// <summary>
    /// This class provides a specialized version of a PlayerDataLabel
    /// that adds editing functionality to the data.
    /// When the data is finished editing, it will be updated in the
    /// relevant cloud-save key for the player, and access class
    /// </summary>
    [UxmlElement]
    public partial class PlayerEditableDataLabel : PlayerDataLabel
    {
        Button m_EditButton;
        bool m_Editing;

        public PlayerEditableDataLabel()
        {
            SetupEditButton();
        }

        void SetupEditButton()
        {
            m_EditButton = new Button();
            m_EditButton.AddToClassList(BlocksTheme.Button);
            m_EditButton.AddToClassList(PlayerAccountTheme.LabelButton);
            m_EditButton.text = "Edit";
            Add(m_EditButton);
            m_EditButton.clicked += EditButtonClicked;
        }

        async void EditButtonClicked()
        {
            if (m_Editing)
                await EndEditedPlayerData();
            else
                StartEditingPlayerData();
        }

        void StartEditingPlayerData()
        {
            m_Editing = true;
            m_EditButton.text = "Save";
            m_TextBox.isReadOnly = false;
            m_TextBox.value = m_DataContainer.DataValue;
        }

        async Task EndEditedPlayerData()
        {
            m_Editing = false;
            m_EditButton.text = "Edit";
            m_TextBox.isReadOnly = true;
            try
            {
                await m_DataContainer.UpdateRemoteDataValue(m_TextBox.value);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to set player data '{DataKey}' to '{m_TextBox.value}'. Reason: {e.Message}");
            }
        }
    }
}
