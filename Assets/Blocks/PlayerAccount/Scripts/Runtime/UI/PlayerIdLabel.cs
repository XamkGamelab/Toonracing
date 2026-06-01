using System;
using Blocks.Common;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Blocks.PlayerAccount
{
    /// <summary> This label will display the current player ID and offer a useful copy-to-clipboard
    /// functionality so that you can easily find them on the cloud dashboard </summary>
    [UxmlElement]
    public partial class PlayerIdLabel : VisualElement
    {
        Label m_Label;
        Button m_CopyToClipboard;
        readonly CloudDataContainer m_CloudDataContainer;
        TextField m_TextBox;

        /// <summary> This uxml attribute can be used to customize the label tag before the data </summary>
        [UxmlAttribute("label")]
        public string LabelPrefix
        {
            get => m_Label.text;
            set => m_Label.text = value;
        }

        public PlayerIdLabel()
        {
            dataSource = m_CloudDataContainer = new CloudDataContainer();
            AddToClassList(PlayerAccountTheme.PlayerLabelContainer);
            SetupLabel();
            LabelPrefix = "Played ID";
            SetupTextBox();
            SetupCopyButton();
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
            m_TextBox.value = "<player id>";
            m_TextBox.dataSource = dataSource;
            var dataBinding = new DataBinding()
            {
                dataSourcePath = new PropertyPath(nameof(CloudDataContainer.PlayerId)),
                bindingMode = BindingMode.ToTarget,
            };

            m_TextBox.SetBinding(new BindingId("value"), dataBinding);
            m_TextBox.AddToClassList(BlocksTheme.TextField);
            Add(m_TextBox);
        }

        void SetupCopyButton()
        {
            m_CopyToClipboard = new Button();
            m_CopyToClipboard.AddToClassList(BlocksTheme.Button);
            m_CopyToClipboard.AddToClassList(PlayerAccountTheme.LabelButton);
            m_CopyToClipboard.text = "Copy";
            Add(m_CopyToClipboard);
            m_CopyToClipboard.clicked += () => GUIUtility.systemCopyBuffer = m_CloudDataContainer.PlayerId;
        }
    }
}
