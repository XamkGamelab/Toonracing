using System;
using System.Collections.Generic;
using Blocks.Common;
using Unity.Properties;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.CloudSave.Models.Data.Player;
using Unity.Services.Core;
using UnityEngine.UIElements;

namespace Blocks.PlayerAccount
{
    /// <summary>
    /// This control will load all keys for the Player's defaul access class, and let you
    /// chose froma dropdown which one to load. This control is best suited for debugging purposes
    /// as it exposes your internal key-nomenclature, but it is a good demonstration of
    /// cloud-save usage
    /// </summary>
    [UxmlElement]
    public partial class PlayerLoadDataControl : VisualElement
    {
        VisualElement m_TopElement;
        DropdownField m_DropdownField;
        Button m_RefreshButton;
        Label m_ValueLabel;
        CloudDataContainer m_DataContainer;
        Label m_Label;

        public PlayerLoadDataControl()
        {
            m_DataContainer = new CloudDataContainer();
            m_DataContainer.UpdateCloudSaveInformation();
            dataSource = m_DataContainer;
            m_DataContainer.propertyChanged += OnDropdownChoicesChanged;
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            AddToClassList(PlayerAccountTheme.LoadDataControl);
            AddToClassList(PlayerAccountTheme.PlayerLabelContainer);
            SetupTopElement();
            SetupDropdown();
            SetupButton();
            SetupValueLabel();
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            // dispose service observer
            if (dataSource is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        void SetupTopElement()
        {
            m_TopElement = new VisualElement();
            Add(m_TopElement);
        }

        void SetupDropdown()
        {
            m_Label = new Label("Data Value");
            m_Label.AddToClassList(BlocksTheme.Label);
            m_TopElement.Add(m_Label);
            m_DropdownField = new DropdownField();
            var dataBinding = new DataBinding()
            {
                dataSourcePath = new PropertyPath(nameof(CloudDataContainer.DataKeyChoices)),
                bindingMode = BindingMode.ToTarget,
            };
            m_DropdownField.AddToClassList(BlocksTheme.Dropdown);
            m_DropdownField.AddToClassList(PlayerAccountTheme.LabelTextField);
            m_DropdownField.SetBinding(new BindingId("choices"), dataBinding);
            m_DropdownField.RegisterValueChangedCallback(_ => m_DataContainer.SelectDataKey(m_DropdownField.value));
            m_DropdownField.label = "Data Key:";

            m_TopElement.Add(m_DropdownField);
        }

        void OnDropdownChoicesChanged(object caller, BindablePropertyChangedEventArgs args)
        {
            if (args.propertyName != nameof(CloudDataContainer.DataKeyChoices))
            {
                return;
            }

            if (m_DataContainer.DataKeyChoices.Count == 0)
            {
                m_DropdownField.enabledSelf = false;
                m_DropdownField.SetValueWithoutNotify("Found no keys to load");
            }
            else
            {
                m_DropdownField.value = string.Empty;
                m_DropdownField.enabledSelf = true;
            }
        }

        void SetupButton()
        {
            m_RefreshButton = new Button();
            m_RefreshButton.clicked += () => m_DataContainer.UpdateCloudSaveInformation();
            m_RefreshButton.text = "Refresh";
            m_RefreshButton.AddToClassList(BlocksTheme.Button);
            m_RefreshButton.AddToClassList(PlayerAccountTheme.LabelButton);

            var dataBinding = new DataBinding()
            {
                dataSourcePath = new PropertyPath(nameof(CloudDataContainer.IsRefreshing)),
                bindingMode = BindingMode.ToTarget,
            };
            dataBinding.sourceToUiConverters.AddConverter((ref bool b) => !b);
            m_RefreshButton.SetBinding(new BindingId("enabledSelf"), dataBinding);

            m_TopElement.Add(m_RefreshButton);
        }

        void SetupValueLabel()
        {
            m_ValueLabel = new Label();
            m_ValueLabel.AddToClassList(BlocksTheme.Label);
            m_ValueLabel.text = "test";
            var dataBinding = new DataBinding()
            {
                dataSourcePath = new PropertyPath(nameof(CloudDataContainer.DataValue)),
                bindingMode = BindingMode.ToTarget,
            };
            dataBinding.sourceToUiConverters.AddConverter((ref string n) => $"{n}");
            m_ValueLabel.SetBinding(new BindingId("text"), dataBinding);

            Add(m_ValueLabel);
        }
    }
}
