using System;
using System.Collections.Generic;
using Blocks.Common;
using Unity.Services.Core;
using UnityEngine.UIElements;

namespace Blocks.PlayerAccount
{
    /// <summary>This UI element provides a flow to sign in or sign up with user name and password, including
    /// feedback for issues such as incorrect password, password not matching requirements, etc.</summary>
    [UxmlElement]
    public partial class PlayerSignUpOrSignInWithPassword : VisualElement
    {
        readonly CloudDataContainer m_CloudDataContainer;
        TextField m_Username;
        TextField m_Password;
        List<VisualElement> m_Controls;
        TextElement m_PwdMessage;
        VisualElement m_PwdMessageContainer;
        Button m_SignIn;
        Button m_SignUp;
        const string k_HiddenClass = "hidden";

        public event Action SignInCanceled;

        public PlayerSignUpOrSignInWithPassword() : this(null) { }

        /// <summary> Creates an instance of the object. </summary>
        /// <param name="cloudDataContainer">Optionally pass an instance of a CloudDataContainer to re-use the object</param>
        public PlayerSignUpOrSignInWithPassword(CloudDataContainer cloudDataContainer)
        {
            dataSource = m_CloudDataContainer = cloudDataContainer ?? new CloudDataContainer();
            SetupHeader();
            SetupTextFields();
            SetupErrorMessage();
            SetupButtons();
            m_Controls = new List<VisualElement>
            {
                m_Username, m_Password, m_SignIn, m_SignUp
            };
        }

        void SetupHeader()
        {
            var label = new Label("SIGN IN");
            label.AddToClassList(PlayerAccountTheme.BlocksLabel);
            label.AddToClassList(PlayerAccountTheme.BlocksSigninHeader);
            Add(label);
        }

        void SetupTextFields()
        {
            m_Username = new TextField();
            m_Username.AddToClassList(BlocksTheme.TextField);
            m_Username.textEdition.placeholder = "Username";
            m_Username.textEdition.hidePlaceholderOnFocus = true;
            m_Password = new TextField() { isPasswordField = true };
            m_Password.AddToClassList(BlocksTheme.TextField);
            m_Password.textEdition.placeholder = "Password";
            m_Password.textEdition.hidePlaceholderOnFocus = true;
            Add(m_Username);
            Add(m_Password);
        }

        void SetupButtons()
        {
            var signIn = new Button { text = "Sign In" };
            m_SignIn = signIn;
            m_SignIn.clicked += SignInOnClicked;
            var signUp = new Button { text = "Sign Up" };
            m_SignUp = signUp;
            m_SignUp.clicked += SignUpOnClicked;
            var cancel = new Button { text = "Cancel" };
            cancel.clicked += CancelClicked;

            m_SignIn.AddToClassList(BlocksTheme.Button);
            m_SignUp.AddToClassList(BlocksTheme.Button);
            cancel.AddToClassList(BlocksTheme.Button);

            var buttonContainer = new VisualElement();
            buttonContainer.AddToClassList(PlayerAccountTheme.SignUpButtonContainer);
            buttonContainer.Add(m_SignIn);
            buttonContainer.Add(cancel);
            Add(buttonContainer);
            var lineSeparator = new VisualElement();
            lineSeparator.AddToClassList(PlayerAccountTheme.LineSeparator);
            Add(lineSeparator);
            var footerContainer = new VisualElement();
            footerContainer.AddToClassList(PlayerAccountTheme.SignUpFooterContainer);
            var signUpLabel = new Label("Don't have and account?");
            signUpLabel.AddToClassList(BlocksTheme.Label);
            footerContainer.Add(signUpLabel);
            footerContainer.Add(m_SignUp);

            Add(footerContainer);
        }

        void CancelClicked()
        {
            m_PwdMessageContainer.AddToClassList(k_HiddenClass);
            SignInCanceled?.Invoke();
        }

        void SetupErrorMessage()
        {
            m_PwdMessageContainer = new VisualElement();
            m_PwdMessageContainer.AddToClassList(k_HiddenClass);
            m_PwdMessageContainer.AddToClassList(PlayerAccountTheme.SignInErrorMsg);
            var errorIcon = new VisualElement();
            errorIcon.AddToClassList(PlayerAccountTheme.ErrorIcon);
            m_PwdMessage = new TextElement();
            m_PwdMessage.text = "Lorem Ipsum";
            m_PwdMessageContainer.Add(errorIcon);
            m_PwdMessageContainer.Add(m_PwdMessage);
            Add(m_PwdMessageContainer);
        }

        async void SignUpOnClicked()
        {
            SetEnableControls(false);
            try
            {
                await m_CloudDataContainer.SignUpWithPassword(m_Username.value, m_Password.value);
            }
            catch (RequestFailedException e)
            {
                m_PwdMessage.text = e.Message;
                m_PwdMessageContainer.RemoveFromClassList(k_HiddenClass);
            }
            finally
            {
                SetEnableControls(true);
            }
        }

        async void SignInOnClicked()
        {
            SetEnableControls(false);
            try
            {
                await m_CloudDataContainer.SignInWithPassword(m_Username.value, m_Password.value);
            }
            catch (RequestFailedException e)
            {
                m_PwdMessage.text = e.Message;
                m_PwdMessageContainer.RemoveFromClassList(k_HiddenClass);
            }
            finally
            {
                SetEnableControls(true);
            }
        }

        void SetEnableControls(bool enabled)
        {
            foreach (var control in m_Controls)
                control.SetEnabled(enabled);
            if (!enabled)
                m_PwdMessageContainer.AddToClassList(k_HiddenClass);
        }
    }
}
