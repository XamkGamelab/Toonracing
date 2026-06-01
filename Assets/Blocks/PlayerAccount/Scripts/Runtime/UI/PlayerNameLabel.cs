using System;
using System.Threading.Tasks;
using Blocks.Common;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Blocks.PlayerAccount
{
    /// <summary>This controls shows the current player name, and also modify it.
    /// Be aware that the player name is suffixed by 4 digits that are appended by the service.
    /// It is recommended that you use a server authority (like cloud-code) to ensure the name
    /// of the player is suitable for your context, and not let players arbitrarily edit them</summary>
    [UxmlElement]
    public partial class PlayerNameLabel : VisualElement
    {
        bool m_Editing = false;
        TextField m_TextBox;
        Button m_EditButton;
        Label m_Label;
        CloudDataContainer m_CloudDataContainer;

        /// <summary> This uxml attribute can be used to customize the label tag before the data </summary>
        [UxmlAttribute("label")]
        public string LabelPrefix
        {
            get => m_Label.text;
            set => m_Label.text = value;
        }

        public PlayerNameLabel()
        {
            dataSource = m_CloudDataContainer = new CloudDataContainer();
            AddToClassList(PlayerAccountTheme.PlayerLabelContainer);
            SetupLabel();
            LabelPrefix = "Played Name";
            SetupTextBox();
            SetupEditButton();
        }

        void SetupLabel()
        {
            m_Label = new Label();
            m_Label.AddToClassList(BlocksTheme.Label);
            Add(m_Label);
        }

        void SetupTextBox()
        {
            m_TextBox = new TextField();
            m_TextBox.AddToClassList(BlocksTheme.TextField);
            m_TextBox.AddToClassList(PlayerAccountTheme.LabelTextField);
            m_TextBox.isReadOnly = true;
            m_TextBox.value = "<player name>";
            m_TextBox.dataSource = dataSource;
            var dataBinding = new DataBinding()
            {
                dataSourcePath = new PropertyPath(nameof(CloudDataContainer.PlayerName)),
                bindingMode = BindingMode.ToTarget,
            };

            m_TextBox.SetBinding(new BindingId("value"), dataBinding);
            Add(m_TextBox);
        }

        void SetupEditButton()
        {
            m_EditButton = new Button();
            m_EditButton.text = "Edit";
            m_EditButton.AddToClassList(BlocksTheme.Button);
            m_EditButton.AddToClassList(PlayerAccountTheme.LabelButton);
            Add(m_EditButton);
            m_EditButton.clicked += EditButtonClicked;
        }

        async void EditButtonClicked()
        {
            if (m_Editing)
                await EndEditedPlayerName();
            else
                StartEditingPlayerName();
        }

        void StartEditingPlayerName()
        {
            m_Editing = true;
            m_EditButton.text = "Save";
            m_TextBox.isReadOnly = false;
            // #xyz always added by the auth service, must remove before showing edit mode
            // or you end up stacking them
            var playerName = m_CloudDataContainer.PlayerName;

            if (playerName != null)
            {
                var poundIndex = playerName.LastIndexOf('#');
                playerName = playerName.Substring(0, poundIndex);
            }
            else
            {
                playerName = string.Empty;
            }

            m_TextBox.value = playerName;
        }

        async Task EndEditedPlayerName()
        {
            m_Editing = false;
            m_EditButton.text = "Edit";
            m_TextBox.isReadOnly = true;
            try
            {
                await m_CloudDataContainer.UpdateRemotePlayerName(m_TextBox.value);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to set player name '{m_TextBox.value}'. Reason: {e.Message}");
            }
        }
    }
}
