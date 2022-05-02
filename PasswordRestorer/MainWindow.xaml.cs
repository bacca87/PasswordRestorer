using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Input;

namespace PasswordRestorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static Random random = new Random();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void frmMain_Loaded(object sender, RoutedEventArgs e)
        {
            txtUserName.Text = System.Environment.UserName;
            txtDomain.Text = IPGlobalProperties.GetIPGlobalProperties().DomainName;
        }

        private void cmdRestore_Click(object sender, RoutedEventArgs e)
        {
            if (txtCurrentPsw.Text == string.Empty)
            {
                MessageBox.Show("The current password cannot be empty! Operation cancelled.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (txtPswToRestore.Text == string.Empty)
            {
                MessageBox.Show("The password to restore cannot be empty! Operation cancelled.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (txtCurrentPsw.Text == txtPswToRestore.Text)
            {
                MessageBox.Show("The password to restore must be different from the current one! Operation cancelled.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (WaitCursor cursor = new WaitCursor())
            {
                string currentPassword = txtCurrentPsw.Text;
                string newPassword = string.Empty;
                int historySize = 0;
                int.TryParse(txtHistorySize.Text, out historySize);
                List<string> passwordCache = new List<string>();
                passwordCache.Add(currentPassword);

                try
                {   
                    ContextType contextType = txtDomain.Text != string.Empty ? ContextType.Domain : ContextType.Machine;
                    string contextName = contextType == ContextType.Domain ? txtDomain.Text.Trim() : IPGlobalProperties.GetIPGlobalProperties().HostName;

                    using (var context = new PrincipalContext(contextType, contextName))
                    using (var user = UserPrincipal.FindByIdentity(context, txtUserName.Text))
                    {
                        for (int i = 0; i < historySize; i++)
                        {
                            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuwxyz0123456789|!£$%&/()=?^'[]1+*@°#-_";
                            

                            do
                            {
                                newPassword = new string(Enumerable.Repeat(chars, 16).Select(s => s[random.Next(s.Length)]).ToArray());
                            }
                            while (passwordCache.Contains(newPassword));

                            user.ChangePassword(currentPassword, newPassword);
                            currentPassword = newPassword;
                            passwordCache.Add(newPassword);
                        }

                        user.ChangePassword(currentPassword, txtPswToRestore.Text);
                        MessageBox.Show("Password restored succesfully!!!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    File.AppendAllText(@"Error.log", $"[{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}] Exception: {ex.Message} Current Password: {currentPassword}, NewPassword: {newPassword}" + Environment.NewLine);
                    MessageBox.Show($"\n\nException: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    MessageBox.Show($"Current Password: {currentPassword}\nCheck the \"error.log\" file for copy/paste.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void txtHistorySize_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, e.Text.Length - 1))
                e.Handled = true;
        }
        
        private void cmdHelp_Click(object sender, RoutedEventArgs e)
        {
            string rules = $@"The password, to be accepted, must comply with the following rules:
1. it must have at least eight characters.
2. it must be different from the {txtHistorySize.Text} previously used passwords.
3. it must not contain more than two consecutive username characters.
4. it must contain at least three of the following categories:
    a) a minuscule character (a ... z).
    b) an uppercase character (A ... Z).
    c) a number (1 ... 0).
    d) a non-alphanumeric character (for example:!, $, #,%).";

            MessageBox.Show(rules, "E80 Password Rules", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
