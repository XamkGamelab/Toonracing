using Blocks.Common;
using UnityEngine.UIElements;

namespace Blocks.PlayerAccount
{
    /// <summary> This control aggregates 3 ways in which authentication can be done,
    /// anonymous sign-in, sign-in with user-name and password, and sign-in with Unity.
    /// The latter two require setup in the dashboard, or the Authentication project settings
    /// under Identity Providers before they can be used
    /// </summary>
    [UxmlElement]
    public partial class PlayerSignIn : VisualElement
    {
        readonly CloudDataContainer m_CloudDataContainer;
        Button m_SignInAnonButton;
        Button m_SignInWithUnity;
        Button m_SignInWithPassword;
        PlayerSignUpOrSignInWithPassword m_SignInWithPasswordControl;
        const string k_HiddenClass = "hidden";

        public PlayerSignIn()
        {
            dataSource = m_CloudDataContainer = new CloudDataContainer();
            AddToClassList(PlayerAccountTheme.SignInOptions);
            SetupSignInAnonymously();
            SetupSignInWithUnity();
            SetupSignInWithPassword();
        }

        void SetupSignInAnonymously()
        {
            m_SignInAnonButton = new Button();
            m_SignInAnonButton.AddToClassList(PlayerAccountTheme.SignInAnon);
            m_SignInAnonButton.AddToClassList(BlocksTheme.Button);
            m_SignInAnonButton.text = "Sign in Anonymously";
            m_SignInAnonButton.clicked += () => m_CloudDataContainer.SignInAnonymously();
            Add(m_SignInAnonButton);
        }

        void SetupSignInWithUnity()
        {
            m_SignInWithUnity = new Button();
            m_SignInWithUnity.AddToClassList(PlayerAccountTheme.SignInUnity);
            m_SignInWithUnity.AddToClassList(BlocksTheme.Button);
            m_SignInWithUnity.text = "Sign in with Unity";
            m_SignInWithUnity.clicked += () => m_CloudDataContainer.StartSignInWithUnity();
            Add(m_SignInWithUnity);
        }

        void SetupSignInWithPassword()
        {
            m_SignInWithPassword = new Button();
            m_SignInWithPassword.AddToClassList(PlayerAccountTheme.SignInPassword);
            m_SignInWithPassword.AddToClassList(BlocksTheme.Button);
            m_SignInWithPassword.text = "Sign in with Password";
            m_SignInWithPassword.clicked += () =>
            {
                m_SignInAnonButton.AddToClassList(k_HiddenClass);
                m_SignInWithUnity.AddToClassList(k_HiddenClass);
                m_SignInWithPassword.AddToClassList(k_HiddenClass);
                if (m_SignInWithPasswordControl == null)
                {
                    var signInControl = new PlayerSignUpOrSignInWithPassword(m_CloudDataContainer);
                    m_SignInWithPasswordControl = signInControl;
                    m_SignInWithPasswordControl.SignInCanceled += () =>
                    {
                        m_SignInAnonButton.RemoveFromClassList(k_HiddenClass);
                        m_SignInWithUnity.RemoveFromClassList(k_HiddenClass);
                        m_SignInWithPassword.RemoveFromClassList(k_HiddenClass);
                        Remove(m_SignInWithPasswordControl);
                    };
                }
                Add(m_SignInWithPasswordControl);
            };
            Add(m_SignInWithPassword);
        }
    }
}
