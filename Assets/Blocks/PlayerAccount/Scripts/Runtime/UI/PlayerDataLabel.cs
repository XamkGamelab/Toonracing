using System;
using System.Threading.Tasks;
using Blocks.Common;
using Unity.Properties;
using Unity.Services.CloudSave.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace Blocks.PlayerAccount
{
    /// <summary>
    /// Provides a label that will load the cloud-save data specified
    /// by the key and the access-class specified
    /// </summary>
    [UxmlElement]
    public partial class PlayerDataLabel : VisualElement
    {
        string m_DataKey;
        IVisualElementScheduledItem m_LoadingAnimation;
        AccessClass m_DataAccessClass;
        protected CloudDataContainer m_DataContainer;
        protected Label m_Label;
        protected TextField m_TextBox;

        // Custom UXML attributes

        /// <summary> This uxml attribute can be used to customize the label tag before the data </summary>
        [UxmlAttribute("label")]
        public string LabelPrefix
        {
            get => m_Label.text;
            set => m_Label.text = value;
        }

        /// <summary> This identifies what data key to fetch from the player data </summary>
        [UxmlAttribute("data-key")]
        public string DataKey
        {
            get => m_DataKey;
            set
            {
                m_DataKey = value;
                SetDataKey();
            }
        }

        /// <summary> Access Class can be Default, Public or Proteced. Check readme or cloud-save docs for details </summary>
        [UxmlAttribute("data-access-class")]
        public AccessClass DataAccessClass
        {
            get => m_DataAccessClass;
            set
            {
                m_DataAccessClass = value;
                SetDataAccessClass();
            }
        }

        public PlayerDataLabel()
        {
            m_DataContainer = new CloudDataContainer();
            dataSource = m_DataContainer;
            AddToClassList(PlayerAccountTheme.PlayerLabelContainer);
            SetupLabel();
            LabelPrefix = "Data Value";
            SetupTextBox();
        }

        /// <summary> This method will refresh </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task RefreshAsync()
        {
            return m_DataContainer.UpdateLocalDataValue();
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
            m_TextBox.value = "<data value>";
            m_TextBox.dataSource = dataSource;
            var dataBinding = new DataBinding()
            {
                dataSourcePath = new PropertyPath(nameof(CloudDataContainer.DataValue)),
                bindingMode = BindingMode.ToTarget,
            };
            dataBinding.sourceToUiConverters.AddConverter((ref string n) =>
            {
                StopLoadingAnimation();
                return n;
            });
            m_TextBox.SetBinding(new BindingId("value"), dataBinding);
            m_DataContainer.propertyChanged += (_, args) =>
            {
                if (args.propertyName == nameof(CloudDataContainer.IsLoadingValue))
                {
                    if (m_DataContainer.IsLoadingValue)
                        StartLoadingAnimation();
                }
            };
            Add(m_TextBox);
        }

        void StartLoadingAnimation()
        {
            int dots = 0;
            m_LoadingAnimation?.Pause();
            m_LoadingAnimation = schedule.Execute(() =>
            {
                dots = (++dots % 3) + 1;
                m_TextBox.value = $"{LabelPrefix}: loading{new string('.', dots)}";
            }).Every(150);
        }

        void StopLoadingAnimation()
        {
            m_LoadingAnimation?.Pause();
            m_LoadingAnimation = null;
        }

        void SetDataKey()
        {
            if (!Application.isPlaying)
                return;

            m_DataContainer.SelectDataKey(DataKey);
        }

        void SetDataAccessClass()
        {
            if (!Application.isPlaying)
                return;

            m_DataContainer.SelectDataAccessClass(DataAccessClass);
        }
    }
}
